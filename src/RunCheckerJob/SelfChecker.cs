using System;
using System.IO;
using System.Text;
using log4net;
using Newtonsoft.Json;
using NUnit.Framework;
using Ulearn.Common.Extensions;
using Ulearn.Core;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.RunCheckerJobApi;

namespace RunCheckerJob
{
	internal class SelfChecker
	{
		private readonly DockerSandboxRunner sandboxRunner;

		private static readonly ILog log = LogManager.GetLogger(typeof(SelfChecker));

		public SelfChecker(DockerSandboxRunner sandboxRunner)
		{
			this.sandboxRunner = sandboxRunner;
		}

		public RunningResults SelfCheck(DirectoryInfo sandboxDir)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			var imageName = sandboxDir.Name;
			var srcDirectory = new DirectoryInfo(Path.GetFullPath(Path.Combine(sandboxDir.FullName, "sample/src/")));
			var zipBytes = AbstractExerciseBlock.ToZip(srcDirectory, new[] { "node_modules", ".idea" });
			var submissionFile = new FileInfo(Path.GetFullPath(Path.Combine(sandboxDir.FullName, "sample/submission.json")));
			var submission = JsonConvert.DeserializeObject<CommandRunnerSubmission>(File.ReadAllText(submissionFile.FullName));
			submission.Id = Utils.NewNormalizedGuid();
			submission.ZipFileData = zipBytes;
			submission.DockerImageName = imageName;
			var res = sandboxRunner.Run(submission);
			log.Info(res);
			return res;
		}
	}

	[TestFixture, Explicit]
	internal class SelfCheckerTests
	{
		private static readonly string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

		[Test, Explicit]
		public void JsSelfCheckTest()
		{
			var sandboxesDirectory = new DirectoryInfo(Path.GetFullPath(Path.Combine(baseDirectory, "../../../../../sandboxes/")));
			foreach (var dir in sandboxesDirectory.GetDirectories())
			{
				var res = new SelfChecker(new DockerSandboxRunner())
					.SelfCheck(dir);
				Assert.AreEqual(Verdict.Ok, res.Verdict);
			}
		}
	}
}