using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace InRule.DevOps.Helpers
{
    public class AzureDevOpsApiHelper
    {
        private static readonly string moniker = "DevOps";
        public static string Prefix = "DEVOPS";

        public static void QueuePipelineBuild(string ruleAppName)
        {
            QueuePipelineBuild(moniker, ruleAppName);
        }
        public static async void QueuePipelineBuild(string moniker, string ruleAppName)
        {
            string Organization = SettingsManager.Get($"{moniker}.DevOpsOrganization");
            string Project = SettingsManager.Get($"{moniker}.DevOpsProject");
            string PipelineId = SettingsManager.Get($"{moniker}.DevOpsPipelineID");
            string Token = SettingsManager.Get($"{moniker}.DevOpsToken");
            string FilterByRuleApps = SettingsManager.Get($"{moniker}.FilterByRuleApps");

            if (Organization.Length == 0 || Project.Length == 0 || PipelineId.Length == 0 || Token.Length == 0)
                return;

            if (FilterByRuleApps.Length > 0)
                if (Array.IndexOf(FilterByRuleApps.Split(' '), ruleAppName) == -1)
                    return;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", Token))));

                    //string jsonBody = "{\"parameters\":  \"{\\\"ruleAppName\\\":  \\\"test\\\"}\",\"definition\": { \"id\": " + PipelineId + " } }";
                    //string jsonBody = @"{""parameters"": ""{\""ruleAppName\"":\""HelloWorldValue\""}"",""definition"": { ""id"": 1} }";
                    //string jsonBody = "{\"definition\": { \"id\": " + PipelineId + " } }";
                    string jsonBody = "{\"stagesToSkip\": [],\"templateParameters\": {\"ruleAppName\": \"" + ruleAppName + "\"},\"variables\": {}}";


                    HttpContent content = new StringContent(jsonBody, Encoding.ASCII, "application/json");

                    using (HttpResponseMessage response = await client.PostAsync($"https://dev.azure.com/{Organization}/{Project}/_apis/pipelines/" + PipelineId + "/runs?api-version=6.0", content))
                    {
                        try
                        {
                            await NotificationHelper.NotifyAsync($"Initiate DevOps Pipeline {Organization}/{Project}/{PipelineId}", Prefix, "Debug");
                            response.EnsureSuccessStatusCode();
                            string responseBody = await response.Content.ReadAsStringAsync();
                            //SlackHelper.PostSimpleMessage(responseBody, Prefix);
                        }
                        catch (Exception ex)
                        {
                            await NotificationHelper.NotifyAsync("Failed to initiate build: " + ex.Message + "\r\n" + jsonBody, Prefix, "Debug");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync("Failed to make request to initiate build: " + ex.Message, Prefix, "Debug");
            }
        }
    }
}
