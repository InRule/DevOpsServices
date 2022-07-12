using System;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using InRule.Repository;

namespace InRule.DevOps.Helpers
{
    public class AzureDevOpsApiHelper
    {
        //private static readonly string moniker = "DevOps";
        public static string Prefix = "DevOps";

        public static async void QueuePipelineBuild(string moniker, RuleApplicationDef ruleAppDef, dynamic eventData)
        {
            var Organization = SettingsManager.Get($"{moniker}.DevOpsOrganization");
            var Project = SettingsManager.Get($"{moniker}.DevOpsProject");
            var PipelineId = SettingsManager.Get($"{moniker}.DevOpsPipelineID");
            var Token = SettingsManager.Get($"{moniker}.DevOpsToken");
            var filterByRuleApps = SettingsManager.Get($"{moniker}.FilterByRuleApps");
            var filterByLabels = SettingsManager.Get($"{moniker}.FilterByLabels");
            try
            {
                if (Organization.Length == 0 || Project.Length == 0 || PipelineId.Length == 0 || Token.Length == 0)
                    return;
          
                var ruleAppName = ruleAppDef.Name;
                var label = eventData.Label;
                if (filterByRuleApps is not null && filterByRuleApps.Length  > 0 )
                {
                    if (!filterByRuleApps.Contains(ruleAppName)) return;
                }
                if (filterByLabels is not null && filterByLabels.Length > 0)
                {
                    if (!filterByLabels.Contains(label)) return;
                }
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", Token))));

                //string jsonBody = "{\"parameters\":  \"{\\\"ruleAppName\\\":  \\\"test\\\"}\",\"definition\": { \"id\": " + PipelineId + " } }";
                //string jsonBody = @"{""parameters"": ""{\""ruleAppName\"":\""HelloWorldValue\""}"",""definition"": { ""id"": 1} }";
                //string jsonBody = "{\"definition\": { \"id\": " + PipelineId + " } }";
                string jsonBody = "{\"stagesToSkip\": [],\"templateParameters\": {\"ruleAppName\": \"" + ruleAppName + "\"},\"variables\": {}}";

                var content = new StringContent(jsonBody, Encoding.ASCII, "application/json");

                using var response = await client.PostAsync($"https://dev.azure.com/{Organization}/{Project}/_apis/pipelines/" + PipelineId + "/runs?api-version=6.0", content);
                try
                {
                    await NotificationHelper.NotifyAsync($"Initiate DevOps Pipeline {Organization}/{Project}/{PipelineId} for {ruleAppName}", Prefix, "Debug");
                    response.EnsureSuccessStatusCode();
                    //var responseBody = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    { 
                        NotificationHelper.NotifyAsync($"Successfully pushed {ruleAppName} to Azure DevOps", Prefix, "Debug");
                    }
                    
                    
                }
                catch (Exception ex)
                {
                    await NotificationHelper.NotifyAsync("Failed to initiate build: " + ex.Message + "\r\n" + jsonBody, Prefix, "Debug");
                }
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync("Failed to make request to initiate build: " + ex.Message, Prefix, "Debug");
            }
        }
    }
}
