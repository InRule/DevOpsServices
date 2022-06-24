using InRule.Repository;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using InRule.Authoring.BusinessLanguage;
using InRule.DevOps.Helpers;
using InRule.DevOps.Helpers.Models;
using InRule.Repository.Client;

namespace InRule.DevOps.Helpers;

public class BariumLiveHelper
{
    #region ConfigParams

    private static readonly string moniker = "BariumLive";
    public static string Prefix = "BariumLive - ";

    #endregion

    public static async Task BariumLiveCreateInstance()
    {
        var host = SettingsManager.Get($"{moniker}.Host");
        var processName = SettingsManager.Get($"{moniker}.ProcessName");
        var apiVersion = SettingsManager.Get($"{moniker}.ApiVersion");
        var authenticate = await BariumAuthenticate(host, apiVersion);
        var baseRequest = new BariumLive.BaseRequest()
        {
            Host = host,
            ApiVersion = apiVersion,
            Ticket = authenticate.ticket
        };
        var appsObject = await BariumCallAppsForAppId(baseRequest);
        var appId = "";
        Parallel.ForEach(appsObject.Data, appInfo =>
        {
            if (appInfo.Name == processName)
                appId = appInfo.Id;
            else
            {
                NotificationHelper.NotifyAsync($"Cannot find process name in Barium", Prefix, "Debug");
            }
        });
        await BariumCallAppsToCreateInstance(baseRequest, appId);
        await NotificationHelper.NotifyAsync($"Successfully created an instance for process {processName}", Prefix,
            "Debug");
    }

    public static async Task<string> BariumLiveApprovalProcess(string downloadUrl, dynamic eventData, RuleApplicationDef ruleAppDef)
    {
        var host = SettingsManager.Get($"{moniker}.Host");
        var processName = SettingsManager.Get($"{moniker}.ProcessName");
        var template = SettingsManager.Get($"{moniker}.Template");
        var message = SettingsManager.Get($"{moniker}.Message");
        const string apiVersion = "v1.1";
        var appId = "";
        var authenticate = await BariumAuthenticate(host, apiVersion);
        var baseRequest = new BariumLive.BaseRequest()
        {
            Host = host,
            ApiVersion = apiVersion,
            Ticket = authenticate.ticket
        };
        var appsObject = await BariumCallAppsForAppId(baseRequest);
        if (appsObject.Error != null)
        {
            NotificationHelper.NotifyAsync($"Error in getting application information", Prefix, "Debug");
            return "";
        }

        Parallel.ForEach(appsObject.Data.Where(appInfo => appInfo.Name == processName), appInfo => { appId = appInfo.Id; });
        if (appId != "")
        {
            try
            {
                var appInstance = await BariumLiveAppsPassInApprovalUrl(baseRequest, appId, downloadUrl, eventData, template, message);
                if (appInstance.success)
                {
                    var objects = await PostReports(baseRequest, appInstance.InstanceId, ruleAppDef, eventData);
                }
            }
            catch (Exception e)
            {
                NotificationHelper.NotifyAsync($"Failure posting approval to Barium Live. Error: {e.Message}", Prefix,"Debug");
                return "Error";
            }
            return "";
        }
        NotificationHelper.NotifyAsync($"Cannot find process name in Barium Live", Prefix, "Debug");
        return "";
    }

