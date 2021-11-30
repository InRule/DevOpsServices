using InRule.Repository;
using InRule.Repository.Client;
using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace InRule.CICD.Helpers
{
    public class CheckInApprovalHelper
    {
        private static readonly string moniker = "ApprovalFlow";

        [Obsolete]
        public static async Task SendApproveRequestAsync(ExpandoObject eventDataSource, RuleApplicationDef ruleAppDef)
        {
            await SendApproveRequestAsync(eventDataSource, ruleAppDef, moniker);
        }

        [Obsolete]
        public static async Task SendApproveRequestAsync(ExpandoObject eventDataSource, RuleApplicationDef ruleAppDef, string moniker)
        {
            string ApplyLabelApprover = SettingsManager.Get($"{moniker}.ApplyLabelApprover");
            string NotificationChannel = SettingsManager.Get($"{moniker}.NotificationChannel");
            string RequesterNotificationChannel = SettingsManager.Get($"{moniker}.RequesterNotificationChannel");

            try
            {
                var eventData = (dynamic)eventDataSource;
                var InRuleCICDServiceUri = eventData.InRuleCICDServiceUri;
                eventData.Name = ruleAppDef.Name;

                if (eventData.RequestorUsername.ToString().ToLower() != ApplyLabelApprover.ToLower())
                {
                    using (RuleCatalogConnection connection = new RuleCatalogConnection(new Uri(eventData.RepositoryUri.ToString()), new TimeSpan(0, 10, 0), SettingsManager.Get("CatalogUsername"), SettingsManager.Get("CatalogPassword")))
                    {
                        connection.ApplyLabel(ruleAppDef, $"PENDING {eventData.Label} ({eventData.RuleAppRevision.ToString()})");
                    }

                    JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                    string applyLabelEvent = javaScriptSerializer.Serialize(eventData);
                    string encryptedApplyLabelEvent = CryptoHelper.EncryptString(string.Empty, applyLabelEvent);

                    var approvalUrl = $"{InRuleCICDServiceUri + "/ApproveRuleAppPromotion"}?data={encryptedApplyLabelEvent}";

                    var channels = NotificationChannel.Split(' ');
                    foreach (var channel in channels)
                    {

                        switch (SettingsManager.GetHandlerType(channel))
                        {
                            case IHelper.InRuleEventHelperType.Teams:
                                TeamsHelper.PostMessageWithDownloadButton($"Click here to approve label {eventData.Label} for rule application {ruleAppDef.Name}", "Apply Label", approvalUrl, "APPROVAL FLOW - ", channel);
                                break;
                            case IHelper.InRuleEventHelperType.Slack:
                                SlackHelper.PostMessageWithDownloadButton($"Click here to approve label {eventData.Label} for rule application {ruleAppDef.Name}", "Apply Label", approvalUrl, "APPROVAL FLOW - ", channel);
                                break;
                            case IHelper.InRuleEventHelperType.Email:
                                await SendGridHelper.SendEmail($"Approval Requested - ApplyLabel by user {eventData.RequestorUsername}", "", $"{SendGridHelper.GetHtmlForEventData(eventData, "", $"To see what changed, please review Difference Report, sent separately.<br><br><a href = '{approvalUrl}'>Click here to approve changes.</a>")}", channel);
                                break;
                        }
                    }

                    var requesterChannels = RequesterNotificationChannel.Split(' ');
                    foreach (var channel in requesterChannels)
                    {
                        switch (SettingsManager.GetHandlerType(channel))
                        {
                            case IHelper.InRuleEventHelperType.Teams:
                                TeamsHelper.PostSimpleMessage($"A request for approving label {eventData.Label} has been sent to user{(ApplyLabelApprover.Split(' ').Length > 1 ? "s " : " ")}{ApplyLabelApprover}.", "APPROVAL FLOW", channel);
                                break;
                            case IHelper.InRuleEventHelperType.Slack:
                                SlackHelper.PostMarkdownMessage($"A request for approving label {eventData.Label} has been sent to user{(ApplyLabelApprover.Split(' ').Length > 1 ? "s " : " ")}{ApplyLabelApprover}.", "APPROVAL FLOW", channel);
                                break;
                            case IHelper.InRuleEventHelperType.Email:
                                await SendGridHelper.SendEmail("APPLY LABEL REQUEST SENT", $"A request for approving label {eventData.Label} has been sent to user{(ApplyLabelApprover.Split(' ').Length > 1 ? "s " : " ")}{ApplyLabelApprover}.", "", channel);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Error sending apply label approval request: {ex.Message}", "APPROVAL FLOW", "Debug");
            }
        }
    }
}
