using InRule.Repository;
using InRule.Repository.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using InRule.Authoring.BusinessLanguage;
using InRule.DevOps.Helpers.Models;
using InRule.Repository.Client;
using InRule.Runtime;

namespace InRule.DevOps.Helpers;

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
        var apiVersion = SettingsManager.Get($"{moniker}.ApiVersion");

        Barium.Authenticate authenticate = await BariumAuthenticate(host, apiVersion);
        Barium.AppsGetAppID appsObject = await BariumCallAppsForAppId(host, apiVersion, authenticate.ticket);

        var appId = "";
        foreach (var appInfo in appsObject.Data)
        {
            if (appInfo.Name == processName) appId = appInfo.Id;
            else
            {
                NotificationHelper.NotifyAsync($"Cannot find process name in Barium", Prefix, "Debug");
                return;
            }
        }

        await BariumCallAppsToCreateInstance(host, apiVersion, authenticate.ticket, appId);

        await NotificationHelper.NotifyAsync($"Successfully created an instance for process {processName}", Prefix,
            "Debug");
    }

    public static async Task<string> BariumLiveApprovalProcess(string downloadUrl, dynamic eventData, RuleApplicationDef ruleAppDef)
    {
        var host = SettingsManager.Get($"{moniker}.Host");
        var processName = SettingsManager.Get($"{moniker}.ProcessName");
        var apiVersion = SettingsManager.Get($"{moniker}.ApiVersion");
        var template = SettingsManager.Get($"{moniker}.Template");
        var message = SettingsManager.Get($"{moniker}.Message");

        Barium.Authenticate authenticate = await BariumAuthenticate(host, apiVersion);
        Barium.AppsGetAppID appsObject = await BariumCallAppsForAppId(host, apiVersion, authenticate.ticket);
        if (appsObject.Error != null)
        {
            NotificationHelper.NotifyAsync($"Error in getting application information", Prefix, "Debug");
            return "";

        }

        var appId = "";
        foreach (var appInfo in appsObject.Data.Where(appInfo => appInfo.Name == processName))
        {
            appId = appInfo.Id;
        }
        if (appId == "")
        {
            NotificationHelper.NotifyAsync($"Cannot find process name in Barium Live", Prefix, "Debug");
            return "";
        }

        try
        {
            await BariumLiveAppsPassInApprovalUrl(host, apiVersion, authenticate.ticket, appId, downloadUrl, eventData, ruleAppDef, template, message);
        }
        catch (Exception e)
        {
            NotificationHelper.NotifyAsync($"Failure posting approval to Barium Live. Error: {e.Message}", Prefix, "Debug");
            return "Error";
        }

        return "";
    }

    public static async Task<Barium.AppsGetAppID> BariumCallAppsForAppId(string host, string apiVersion, string ticket)
    {
        var appId = new Barium.AppsGetAppID();
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{host}/API/{apiVersion}/apps/");
        request.Headers.Add("ticket", ticket);
        var res = await client.SendAsync(request);
        var postResponse = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode) appId = JsonConvert.DeserializeObject<Barium.AppsGetAppID>(postResponse);
        else appId.Error = res.ToString();
        return appId;
    }

    public static async Task<Barium.AppGetProcessID> BariumCallAppsToCreateInstance(string host, string apiVersion, string ticket, string instanceId)
    {
        var template = SettingsManager.Get($"{moniker}.Template");
        var message = SettingsManager.Get($"{moniker}.Message");
        var appInstance = new Barium.AppGetProcessID();

        var dict = new Dictionary<string, string>
        {
            {"template", template},
            {"message", message}
        };
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("ticket", ticket);
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"{host}/API/{apiVersion}/apps/{instanceId}")) {Content = new FormUrlEncodedContent(dict)};
        var res = await client.SendAsync(req);
        var postResponse = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode) appInstance = JsonConvert.DeserializeObject<Barium.AppGetProcessID>(postResponse);
        else appInstance.errorMessage = res.ToString();
        return appInstance;
    }

    public static async Task<Barium.AppGetProcessID> BariumLiveAppsPassInApprovalUrl(string host, string apiVersion, string ticket,
        string instanceId, string approvalUrl, dynamic eventData, RuleApplicationDef ruleAppDef, string template, string message)
    {
        var approvalUrlField = SettingsManager.Get($"{moniker}.DevOpsApprovalURLField");
        var importedRuleApplicationReport = SettingsManager.Get($"{moniker}.ImportedRuleApplicationReport");
        var importedRuleDifferenceReport = SettingsManager.Get($"{moniker}.ImportedRuleDifferenceReport");
        var revisionLabel = SettingsManager.Get($"{moniker}.RevisionLabel");
        var ruleApplicationName = SettingsManager.Get($"{moniker}.RuleApplicationName");
        var ruleApplicationRevision = SettingsManager.Get($"{moniker}.RuleApplicationRevision");
        var ruleApplicationRevisionComment = SettingsManager.Get($"{moniker}.RuleApplicationRevisionComment");

        var ruleApplicationReport = ""; var differenceReport = "";

        if (importedRuleApplicationReport != "")
        {
            try
            {
                ruleApplicationReport = GetRuleAppReport(ruleAppDef);
            }
            catch (Exception ex)
            {
                NotificationHelper.NotifyAsync($"Cannot retrieve rule application from {eventData.RepositoryUri.ToString()}, error: {ex.Message}", Prefix, "Debug");
            }
        }

        //var ruleApplicationReportPDF = PdfSharpConvert(ruleApplicationReport);

        if (importedRuleDifferenceReport != "")
        {
            try
            {
                differenceReport = GetDifferenceReport(ruleAppDef, eventData);
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Cannot retrieve rule application from {eventData.RepositoryUri.ToString()}, error: {ex.Message}",
                    Prefix, "Debug");
            }
        }

        //var differenceReportPDF =  PdfSharpConvert(differenceReport);
        

        // var map = eventData as IDictionary<string, object>;

        // var comment = eventData.Comment;
        // eventData.Comment
        var appInstance = new Barium.AppGetProcessID();
        var dict = new Dictionary<string, string>
        {
            {"template", template},
            {"message", message},
            {approvalUrlField, approvalUrl},
            {importedRuleApplicationReport, ruleApplicationReport},
            {importedRuleDifferenceReport, differenceReport},
            {revisionLabel, eventData.Label},
            {ruleApplicationName, eventData.Name},
            {ruleApplicationRevision, eventData.RuleAppRevision.ToString()},
            {ruleApplicationRevisionComment, ""}
        };
        
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("ticket", ticket);
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"{host}/API/{apiVersion}/apps/{instanceId}")) {Content = new FormUrlEncodedContent(dict)};
        var res = await client.SendAsync(req);
        var postResponse = await res.Content.ReadAsStringAsync();
        NotificationHelper.NotifyAsync($"Response Code {res.ToString()}", Prefix, "Debug");
        if (res.IsSuccessStatusCode)
        {
            appInstance = JsonConvert.DeserializeObject<Barium.AppGetProcessID>(postResponse);
            NotificationHelper.NotifyAsync($"Successfully sent an approval request to Barium Live", Prefix, "Debug");
        }
        else
        {
            appInstance.errorMessage = res.ToString();
            NotificationHelper.NotifyAsync($"Error sending approval to Barium Live{appInstance.errorMessage}", Prefix, "Debug");
        }
        return appInstance;
    }

    public static async Task<Barium.Authenticate> BariumAuthenticate(string host, string apiVersion)
    {
        var authenticate = new Barium.Authenticate();
        try
        {
            var username = SettingsManager.Get($"{moniker}.Username");
            var password = SettingsManager.Get($"{moniker}.Password");
            var apiKey = SettingsManager.Get($"{moniker}.ApiKey");
            var webTicket = SettingsManager.Get($"{moniker}.WebTicket");

            if (username.Length == 0 || password.Length == 0 || apiKey.Length == 0 || webTicket.Length == 0 ||
                apiVersion.Length == 0)
            {
                authenticate.Error = "One or more required fields are missing to authenticate with Barium Live.";
                return authenticate;
            }

            var dict = new Dictionary<string, string>
            {
                {"username", username},
                {"password", password},
                {"apikey", apiKey},
                {"webticket", webTicket}
            };

            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"{host}/API/{apiVersion}/authenticate"))
                {Content = new FormUrlEncodedContent(dict)};
            var res = await client.SendAsync(req);
            var postResponse = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                authenticate = JsonConvert.DeserializeObject<Barium.Authenticate>(postResponse);
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


    public static string GetRuleAppReport(RuleApplicationDef ruleAppDef)
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
        //ruleApplicationReport64 = GetRuleAppReportBL(ruleAppDef);
        NotificationHelper.NotifyAsync("Generating rule application difference report...", "RULEAPP DIFF REPORT", "Debug");
        if (ruleAppDef.Revision <= 1) return null;
        var localEncoding = Encoding.UTF8;
        //var revision = ruleAppDef.Revision--;
        //var catalogUsername = SettingsManager.Get("CatalogUsername");
        //var catalogPassword = SettingsManager.Get("CatalogPassword");
        //var connection = new CatalogRuleApplicationReference(eventData.RepositoryUri.ToString(), ruleAppDef.Guid.ToString(),catalogUsername, catalogPassword, revision);

        var fromRuleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), ruleAppDef.Guid.ToString(), ruleAppDef.Revision - 1, string.Empty);

        //var oldRuleAppRef = connection.GetRuleApplicationDef();
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

    //public static Byte[] PdfSharpConvert(String html)
    //{
    //    Byte[] res = null;
    //    using (MemoryStream ms = new MemoryStream())
    //    {
    //        var pdf = PdfGenerator.GeneratePdf(html, PdfSharp.PageSize.A4);
    //        pdf.Save(ms);
    //                    using (FileStream fs = new FileStream("output.txt", FileMode.OpenOrCreate))
    //        {
    //            ms.CopyTo(fs);
    //            fs.Flush();
    //        }
    //        res = ms.ToArray();

    //        using (FileStream fs = new FileStream(@"c:\code\report.pdf", FileMode.OpenOrCreate))
    //        {
    //            ms.CopyTo(fs);
    //            fs.Flush();
    //        }

    //    }
    //    return res;
    //}


    //private static RuleApplicationDef GetPreviousRuleAppDef(string catalogUri, string ruleAppGuid, int revision)
    //{
    //    try
    //    {
    //        if (revision <= 1) return null;
    //        revision--;
    //        var catalogUsername = SettingsManager.Get("CatalogUsername");
    //        var catalogPassword = SettingsManager.Get("CatalogPassword");
    //        var connection = new CatalogRuleApplicationReference(catalogUri, ruleAppGuid, catalogUsername, catalogPassword, revision);
    //        connection.GetRuleApplicationDef();

    //        return connection.GetRuleApplicationDef();
    //    }
    //    catch (Exception ex)
    //    {
    //        NotificationHelper.NotifyAsync(ex.Message, "CANNOT RETRIEVE RULEAPP FROM " + catalogUri + " - ",
    //            "Debug");
    //        NotificationHelper.NotifyAsync(
    //            $"Cannot retrieve rule application from {eventData.RepositoryUri.ToString()}, error: {ex.Message}", Prefix, "Debug");

    //    }

    //    return null;
    //}
}