    public static async Task<string> PostReports(BariumLive.BaseRequest baseRequest, string instanceId, RuleApplicationDef ruleAppDef, dynamic eventData)
    {
        var approvalFormName = SettingsManager.Get($"{moniker}.ApprovalFormName");
        var ruleApplicationReport = SettingsManager.Get($"{moniker}.RuleApplicationReportAttachmentName");
        var ruleDifferenceReport = SettingsManager.Get($"{moniker}.RuleDifferenceReportAttachmentName");
        //var postFormsResponse = new BariumLive.PostFormsResponse();
        var objects = new BariumLive.Objects();
        var formId = "";
        using (var client = new HttpClient())
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{baseRequest.Host}/api/{baseRequest.ApiVersion}/Instances/{instanceId}/Objects");
            request.Headers.Add("ticket", baseRequest.Ticket);
            var res = await client.SendAsync(request);
            var postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode) objects = JsonConvert.DeserializeObject<BariumLive.Objects>(postResponse);
            Parallel.ForEach(objects.Data.Where(obj => obj.Name == approvalFormName), obj => { formId = obj.Id; });
        }

        var ruleApplicationReportHtml = GetRuleAppReport(ruleAppDef, eventData);
        var ruleDifferenceReportHtml = GetDifferenceReport(ruleAppDef, eventData);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("ticket", baseRequest.Ticket);
            var form = new MultipartFormDataContent();
            var ruleApplicationReportStream = new MemoryStream(Encoding.UTF8.GetBytes(ruleApplicationReportHtml));
            var differenceReportStream = new MemoryStream(Encoding.UTF8.GetBytes(ruleDifferenceReportHtml));
            var fileContentRuleApplicationReport = new StreamContent(content: ruleApplicationReportStream);
            fileContentRuleApplicationReport.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            var fileContentDifferenceReport = new StreamContent(content: differenceReportStream);
            fileContentDifferenceReport.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            form.Add(fileContentRuleApplicationReport, name: ruleApplicationReport, fileName: $"{ruleApplicationReport}.html");
            form.Add(fileContentDifferenceReport, name: ruleDifferenceReport, fileName: $"{ruleDifferenceReport}.html");

            var res = await client.PostAsync(
                $"{baseRequest.Host}/api/{baseRequest.ApiVersion}/DataForms/{formId}/Attachments", form);
            //var postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                //postFormsResponse = JsonConvert.DeserializeObject<BariumLive.PostFormsResponse>(postResponse);
                await BariumTellInstanceAllClear(baseRequest, instanceId);
            }
        }
        return "success";
    }

    public static async Task<string> BariumTellInstanceAllClear(BariumLive.BaseRequest baseRequest, string instanceId)
    {
        var attachmentFieldName = SettingsManager.Get($"{moniker}.AttachmentFieldName");
        using var client = new HttpClient();
        var dict = new Dictionary<string, string>
        {
            {"message", attachmentFieldName}
        };
        client.DefaultRequestHeaders.Add("ticket", baseRequest.Ticket);
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"{baseRequest.Host}/api/{baseRequest.ApiVersion}/Instances/{instanceId}")) { Content = new FormUrlEncodedContent(dict) };
        var res = await client.SendAsync(req);
        //var postResponse = await res.Content.ReadAsStringAsync();
        await NotificationHelper.NotifyAsync(
            res.IsSuccessStatusCode
                ? $"Successfully sent reports to Barium Live"
                : $"Error sending reports to Barium Live", Prefix, "Debug");
        return "success";
    }

    public static async Task<BariumLive.AppsGetAppID> BariumCallAppsForAppId(BariumLive.BaseRequest baseRequest)
    {
        var appId = new BariumLive.AppsGetAppID();
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseRequest.Host}/api/{baseRequest.ApiVersion}/apps/");
        request.Headers.Add("ticket", baseRequest.Ticket);
        var res = await client.SendAsync(request);
        var postResponse = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode) appId = JsonConvert.DeserializeObject<BariumLive.AppsGetAppID>(postResponse);
        else appId.Error = res.ToString();
        return appId;
    }

    public static async Task<BariumLive.AppGetProcessID> BariumCallAppsToCreateInstance(BariumLive.BaseRequest baseRequest, string instanceId)
    {
        var template = SettingsManager.Get($"{moniker}.Template");
        var message = SettingsManager.Get($"{moniker}.Message");
        var appInstance = new BariumLive.AppGetProcessID();
        var dict = new Dictionary<string, string>
        {
            {"template", template}, {"message", message}
        };
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("ticket", baseRequest.Ticket);
        var req = new HttpRequestMessage(HttpMethod.Get, new Uri($"{baseRequest.Host}/api/{baseRequest.ApiVersion}/apps/{instanceId}")) { Content = new FormUrlEncodedContent(dict) };
        var res = await client.SendAsync(req);
        var postResponse = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode) appInstance = JsonConvert.DeserializeObject<BariumLive.AppGetProcessID>(postResponse);
        else appInstance.errorMessage = res.ToString();
        return appInstance;
    }

    public static async Task<BariumLive.AppGetProcessID> BariumLiveAppsPassInApprovalUrl(BariumLive.BaseRequest baseRequest,
        string appId, string approvalUrl, dynamic eventData, string template, string message)
    {
        var approvalUrlField = SettingsManager.Get($"{moniker}.DevOpsApprovalURLField");
        var revisionLabel = SettingsManager.Get($"{moniker}.RevisionLabel");
        var ruleApplicationName = SettingsManager.Get($"{moniker}.RuleApplicationName");
        var ruleApplicationRevision = SettingsManager.Get($"{moniker}.RuleApplicationRevision");
        var ruleApplicationRevisionComment = SettingsManager.Get($"{moniker}.RuleApplicationRevisionComment");
        var appInstance = new BariumLive.AppGetProcessID();
        var dict = new Dictionary<string, string>
        {
            {"template", template},
            {"message", message},
            {approvalUrlField, approvalUrl},
            {revisionLabel, eventData.Label},
            {ruleApplicationName, eventData.Name},
            {ruleApplicationRevision, eventData.RuleAppRevision.ToString()},
            {ruleApplicationRevisionComment, "Sent by DevOps Services"}
        };
        var content = new FormUrlEncodedContent(dict);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("ticket", baseRequest.Ticket);
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"{baseRequest.Host}/api/{baseRequest.ApiVersion}/apps/{appId}")) { Content = content };

        var res = await client.SendAsync(req);
        var postResponse = await res.Content.ReadAsStringAsync();
        
        if (res.IsSuccessStatusCode)
        {
            appInstance = JsonConvert.DeserializeObject<BariumLive.AppGetProcessID>(postResponse);
            NotificationHelper.NotifyAsync($"Successfully sent an approval request to Barium Live", Prefix, "Debug");
        }
        else
        {
            appInstance.errorMessage = res.ToString();
            NotificationHelper.NotifyAsync($"Error sending approval to Barium Live{appInstance.errorMessage}", Prefix, "Debug");
        }
        return appInstance;
    }

    public static async Task<BariumLive.Authenticate> BariumAuthenticate(string host, string apiVersion)
    {
        var authenticate = new BariumLive.Authenticate();
        try
        {
            var username = SettingsManager.Get($"{moniker}.Username");
            var password = SettingsManager.Get($"{moniker}.Password");
            var apiKey = SettingsManager.Get($"{moniker}.ApiKey");
            var webTicket = SettingsManager.Get($"{moniker}.WebTicket");

            if (username.Length == 0 || password.Length == 0 || apiKey.Length == 0 || webTicket.Length == 0 || apiVersion.Length == 0)
            {
                authenticate.Error = "One or more required fields are missing to authenticate with Barium Live.";
                return authenticate;
            }
            var dict = new Dictionary<string, string>
            {
                {"username", username}, {"password", password}, {"apikey", apiKey}, {"webticket", webTicket}
            };

            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"{host}/api/{apiVersion}/authenticate")) { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            var postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                authenticate = JsonConvert.DeserializeObject<BariumLive.Authenticate>(postResponse);
                await NotificationHelper.NotifyAsync($"BariumLive API Authentication Success", Prefix, "Debug");
            }
            else
            {
                authenticate.Error = res.ToString();
                await NotificationHelper.NotifyAsync($"Unable to authenticate to BariumLive", Prefix, "Debug");
            }
        }
        catch (Exception ex)
        {
            await NotificationHelper.NotifyAsync($"Barium API Authentication Error: {ex.Message}", Prefix, "Debug");
        }
        return authenticate;
    }

    public static string GetRuleAppReport(RuleApplicationDef ruleAppDef, dynamic eventData)
    {
        NotificationHelper.NotifyAsync("Generating rule application report...", Prefix, "Debug");
        var LocalEncoding = Encoding.UTF8;
        var templateEngine = new TemplateEngine();
        //var startTime = DateTime.UtcNow;
        templateEngine.LoadRuleApplication(ruleAppDef);
        templateEngine.LoadStandardTemplateCatalog();
        var fileInfo = Authoring.Reporting.RuleAppReport.RunRuleAppReport(ruleAppDef, templateEngine);
        templateEngine.Dispose();
        var inputFile = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
        //var base64Stream = new CryptoStream(inputFile, new ToBase64Transform(), CryptoStreamMode.Read);
        var mem = new MemoryStream(File.ReadAllBytes(fileInfo.FullName));
        inputFile.Dispose();
        var ruleReport = LocalEncoding.GetString(mem.ToArray());
        mem.Dispose();
        return ruleReport;
    }

    public static string GetDifferenceReport(RuleApplicationDef ruleAppDef, dynamic eventData)
    {
        NotificationHelper.NotifyAsync("Generating rule application difference report...", "RULEAPP DIFF REPORT", "Debug");
        if (ruleAppDef.Revision <= 1) return null;
        var localEncoding = Encoding.UTF8;
        var fromRuleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), ruleAppDef.Guid.ToString(), ruleAppDef.Revision - 1, string.Empty);
        var templateEngine = new TemplateEngine();
        templateEngine.LoadRuleApplication(ruleAppDef);
        templateEngine.LoadRuleApplication(fromRuleAppDef);
        templateEngine.LoadStandardTemplateCatalog();
        var fileInfo = Authoring.Reporting.DiffReport.CreateReport(ruleAppDef, fromRuleAppDef);
        templateEngine.Dispose();
        var inputFile = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
        var mem = new MemoryStream(File.ReadAllBytes(fileInfo.FullName));
        inputFile.Dispose();
        var differenceReport = localEncoding.GetString(mem.ToArray());
        mem.Dispose();
        return differenceReport;
    }

    private static RuleApplicationDef GetRuleAppDef(string catalogUri, string ruleAppGuid, int revision, string catalogEventName)
    {
        try
        {
            var connection = new RuleCatalogConnection(new Uri(catalogUri), new TimeSpan(0, 10, 0),
                SettingsManager.Get("CatalogUsername"), SettingsManager.Get("CatalogPassword"));
            return connection.GetSpecificRuleAppRevision(new System.Guid(ruleAppGuid), revision);
        }
        catch (Exception ex)
        {
            NotificationHelper.NotifyAsync(ex.Message, "CANNOT RETRIEVE RULEAPP FROM " + catalogUri + " - ", "Debug").Wait();
        }
        return null;
    }
}
