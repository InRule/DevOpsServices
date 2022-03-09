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
    public class BariumLiveHelper
    {
        #region ConfigParams

        private static readonly string moniker = "BariumLive";
        public static string Prefix = "BariumLive - ";

        #endregion

        public static async Task BariumLiveCreateInstance()
        {
            // Check if URL has trailing slash
            var host = SettingsManager.Get($"{moniker}.Host");
            var processName = SettingsManager.Get($"{moniker}.ProcessName");

            //string hostSub = host.Substring(host.Length - 1, host.Length);
            //if (host.Substring(host.Length - 1, host.Length) == "/")
            //host = host.Substring(host.Length - 1, host.Length);

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

        public static async Task BariumLiveApprovalProcess(string downloadUrl)
        {
            var host = SettingsManager.Get($"{moniker}.Host");
            var processName = SettingsManager.Get($"{moniker}.ProcessName");

            //string hostSub = host.Substring(host.Length - 1, host.Length);
            //if (host.Substring(host.Length - 1, host.Length) == "/")
            //host = host.Substring(host.Length - 1, host.Length);

            Barium.Authenticate authenticate = await BariumAuthenticate(host);
            Barium.AppsGetAppID appsObject = await BariumCallAppsForAppId(authenticate.ticket);

            var appData = appsObject.Data;
            var appID = "";

            foreach (var appInfo in appData)
            {
                if (appInfo.Name == processName)
                    appID = appInfo.Id;
                
            }

            if (appID == "")
            {
                await NotificationHelper.NotifyAsync($"Cannot find process name in Barium.", Prefix, "Debug");
                return;
            }
                

            Barium.AppGetProcessID appInstance = await BariumLiveAppsPassInApprovalUrl(host, authenticate.ticket, appID, downloadUrl);

            await NotificationHelper.NotifyAsync($"Successfully sent an approval request to Barium Live for process {processName}.", Prefix, "Debug");
        }

        public static async Task<Barium.AppsGetAppID> BariumCallAppsForAppId(string ticket)
        {
            var host = SettingsManager.Get($"{moniker}.Host");
            var ApiVersion = SettingsManager.Get($"{moniker}.APIVersion");

            var bariumLiveApiAppsURL = $"{host}/API/{ApiVersion}/apps/";
            var appID = new Barium.AppsGetAppID();
            using HttpClient client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, bariumLiveApiAppsURL);
            request.Headers.Add("ticket", ticket);
            var res = await client.SendAsync(request);
            var postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
                appID = JsonConvert.DeserializeObject<Barium.AppsGetAppID>(postResponse);
            else
                appID.Error = res.ToString();

            return appID;
        }

        public static async Task<Barium.AppGetProcessID> BariumCallAppsToCreateInstance(string host, string ticket, string instanceId)
        {
            var ApiVersion = SettingsManager.Get($"{moniker}.APIVersion");
            var template = SettingsManager.Get($"{moniker}.Template");
            var message = SettingsManager.Get($"{moniker}.Message");

            var bariumLiveApiAppsUrl = $"{host}/API/{ApiVersion}/apps/{instanceId}";
            var appInstance = new Barium.AppGetProcessID();

            var dict = new Dictionary<string, string>
            {
                { "template", template },
                { "message", message }
            };

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ticket", ticket);
            var req = new HttpRequestMessage(HttpMethod.Post, new Uri(bariumLiveApiAppsUrl))
                { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            var postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
                appInstance = JsonConvert.DeserializeObject<Barium.AppGetProcessID>(postResponse);
            else
                appInstance.errorMessage = res.ToString();

            return appInstance;
            
        }
        public static async Task<Barium.AppGetProcessID> BariumLiveAppsPassInApprovalUrl(string host, string ticket, string instanceId, string approvalUrl)
        {
            var APIVersion = SettingsManager.Get($"{moniker}.APIVersion");
            var template = SettingsManager.Get($"{moniker}.Template");
            var message = SettingsManager.Get($"{moniker}.Message");
            var approvalUrlField = SettingsManager.Get($"{moniker}.ApprovalUrlField");

            var bariumLiveAPIAppsUrl = $"{host}/API/{APIVersion}/apps/{instanceId}";
            var appInstance = new Barium.AppGetProcessID();

            var dict = new Dictionary<string, string>
            {
                { "template", template },
                { "message", message },
                { approvalUrlField, approvalUrl }
            };

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ticket", ticket);
            var req = new HttpRequestMessage(HttpMethod.Post, new Uri(bariumLiveAPIAppsUrl))
                { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            var postResponse = await res.Content.ReadAsStringAsync();
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
                var username = SettingsManager.Get($"{moniker}.Username");
                var password = SettingsManager.Get($"{moniker}.Password");
                var apikey = SettingsManager.Get($"{moniker}.Apikey");
                var webticket = SettingsManager.Get($"{moniker}.Webticket");
                var apiVersion = SettingsManager.Get($"{moniker}.APIVersion");

                if (username.Length == 0 || password.Length == 0 || apikey.Length == 0 || webticket.Length == 0 || apiVersion.Length == 0)
                {
                    authenticate.Error = "One or more required fields are missing to authenticate with Barium Live.";
                    return authenticate;
                }

                var bariumLiveApiAuthURL = $"{host}/API/{apiVersion}/authenticate";

                var dict = new Dictionary<string, string>
                {
                    { "username", username },
                    { "password", password },
                    { "apikey", apikey },
                    { "webticket", webticket }
                };

                var client = new HttpClient();
                var req = new HttpRequestMessage(HttpMethod.Post, new Uri(bariumLiveApiAuthURL))
                    {Content = new FormUrlEncodedContent(dict)};
                var res = await client.SendAsync(req);
                var postResponse = await res.Content.ReadAsStringAsync();
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