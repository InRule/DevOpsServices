using InRule.Repository;
using InRule.Repository.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using InRule.CICD.Helpers.Models;

namespace InRule.CICD.Helpers
{
    public class BariumHelper
    {
        #region ConfigParams

        private static readonly string moniker = "Barium";
        public static string Prefix = "Barium - ";

        #endregion

        public static async Task BariumCreateInstance()
        {
            // Check if URL has trailing slash
            string host = SettingsManager.Get($"{moniker}.Host");
            string processName = SettingsManager.Get($"{moniker}.CreateInstance.ProcessName");

            //string hostSub = host.Substring(host.Length - 1, host.Length);
            //if (host.Substring(host.Length - 1, host.Length) == "/")
            //    host = host.Substring(host.Length - 1, host.Length);

            Barium.Authenticate authenticate = await BariumAuthenticate(host);

            Barium.AppsGetAppID appsObject = await BariumCallAppsForAppId(authenticate.ticket);

            var appData = appsObject.Data;
            var appID = "";

            foreach (var appInfo in appData)
            {
                if (appInfo.Name == processName)
                    appID = appInfo.Id;
                else await NotificationHelper.NotifyAsync($"Cannot find process name in Barium.", Prefix, "Debug");
            }
            if (appID == "")
                return;

            Barium.AppGetProcessID appInstance = await BariumCallAppsToCreateInstance(host, authenticate.ticket, appID);

            await NotificationHelper.NotifyAsync($"Successfully created an instance for process {processName}. instanceId: {appInstance.InstanceId}", Prefix, "Debug");
        }

        public static async Task<Barium.AppsGetAppID> BariumCallAppsForAppId(string ticket)
        {
            string host = SettingsManager.Get($"{moniker}.Host");
            string APIVersion = SettingsManager.Get($"{moniker}.APIVersion");

            string bariumAPIAppsURL = $"{host}/API/{APIVersion}/apps/";
            var appID = new Barium.AppsGetAppID();
            using HttpClient client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, bariumAPIAppsURL);
            request.Headers.Add("ticket", ticket);
            var res = await client.SendAsync(request);
            string postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
                appID = JsonConvert.DeserializeObject<Barium.AppsGetAppID>(postResponse);
            else
                appID.Error = res.ToString();

            return appID;
        }

        public static async Task<Barium.AppGetProcessID> BariumCallAppsToCreateInstance(string host, string ticket, string instanceId)
        {
            string APIVersion = SettingsManager.Get($"{moniker}.APIVersion");
            string template = SettingsManager.Get($"{moniker}.CreateInstance.Template");
            string message = SettingsManager.Get($"{moniker}.CreateInstance.Message");

            string bariumAPIAppsUrl = $"{host}/API/{APIVersion}/apps/{instanceId}";
            var appInstance = new Barium.AppGetProcessID();

            var dict = new Dictionary<string, string>
            {
                { "template", template },
                { "message", message }
            };

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ticket", ticket);
            var req = new HttpRequestMessage(HttpMethod.Post, new Uri(bariumAPIAppsUrl))
                { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            string postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
                appInstance = JsonConvert.DeserializeObject<Barium.AppGetProcessID>(postResponse);
            else
                appInstance.errorMessage = res.ToString();

            return appInstance;
            
        }
        public static async Task<Barium.Authenticate> BariumAuthenticate(string host)
        {
            Barium.Authenticate authenticate = new Barium.Authenticate();
            try
            {
                string username = SettingsManager.Get($"{moniker}.Username");
                string password = SettingsManager.Get($"{moniker}.Password");
                string apikey = SettingsManager.Get($"{moniker}.Apikey");
                string webticket = SettingsManager.Get($"{moniker}.Webticket");
                
                string APIVersion = SettingsManager.Get($"{moniker}.APIVersion");

                if (username.Length == 0 || password.Length == 0 || apikey.Length == 0 || webticket.Length == 0 || APIVersion.Length == 0)
                {
                    authenticate.Error = "One or more required fields are missing to authenticate with Barium Live.";
                    return authenticate;
                }

                string bariumAPIAuthURL = $"{host}/API/{APIVersion}/authenticate";

                var dict = new Dictionary<string, string>
                {
                    { "username", username },
                    { "password", password },
                    { "apikey", apikey },
                    { "webticket", webticket }
                };

                var client = new HttpClient();
                var req = new HttpRequestMessage(HttpMethod.Post, new Uri(bariumAPIAuthURL))
                    {Content = new FormUrlEncodedContent(dict)};
                var res = await client.SendAsync(req);
                string postResponse = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode)
                    authenticate = JsonConvert.DeserializeObject<Barium.Authenticate>(postResponse);
                else
                    authenticate.Error = res.ToString();

                //Barium.Authenticate authenticate = new Barium.Authenticate();
                await NotificationHelper.NotifyAsync($"Barium API Authentication Success", Prefix, "Debug");
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Barium API Authentication Error: {ex.Message}", Prefix, "Debug");
            }
            return authenticate;
        }
    }
}