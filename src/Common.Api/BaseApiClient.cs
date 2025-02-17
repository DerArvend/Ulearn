using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Serilog;
using Ulearn.Common.Api.Models.Parameters;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;

namespace Ulearn.Common.Api
{
	public class BaseApiClient
	{
		private readonly ILogger logger;
		private readonly ApiClientSettings settings;
		private readonly HttpClient httpClient;

		public BaseApiClient(ILogger logger, ApiClientSettings settings)
		{
			this.logger = logger;
			this.settings = settings;

			httpClient = new HttpClient
			{
				Timeout = settings.DefaultTimeout,
				BaseAddress = settings.EndpointUrl
			};
		}

		protected async Task<TResult> MakeRequestAsync<TParameters, TResult>(HttpMethod method, string url, TParameters parameters)
			where TParameters : ApiParameters
			where TResult : ApiResponse
		{
			HttpResponseMessage response;
			if (settings.LogRequestsAndResponses)
				logger.Information("Send {method} request to {serviceName} ({url}) with parameters: {parameters}", method.Method, settings.ServiceName, url, parameters.ToString());

			try
			{
				if (method == HttpMethod.Get)
					response = await httpClient.GetAsync(BuildUrl(httpClient.BaseAddress + url, parameters.ToNameValueCollection())).ConfigureAwait(false);
				else if (method == HttpMethod.Post)
					response = await httpClient.PostAsJsonAsync(BuildUrl(httpClient.BaseAddress + url).ToString(), parameters).ConfigureAwait(false);
				else
					throw new ApiClientException($"Internal error: unsupported http method: {method.Method}");
			}
			catch (Exception e)
			{
				logger.Error(e, "Can't send request to {serviceName}: {message}", settings.ServiceName, e.Message);
				throw new ApiClientException($"Can't send request to {settings.ServiceName}: {e.Message}", e);
			}

			if (!response.IsSuccessStatusCode)
			{
				logger.Error("Bad response code from {serviceName}: {statusCode} {statusCodeDescrption}", settings.ServiceName, (int)response.StatusCode, response.StatusCode.ToString());
				throw new ApiClientException($"Bad response code from {settings.ServiceName}: {(int)response.StatusCode} {response.StatusCode}");
			}

			TResult result;
			string jsonResult;
			try
			{
				(result, jsonResult) = await response.Content.ReadAsJsonAsync<TResult>().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.Error(e, "Can't parse response from {serviceName}: {message}", settings.ServiceName, e.Message);
				throw new ApiClientException($"Can't parse response from {settings.ServiceName}: {e.Message}", e);
			}

			if (settings.LogRequestsAndResponses)
			{
				var shortened = false;
				var logResult = jsonResult;
				if (jsonResult.Length > 8 * 1024)
				{
					logResult = result.GetShortLogString();
					shortened = true;
				}

				logger.Information($"Received response from \"{settings.ServiceName}\"{(shortened ? " (сокращенный)" : "")}: {logResult}");
			}

			return result;
		}

		private Uri BuildUrl(string url, NameValueCollection parameters = null)
		{
			if (parameters == null)
				parameters = new NameValueCollection();

			var builder = new UriBuilder(url);
			var queryString = WebUtils.ParseQueryString(builder.Query);
			foreach (var parameterName in parameters.AllKeys)
				queryString[parameterName] = parameters[parameterName];

			builder.Query = queryString.ToQueryString();
			return AddCustomParametersToUrl(builder.Uri);
		}

		protected virtual Uri AddCustomParametersToUrl(Uri url)
		{
			return url;
		}
	}

	public class ApiClientSettings
	{
		public Uri EndpointUrl { get; set; }

		public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);

		public string ServiceName { get; set; } = "service";

		public bool LogRequestsAndResponses { get; set; } = true;
	}
}