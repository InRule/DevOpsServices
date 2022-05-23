using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using InRule.Repository;
using Newtonsoft.Json;

namespace InRule.CICD.Helpers
{
    internal class WebhookHelper
    {
        #region ConfigParams 
        private static readonly string moniker = "Webhook";
        public static string Prefix = "Webhook - ";
        #endregion
        public static async Task PostToWebhook(ExpandoObject eventDataSource, RuleApplicationDef ruleAppDef, string ruleAppXml)
        {

            string WebhookURL = SettingsManager.Get($"{moniker}.WebhookURL");
            try
            {
                var client = new HttpClient();
                var req = new HttpRequestMessage(HttpMethod.Post, new Uri("https://webhook.site/7cd26514-9e9c-46d0-90c6-ba514ed04744"))
                {
                    Content = new StringContent(ruleAppXml, Encoding.UTF8, "application/json")
                };
                var res = await client.SendAsync(req);
                var postResponse = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode)
                    await NotificationHelper.NotifyAsync($"Successfully posted to Webhook", Prefix, "Debug");
                else
                    await NotificationHelper.NotifyAsync($"Unsuccessful POST to Webhook", Prefix, "Debug");

            }

            catch (Exception e)
            {
                await NotificationHelper.NotifyAsync($" Webhook fail: {e.Message}", Prefix, "Debug");
            }

        }

    }
}
