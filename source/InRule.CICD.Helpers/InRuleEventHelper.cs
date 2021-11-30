using InRule.Repository;
using InRule.Repository.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace InRule.CICD.Helpers
{
    public class InRuleEventHelper
    {
        public enum UploadChannel
        {
            GitHub,
            Box
        }
        public enum InRuleEventHelperType
        {
            Slack,
            Teams,
            Email,
            TestSuite,
            ServiceBus,
            EventGrid,
            Java,
            JavaScript,
            AppInsights,
            Sql,
            RuleAppReport,
            RuleAppDiffReport,
            DevOps,
            EventLog,
            ApprovalFlow
        }

        [Obsolete]
        public static async Task ProcessEventAsync(ExpandoObject eventDataSource, string ruleAppXml)
        {
            var eventData = (dynamic)eventDataSource;

            try
            {
                if (eventData.OperationName == "CheckinRuleApp" || eventData.OperationName == "OverwriteRuleApp" || eventData.OperationName == "CreateRuleApp")
                    if (((IDictionary<String, object>)eventData).ContainsKey("RuleAppRevision"))
                        eventData.RuleAppRevision++;
                    else
                        ((IDictionary<String, object>)eventData).Add("RuleAppRevision", 1);

                string EventHandlers = SettingsManager.Get("On" + eventData.OperationName);
                if (string.IsNullOrEmpty(EventHandlers))
                {
                    EventHandlers = SettingsManager.Get("OnAny");
                    if (string.IsNullOrEmpty(EventHandlers))
                        return;
                }

                List<string> handlers = EventHandlers.Split(' ').ToList();

                foreach (var handler in handlers)
                {
                    try
                    {
                        InRuleEventHelperType handlerType = new InRuleEventHelperType();
                        if (Enum.IsDefined(typeof(InRuleEventHelperType), handler))
                            Enum.TryParse(handler, out handlerType);
                        else
                        {
                            string handlerTypeInConfig = SettingsManager.Get($"{handler}.Type");
                            Enum.TryParse(handlerTypeInConfig, out handlerType);
                        }

                        await NotificationHelper.NotifyAsync($"BEGIN PROCESSING {eventData.OperationName} -> {handler} ({handlerType})", string.Empty, "Debug");
                        if (handlerType == InRuleEventHelperType.Slack)
                        {
                            await SlackHelper.SendEventToSlackAsync(eventData.OperationName, eventData, "CATALOG EVENT", handler);
                        }
                        else if (handlerType == InRuleEventHelperType.Teams)
                        {
                            await TeamsHelper.SendEventToTeamsAsync(eventData.OperationName, eventData, "CATALOG EVENT", handler);
                        }
                        else if (handler == "Email")
                        {
                            await SendGridHelper.SendEventToEmailAsync(eventData.OperationName, eventData, " - InRule Catalog Event", string.Empty);
                        }
                        else if (handlerType == InRuleEventHelperType.TestSuite)
                        {
                            RuleApplicationDef ruleAppDef = null;
                            if (!string.IsNullOrEmpty(ruleAppXml))
                                ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);
                            else
                                ruleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), eventData.GUID.ToString(), eventData.RuleAppRevision, eventData.OperationName);

                            if (ruleAppDef != null)
                            {
                                TestSuiteRunnerHelper.RunRegressionTestsAsync(eventData.OperationName, eventData, ruleAppDef, handler);
                            }
                        }
                        else if (handlerType == InRuleEventHelperType.ServiceBus)
                        {
                            var eventDataJson = JsonConvert.SerializeObject(eventData);
                            AzureServiceBusHelper.SendMessageAsync(eventDataJson, handler);
                        }
                        else if (handlerType == InRuleEventHelperType.EventGrid)
                        {
                            EventGridHelper.PublishEventAsync(eventData.OperationName, eventData, handler);
                        }
                        else if (handlerType == InRuleEventHelperType.Java)
                        {
                            RuleApplicationDef ruleAppDef;
                            if (!string.IsNullOrEmpty(ruleAppXml))
                                ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);
                            else
                                ruleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), eventData.GUID.ToString(), eventData.RuleAppRevision, eventData.OperationName);

                            if (ruleAppDef != null)
                            {
                                await JavaDistributionHelper.GenerateJavaJar(ruleAppDef, true, handler);
                            }
                        }
                        else if (handlerType == InRuleEventHelperType.JavaScript)
                        {
                            RuleApplicationDef ruleAppDef;
                            if (!string.IsNullOrEmpty(ruleAppXml))
                                ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);
                            else
                                ruleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), eventData.GUID.ToString(), eventData.RuleAppRevision, eventData.OperationName);

                            if (ruleAppDef != null)
                            {
                                await JavaScriptDistributionHelper.CallDistributionServiceAsync(ruleAppDef, true, false, true, handler);
                            }
                        }
                        else if (handlerType == InRuleEventHelperType.AppInsights)
                        {
                            AzureAppInsightsHelper.PublishEventToAppInsights(eventData.OperationName, eventData, handler);
                        }
                        else if (handlerType == InRuleEventHelperType.Sql)
                        {
                            SqlDatabaseHelper.WriteEvent(eventData.OperationName, eventData, handler);
                        }
                        else if (handlerType == InRuleEventHelperType.RuleAppReport)
                        {
                            RuleApplicationDef ruleAppDef;
                            if (!string.IsNullOrEmpty(ruleAppXml))
                                ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);
                            else
                                ruleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), eventData.GUID.ToString(), eventData.RuleAppRevision, eventData.OperationName);

                            if (ruleAppDef != null)
                            {
                                await InRuleReportingHelper.GetRuleAppReportAsync(eventData.OperationName, eventData, ruleAppDef);
                            }
                        }
                        else if (handlerType == InRuleEventHelperType.RuleAppDiffReport)
                        {
                            RuleApplicationDef ruleAppDef;
                            if (!string.IsNullOrEmpty(ruleAppXml))
                                ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);
                            else
                                ruleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), eventData.GUID.ToString(), eventData.RuleAppRevision, eventData.OperationName);

                            if (ruleAppDef != null)
                            {
                                if (ruleAppDef.Revision > 1)
                                {
                                    var fromRuleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), ruleAppDef.Guid.ToString(), ruleAppDef.Revision - 1, string.Empty);
                                    if (fromRuleAppDef != null)
                                        await InRuleReportingHelper.GetRuleAppDiffReportAsync(eventData.OperationName, eventData, fromRuleAppDef, ruleAppDef);
                                }
                            }
                        }
                        else if (handlerType == InRuleEventHelperType.DevOps)
                        {
                            AzureDevOpsApiHelper.QueuePipelineBuild(handler);
                        }
                        else if (handlerType == InRuleEventHelperType.EventLog)
                        {
                            EventLog.WriteEntry("InRule", JsonConvert.SerializeObject(eventData, Newtonsoft.Json.Formatting.Indented), EventLogEntryType.Information);
                        }
                        else if (handlerType == InRuleEventHelperType.ApprovalFlow)
                        {
                            if (eventData.RequiresApproval)
                            {
                                eventData.ApprovalFlowMoniker = handler;
                                eventData = (dynamic)eventDataSource;
                                RuleApplicationDef ruleAppDef;
                                if (!string.IsNullOrEmpty(ruleAppXml))
                                    ruleAppDef = RuleApplicationDef.LoadXml(ruleAppXml);
                                else
                                    ruleAppDef = GetRuleAppDef(eventData.RepositoryUri.ToString(), eventData.GUID.ToString(), eventData.RuleAppRevision, eventData.OperationName);
                                await CheckInApprovalHelper.SendApproveRequestAsync(eventDataSource, ruleAppDef, handler);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await NotificationHelper.NotifyAsync(ex.Message, "PROCESS EVENT " + eventData.OperationName + " ERROR", "Debug");
                    }
                }
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync(ex.Message, "PROCESS EVENT (ProcessEventAsync) ERROR", "Debug");
            }
        }

        private static RuleApplicationDef GetRuleAppDef(string catalogUri, string ruleAppGuid, int revision, string catalogEventName)
        {
            try
            {
                RuleCatalogConnection connection = new RuleCatalogConnection(new Uri(catalogUri), new TimeSpan(0, 10, 0), SettingsManager.Get("CatalogUsername"), SettingsManager.Get("CatalogPassword"));
                return connection.GetSpecificRuleAppRevision(new System.Guid(ruleAppGuid), revision);
            }
            catch (Exception ex)
            {
                NotificationHelper.NotifyAsync(ex.Message, "CANNOT RETRIEVE RULEAPP FROM " + catalogUri + " - ", "Debug").Wait();
            }
            return null;
        }

        private static string GetRuleAppName(string catalogUri, string ruleAppGuid)
        {
            try
            {
                RuleCatalogConnection connection = new RuleCatalogConnection(new Uri(catalogUri), new TimeSpan(0, 10, 0), SettingsManager.Get("CatalogUsername"), SettingsManager.Get("CatalogPassword"));
                return connection.GetRuleAppRef(new System.Guid(ruleAppGuid)).Name;
            }
            catch (Exception ex)
            {
                NotificationHelper.NotifyAsync(ex.Message, "CANNOT RETRIEVE RULEAPP FROM " + catalogUri + " - ", "Debug").Wait();
            }
            return null;
        }
    }
}
