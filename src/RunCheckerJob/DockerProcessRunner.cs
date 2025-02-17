using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Ulearn.Core;
using Ulearn.Core.RunCheckerJobApi;

namespace RunCheckerJob
{
	internal class DockerProcessRunner
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(DockerProcessRunner));

		static DockerProcessRunner()
		{
			/* We should register encoding provider for Encoding.GetEncoding(1251) works */
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public static RunningResults Run(CommandRunnerSubmission submission, DockerSandboxRunnerSettings settings, string submissionDirectory)
		{
			log.Info($"Запускаю проверку решения {submission.Id}");
			var dir = new DirectoryInfo(submissionDirectory);

			try
			{
				Utils.UnpackZip(submission.ZipFileData, dir.FullName);
			}
			catch (Exception ex)
			{
				log.Error("Не могу распаковать решение", ex);
				return new RunningResults(submission.Id, Verdict.SandboxError, error: ex.ToString());
			}

			log.Info($"Запускаю Docker для решения {submission.Id} в папке {dir.FullName}");

			return RunDocker(settings, dir);
		}

		private static RunningResults RunDocker(DockerSandboxRunnerSettings settings, DirectoryInfo dir)
		{
			var name = Guid.NewGuid();
			var dockerCommand = BuildDockerCommand(settings, dir, name);
			log.Info($"Start process command: docker {dockerCommand}");
			using (var dockerShellProcess = BuildShellProcess(dockerCommand))
			{
				var sw = Stopwatch.StartNew();
				dockerShellProcess.Start();
				var readErrTask = new AsyncReader(dockerShellProcess.StandardError, settings.OutputLimit).GetDataAsync();
				var readOutTask = new AsyncReader(dockerShellProcess.StandardOutput, settings.OutputLimit).GetDataAsync();
				var isFinished = Task.WaitAll(new Task[] { readErrTask, readOutTask }, (int)(settings.MaintenanceTimeLimit + settings.TestingTimeLimit).TotalMilliseconds);
				var ms = sw.ElapsedMilliseconds;

				RunningResults unsuccessfulResult = null;

				if (!isFinished)
				{
					log.Warn($"Не хватило времени ({ms} ms) на работу Docker в папке {dir.FullName}");
					unsuccessfulResult = new RunningResults(Verdict.TimeLimit);
				}
				else if (readErrTask.Result.Length > settings.OutputLimit || readOutTask.Result.Length > settings.OutputLimit)
				{
					log.Warn("Программа вывела слишком много");
					unsuccessfulResult = new RunningResults(Verdict.OutputLimit);
				}
				else
					log.Info($"Docker закончил работу за {ms} ms и написал: {readOutTask.Result}");

				if (!dockerShellProcess.HasExited)
					GracefullyShutdownDocker(dockerShellProcess, name, settings);

				if (unsuccessfulResult != null)
					return unsuccessfulResult;

				if (dockerShellProcess.ExitCode != 0)
				{
					log.Info($"Упал в папке {dir.FullName}");
					log.Warn($"Docker написал в stderr:\n{readErrTask.Result}");
					return new RunningResults(Verdict.SandboxError, error: readErrTask.Result);
				}

				return new RunningResults(Verdict.Ok, output: readOutTask.Result, error: readErrTask.Result);
			}
		}

		private static void GracefullyShutdownDocker(Process dockerShellProcess, Guid name, SandboxRunnerSettings settings)
		{
			try
			{
				dockerShellProcess.Kill();
			}
			catch (Win32Exception)
			{
				/* Sometimes we can catch Access Denied error because the process is already terminating. It's ok, we don't need to rethrow exception */
			}
			catch (InvalidOperationException)
			{
				/* If process has already terminated */
			}

			var remainingTimeoutMs = 3000;
			while (!dockerShellProcess.HasExited)
			{
				const int time = 10;
				Thread.Sleep(time);
				remainingTimeoutMs -= time;
				if (remainingTimeoutMs <= 0)
					throw new Exception($"process {dockerShellProcess.Id} is not completed after kill");
			}

			Task.Run(() =>
			{
				var cleanup1 = BuildShellProcess($"container rm -f {name}");
				cleanup1.Start();
				var isCleanupFinished1 = cleanup1.WaitForExit((int)settings.WaitSandboxAfterKilling.TotalMilliseconds);
				if (isCleanupFinished1)
					log.Info($"Повисший контейнер {name} очищен");
				else
					log.Error($"Не удалось очистить повисший контейнер {name}. Errors {cleanup1.StandardError.ReadToEnd()}");
			});
		}

		private static Process BuildShellProcess(string shellCommand)
		{
			return new Process
			{
				StartInfo =
				{
					Arguments = shellCommand,
					FileName = "docker",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					UseShellExecute = false
				}
			};
		}

		private static string BuildDockerCommand(DockerSandboxRunnerSettings settings, DirectoryInfo dir, Guid name)
		{
			var seccompPath = settings.SeccompFileName == null ? null : ConvertToUnixPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DockerConfig", settings.SeccompFileName));
			var parts = new List<string>
			{
				"run",
				$"-v {ConvertToUnixPath(dir.FullName)}:/source",
				seccompPath == null ? "" : $"--security-opt seccomp={seccompPath}",
				"--network none",
				"--restart no",
				"--rm",
				$"--name {name}",
				$"-m {settings.MemoryLimit}b",
				$"--memory-swap {settings.MemorySwapLimit}b",
				"--cpus 1",
				settings.SandBoxName,
				$"sh -c \"cp -R /source/* /app/ ; {settings.RunCommand}\""
			};
			return string.Join(" ", parts);
		}

		private static string ConvertToUnixPath(string path) => path.Replace(@"\", "/");
	}
}