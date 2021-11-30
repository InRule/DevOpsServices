using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using InRule.CICD.Helpers;
using InRule.Repository.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace InRuleCICDCatalogPoller
{
    public static class CatalogOps
    {
        static string InRuleCICDServiceUri = SettingsManager.Get("InRuleCICDServiceUri");
        static string CatalogUri = SettingsManager.Get("CatalogUri");
        static string CatalogUsername = SettingsManager.Get("CatalogUsername");
        static string CatalogPassword = SettingsManager.Get("CatalogPassword");
        static string RuleApps = SettingsManager.Get("RuleApps");
        static string LookBackPeriodInMinutes = SettingsManager.Get("LookBackPeriodInMinutes");

        [FunctionName("CheckCatalog")]
        public static void Run([TimerTrigger("%ScheduleAppSetting%")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Catalog Check Timer trigger function began executing at: {DateTime.Now}");
            try
            {
                DateTime lastCheckDateTime = new DateTime();
                RuleCatalogConnection connection = new RuleCatalogConnection(new Uri(CatalogUri), new TimeSpan(0, 10, 0), CatalogUsername, CatalogPassword);
                //DateTime lastCheckDateTime = new DateTime(2021, 5, 3);

                if (!string.IsNullOrEmpty(LookBackPeriodInMinutes))
                {
                    int period;
                    if (int.TryParse(LookBackPeriodInMinutes, out period))
                        lastCheckDateTime = DateTime.UtcNow.AddMinutes(-period);
                }

                if (lastCheckDateTime.Year == 1)
                {
                    var pollerLabels = connection.GetAllLabels().Keys.Where(x => x.Label.StartsWith("CatalogPollerLastCheck#"));

                    var pollerLabel = string.Empty;
                    if (pollerLabels.Count() > 0)
                    {
                        pollerLabel = pollerLabels.OrderByDescending(l => l.Date).First().Label;
                        DateTime tempDateTime;
                        if (DateTime.TryParse(pollerLabel.Split('#')[1], out tempDateTime))
                            lastCheckDateTime = tempDateTime;

                        connection.RenameLabel(pollerLabel, "CatalogPollerLastCheck#" + DateTime.UtcNow.ToString());
                    }
                    else
                    {
                        lastCheckDateTime = DateTime.UtcNow.AddMinutes(-5);
                        connection.CreateLabel("CatalogPollerLastCheck#" + DateTime.UtcNow.ToString(), string.Empty);
                    }
                }

                var tempRuleAppName = string.Empty;
                var ruleAppXml = string.Empty;
                string[] ruleApps;

                if (string.IsNullOrEmpty(RuleApps))
                    ruleApps = connection.GetAllRuleApps().Select(x => x.Key.Name).Distinct().ToArray();
                else
                    ruleApps = RuleApps.Split(' ');

                foreach (var ruleApp in ruleApps)
                {
                    var ruleAppRef = connection.GetRuleAppRef(ruleApp);

                    if (ruleAppRef != null)
                    {
                        var history = connection.GetCheckinHistoryForDef(ruleAppRef.Guid).Where(p => p.Value.Date.ToUniversalTime() > lastCheckDateTime).ToDictionary(p => p.Key, p => p.Value);

                        foreach (var checkinInfo in history)
                        {
                            var polledRuleAppDef = connection.GetSpecificRuleAppRevision(ruleAppRef.Guid, checkinInfo.Key);
                            ruleAppXml = polledRuleAppDef.GetXml();

                            dynamic eventData = new ExpandoObject();
                            var eventDataDictionary = (IDictionary<string, object>)eventData;

                            eventData.RequestorUsername = checkinInfo.Value.Username;
                            eventData.OperationName = "CheckinRuleApp";
                            eventData.UtcTimestamp = DateTime.UtcNow; //.ToString("o");
                            eventData.RepositoryUri = CatalogUri;
                            eventData.Comment = checkinInfo.Value.Comment;
                            eventData.GUID = ruleAppRef.Guid;
                            eventData.RuleAppRevision = checkinInfo.Key;
                            eventData.Name = ruleApp;
                            eventData.RuleAppXml = ruleAppXml;
                            SendToCICDServicePOST(eventData, log);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error($"Catalog Check Timer trigger function failed at {DateTime.Now}: {ex.Message}\r\n{ex.StackTrace}");
            }
        }

        private static void SendToCICDServicePOST(ExpandoObject data, TraceWriter log)
        {
            try
            {
                JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                string eventData = javaScriptSerializer.Serialize((dynamic)data);
                string encryptedMessage = CryptoHelper.EncryptString(string.Empty, eventData);
                var request = (HttpWebRequest)WebRequest.Create($"{InRuleCICDServiceUri}/ProcessInRuleEvent");
                var configApiKey = SettingsManager.Get("ApiKeyAuthentication.ApiKey");
                request.Headers["Authorization"] = $"APIKEY {configApiKey}";
                request.Method = "POST";

                byte[] requestData = Encoding.ASCII.GetBytes("{\"data\" : \"" + encryptedMessage + "\"}");

                request.ContentType = "application/plain";
                request.ContentLength = requestData.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(requestData, 0, requestData.Length);
                requestStream.Close();

                var webRequestResponse = request.GetResponse();
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                log.Error($"Error sending event to InRule CI/CD service: {ex.Message}\r\n{ex.StackTrace}");
                WriteError($"Error sending event to InRule CI/CD service: " + ex.Message);
            }
        }

        private static void WriteError(string message)
        {
            EventLog.WriteEntry("Application", message, EventLogEntryType.Error);
        }
    }
}
