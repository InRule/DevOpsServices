using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace InRule.DevOps.Helpers
{
    internal class WebhookHelper
    {
        #region ConfigParams 
        private static readonly string moniker = "Webhook";
        public static string Prefix = "Webhook - ";
        #endregion
        public static async Task PostToWebhook(string ruleAppXml)
        {

            string WebhookURL = SettingsManager.Get($"{moniker}.WebhookURL");

            if (string.IsNullOrEmpty(WebhookURL))
            {
                await NotificationHelper.NotifyAsync("Webhook URL is not configured.", Prefix, "Error");
                return;
            }

            try
            {
                var client = new HttpClient();
        
                var req = new HttpRequestMessage(HttpMethod.Post, new Uri(WebhookURL))
                {
                    Content = new StringContent(ruleAppXml, Encoding.UTF8, "application/xml")
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
