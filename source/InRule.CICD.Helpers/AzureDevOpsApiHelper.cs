using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace InRule.CICD.Helpers
{
    public class AzureDevOpsApiHelper
    {
        private static readonly string moniker = "DevOps";
        public static string Prefix = "DEVOPS";

        public static void QueuePipelineBuild()
        {
            QueuePipelineBuild(moniker);
        }
        public static async void QueuePipelineBuild(string moniker)
        {
            string Organization = SettingsManager.Get($"{moniker}.DevOpsOrganization");
            string Project = SettingsManager.Get($"{moniker}.DevOpsProject");
            string PipelineId = SettingsManager.Get($"{moniker}.DevOpsPipelineID");
            string Token = SettingsManager.Get($"{moniker}.DevOpsToken");

            if (Organization.Length == 0 || Project.Length == 0 || PipelineId.Length == 0 || Token.Length == 0)
                return;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", Token))));

                    string jsonBody = "{\"definition\": { \"id\": " + PipelineId + " } }";
                    HttpContent content = new StringContent(jsonBody, Encoding.ASCII, "application/json");

                    using (HttpResponseMessage response = await client.PostAsync($"https://dev.azure.com/{Organization}/{Project}/_apis/build/builds?api-version=5.0", content))
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
                            await NotificationHelper.NotifyAsync("Failed to initiate build: " + ex.Message, Prefix, "Debug");
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
