using InRule.CICD.Helpers;
using System;
using System.Configuration;
using System.Net;
using System.Web;

namespace InRule.CICD
{
	public class ApiKeyAuthentication : IHttpModule
	{
		private static readonly object _lockObject = new object();
		private static bool _isIntialized = false;
		private void InitializeSingleton()
		{
			if (_isIntialized) return;

			lock (_lockObject)
			{
				if (_isIntialized) return;

				// By default .NET 4.5 only enables SSL3 and TLS v1.0. Some web services (like Salesforce) forbid those
				// protocols so we must enable TLS v1.2. This is ADDITIVE; it does not remove the currently
				// set protocols (which may have been changed elsewhere in this AppDomain). This has been added
				// here so extensions (future or otherwise) will not have to manually set this value.
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

				_isIntialized = true;
			}
		}

		public void Init(HttpApplication context)
		{
			InitializeSingleton();

			context.BeginRequest += WhenRequestBegun;
		}

		private void WhenRequestBegun(object sender, EventArgs e)
		{
			if (!((System.Web.HttpApplication)sender).Request.Url.LocalPath.EndsWith("/ApproveRuleAppPromotion"))
			{
				// Try to grab the API key from the config file.
				var configApiKey = SettingsManager.Get("ApiKeyAuthentication.ApiKey");

				// If the value exists, use it.
				if (!string.IsNullOrEmpty(configApiKey))
				{
					var headerApiKey = HttpContext.Current.Request.Headers["Authorization"];

					// Is it the same as the one in the request?
					if (!string.Equals(headerApiKey?.Trim(), $"APIKEY {configApiKey}", StringComparison.OrdinalIgnoreCase))
					{
						// It's not? Beat it.
						Fail((HttpApplication)sender);
					}
				}
			}
		}

		private static void Fail(HttpApplication application)
		{
			application.Context.Response.Clear();
			application.Context.Response.Status = "401 Unauthorized";
			application.Context.Response.StatusCode = 401;
			application.Context.Response.AddHeader("WWW-Authenticate", "APIKEY realm=\"" + HttpContext.Current.Request.Url.Host + "\"");
			application.CompleteRequest();
		}

		public void Dispose()
		{ }
	}
}