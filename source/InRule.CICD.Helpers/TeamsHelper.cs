using RestSharp;
using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static InRule.CICD.Helpers.IHelper;

namespace InRule.CICD.Helpers
{
    public class TeamsHelper// : IHelper
    {
        public static void PostSimpleMessage(string message, string messagePrefix, string moniker = "Teams")
        {
            string[] _teamsWebHooks = SettingsManager.Get($"{moniker}.TeamsWebhookUrl").Split(' ');

            if (_teamsWebHooks.Length == 0)
                return;

            foreach (var teamsWebHook in _teamsWebHooks)
            {
                var client = new RestClient(teamsWebHook);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "text/plain");
                request.AddParameter("text/plain", "{\"text\":\"<b>" + messagePrefix + "</b><br>" + message + "\"}", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                Console.WriteLine(response.Content);
            }
        }

        public static void PostMessageWithDownloadButton(string message, string buttonText, string downloadUrl, string messagePrefix, string handlerMoniker)
        {
            message = "<a href='" + downloadUrl + "'>" + buttonText + "</a><br>" + message;
            PostSimpleMessage(message, messagePrefix, handlerMoniker);
        }

        public static async Task SendEventToTeamsAsync(string eventType, object data, string messagePrefix, string handlerMoniker)
        {
            try
            {
                var map = data as IDictionary<string, object>;

                var textBody = string.Empty;
                string repositoryUri = ((dynamic)data).RepositoryUri;
                string repositoryManagerUri = repositoryUri.Replace(repositoryUri.Substring(repositoryUri.LastIndexOf('/')), "/InRuleCatalogManager"); //, repositoryUri.LastIndexOf('/') - 1)), "/InRuleCatalogManager");

                if (map.ContainsKey("OperationName"))
                    textBody = $"<b>{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}</b><br>";

                textBody += $"<b>Catalog:</b> {((dynamic)data).RepositoryUri}<br>";

                textBody += $"<b>Catalog Manager (likely location):</b> {repositoryManagerUri}<br>";

                if (map.ContainsKey("Name"))
                    textBody += $"<b>Rule application:</b> {((dynamic)data).Name}<br>";

                if (map.ContainsKey("RuleAppRevision"))
                    textBody += $"<b>Revision:</b> {((dynamic)data).RuleAppRevision}<br>";

                if (map.ContainsKey("Label"))
                    textBody += $"<b>Label:</b> { ((dynamic)data).Label}<br>";

                if (map.ContainsKey("Comment"))
                    textBody += $"<b>Comment:</b> { ((dynamic)data).Comment}<br>";

                TeamsHelper.PostSimpleMessage(textBody, messagePrefix, handlerMoniker);
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Error writing {eventType} event out to Slack: {ex.Message}", "PUBLISH TO TEAMS", "Debug");
            }
        }
    }
}