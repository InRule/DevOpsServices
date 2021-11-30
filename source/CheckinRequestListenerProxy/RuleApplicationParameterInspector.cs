using InRule.Repository.Service.Data.Requests;
using InRule.Repository.Service.Requests;
using System;
using System.Linq;
using System.ServiceModel.Dispatcher;
using System.Xml;
using Newtonsoft.Json;
using InRule.Repository.Service.Data;
using InRule.Repository.Service.Data.Responses;
using System.Threading.Tasks;
using System.Dynamic;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

namespace CheckinRequestListener
{
    public class RuleApplicationParameterInspector : IParameterInspector
    {
        string FilterByUser = SettingsManager.Get("FilterEventsByUser");
        string ApplyLabelApprover = SettingsManager.Get("ApprovalFlow.ApplyLabelApprover");
        string InRuleCICDServiceUri = SettingsManager.Get("InRuleCICDServiceUri");
        string ApplyLabelFilterByLabels = SettingsManager.Get("ApprovalFlow.FilterByLabels");

        public object BeforeCall(string operationName, object[] inputs)
        {
            // Not all info is available in AfterCall, so populate the correlationState appropriately for retrieval after the call is complete
            try
            {
                if (inputs.Any() && !operationName.StartsWith("Get") && !operationName.StartsWith("Does") && !operationName.StartsWith("Is") && !operationName.StartsWith("Validate"))
                {
                    // Assign generic data
                    var input = (RepositoryWebRequest)inputs.First();
                    dynamic thisEvent = new ExpandoObject();
                    thisEvent.RequestorUsername = input.SecurityHeader.Identity ?? (input.SecurityHeader.Impersonate ? "Impersonated" : "UNKNOWN");
                    thisEvent.RequiresApproval = false;

                    var filterByUsers = FilterByUser.ToLower().Split(' ').ToList();

                    if (filterByUsers.Any(u => u.Length > 0) && input.GetType().Name != "ApplyLabelRequest")
                        if (!filterByUsers.Contains(thisEvent.RequestorUsername.ToString().ToLower()))
                            return thisEvent;

                    thisEvent.OperationName = operationName;
                    thisEvent.UtcTimestamp = DateTime.UtcNow;
                    thisEvent.RepositoryUri = System.ServiceModel.OperationContext.Current.RequestContext.RequestMessage.Headers.To.AbsoluteUri;

                    #region Assign request-specific data
                    // Future: Add cache of rule app guid/name, userId/name, roleId/name
                    Def appDef = Def.None;

                    // Rule App Management Requests
                    if (input is ApplyLabelRequest applyLabelRequest)
                    {
                        appDef = applyLabelRequest.AppDef;
                        thisEvent.Label = applyLabelRequest.Label;
                        thisEvent.InRuleCICDServiceUri = InRuleCICDServiceUri;

                        if (!string.IsNullOrEmpty(ApplyLabelFilterByLabels))
                        {
                            var labels = ApplyLabelFilterByLabels.ToLower().Split(' ');

                            if (labels.Contains(applyLabelRequest.Label.ToLower()))
                                if (thisEvent.RequestorUsername.ToString().ToLower() != ApplyLabelApprover.ToLower())
                                {
                                    thisEvent.RequiresApproval = true;
                                    thisEvent.GUID = appDef.Guid;
                                    thisEvent.RuleAppRevision = appDef.Revision;
                                    SendToCICDService(thisEvent);
                                    throw (new Exception($"\r\n\r\nUSER {thisEvent.RequestorUsername.ToString()} CANNOT APPLY LABEL WITHOUT AUTHORIZATION!  A REQUEST HAS BEEN SUBMITTED."));
                                }
                        }
                    }
                    else if (input is CheckinRuleAppRequest checkingRuleAppRequest)
                    {
                        appDef = checkingRuleAppRequest.AppDef;
                        if (!string.IsNullOrEmpty(checkingRuleAppRequest.Comment)) thisEvent.Comment = checkingRuleAppRequest.Comment;
                    }
                    else if (input is CheckoutDefsRequest checkoutDefsRequest)
                    {
                        appDef = checkoutDefsRequest.AppDef;
                        if (!string.IsNullOrEmpty(checkoutDefsRequest.Comment)) thisEvent.Comment = checkoutDefsRequest.Comment;
                    }
                    else if (input is CreateLabelRequest createLabelRequest)
                    {
                        thisEvent.Label = createLabelRequest.Label;
                        if (!string.IsNullOrEmpty(createLabelRequest.Comment)) thisEvent.Comment = createLabelRequest.Comment;
                    }
                    else if (input is CreateRuleAppRequest createRuleAppRequest)
                    {
                        thisEvent.GUID = createRuleAppRequest.AppGuid;
                        if (!string.IsNullOrEmpty(createRuleAppRequest.Comments)) thisEvent.Comment = createRuleAppRequest.Comments;
                    }
                    else if (input is DeleteRuleAppRequest deleteRuleAppRequest)
                    {
                        thisEvent.GUID = deleteRuleAppRequest.AppGuid;
                        thisEvent.Comment = deleteRuleAppRequest.RequestType.ToString() + ":" + deleteRuleAppRequest.MaxRevisionToKeep;
                    }
                    else if (input is DeleteWorkspaceRequest deleteWorkspaceRequest)
                    {
                        thisEvent.GUID = deleteWorkspaceRequest.AppGuid;
                        thisEvent.Comment = $"For UserId {deleteWorkspaceRequest.UserId}";
                    }
                    else if (input is OverwriteRuleAppRequest overwriteRuleAppRequest)
                    {
                        thisEvent.GUID = overwriteRuleAppRequest.TargetAppGuid;
                        if (!string.IsNullOrEmpty(overwriteRuleAppRequest.Comment)) thisEvent.Comment = overwriteRuleAppRequest.Comment;
                    }
                    else if (input is PromoteRuleAppRequest promoteRequest)
                    {
                        appDef = promoteRequest.AppDef;
                        if (!string.IsNullOrEmpty(promoteRequest.Comment)) thisEvent.Comment = promoteRequest.Comment;
                    }
                    else if (input is RemoveLabelRequest removeLabelRequest)
                    {
                        thisEvent.GUID = removeLabelRequest.AppDefGuid;
                        thisEvent.Label = removeLabelRequest.Label;
                    }
                    else if (input is RenameLabelRequest renameLabelRequest)
                    {
                        thisEvent.Comment = $"From {renameLabelRequest.OldLabel} to {renameLabelRequest.NewLabel}";
                    }
                    else if (input is SaveRuleAppRequest saveRuleAppRequest)
                    {
                        appDef = saveRuleAppRequest.AppDef;
                    }
                    else if (input is UndoCheckoutRequest undoCheckoutRequest)
                    {
                        appDef = undoCheckoutRequest.AppDef;
                    }
                    else if (input is UpdateDefRequest updateDefRequest)
                    {
                        appDef = updateDefRequest.AppDef;
                    }
                    else if (input is UpdateRuleAppRequest updateRuleAppRequest)
                    {
                        thisEvent.GUID = updateRuleAppRequest.AppGuid;
                    }
                    else if (input is UpdateStaleDefsRequest updateStaleDefsRequest)
                    {
                        appDef = updateStaleDefsRequest.AppDef;
                    }
                    // User Management Requests
                    else if (input is AddRoleRequest addRoleRequest)
                    {
                        thisEvent.Name = addRoleRequest.RoleName;
                    }
                    else if (input is AddUserRequest addUserRequest)
                    {
                        thisEvent.Name = addUserRequest.Username;
                    }
                    else if (input is AddUserToRoleRequest addUserToRoleRequest)
                    {
                        thisEvent.UserId = addUserToRoleRequest.UserId.Value;
                        thisEvent.RoleId = addUserToRoleRequest.RoleId.Value;
                    }
                    else if (input is RemoveGroupFromRoleRequest removeGroupFromRoleRequest)
                    {
                        thisEvent.GroupId = removeGroupFromRoleRequest.GroupId.Value;
                        thisEvent.RoleId = removeGroupFromRoleRequest.RoleId.Value;
                    }
                    else if (input is RemoveGroupRequest removeGroupRequest)
                    {
                        thisEvent.GroupId = removeGroupRequest.GroupId.Value;
                    }
                    else if (input is RemoveRoleRequest removeRoleRequest)
                    {
                        thisEvent.RoleId = removeRoleRequest.RoleId.Value;
                    }
                    else if (input is RemoveUserFromRoleRequest removeUserFromRoleRequest)
                    {
                        thisEvent.UserId = removeUserFromRoleRequest.UserId.Value;
                        thisEvent.RoleId = removeUserFromRoleRequest.RoleId.Value;
                    }
                    else if (input is RemoveUserRequest removeUserRequest)
                    {
                        thisEvent.UserId = removeUserRequest.UserId.Value;
                    }
                    else if (input is SetConfigParamRequest setConfigParamRequest)
                    {
                        thisEvent.Name = setConfigParamRequest.ParamName;
                        //NOTE: ParamValue not included in the output due to security implications
                    }
                    else if (input is SetUserPasswordRequest setUserPasswordRequest)
                    {
                        thisEvent.UserId = setUserPasswordRequest.UserId.Value;
                    }
                    else if (input is UpdateGroupRequest updateGroupRequest)
                    {
                        thisEvent.Name = updateGroupRequest.Group.Name;
                        thisEvent.GroupId = updateGroupRequest.Group.Id.Value;
                        thisEvent.IsActive = updateGroupRequest.Group.IsActive;
                    }
                    else if (input is UpdateRoleRequest updateRoleRequest)
                    {
                        thisEvent.Name = updateRoleRequest.Role.Name;
                        thisEvent.Permissions = updateRoleRequest.Role.Permissions.ToString();
                        thisEvent.RoleId = updateRoleRequest.Role.Id.Value;
                    }
                    else if (input is UpdateUserRequest updateUserRequest)
                    {
                        thisEvent.Name = updateUserRequest.User.Name;
                        thisEvent.IsActive = updateUserRequest.User.IsActive;
                        thisEvent.EmailAddress = updateUserRequest.User.EmailAddress;
                    }

                    if (!appDef.Equals(Def.None))
                    {
                        thisEvent.GUID = appDef.Guid;
                        thisEvent.RuleAppRevision = appDef.Revision;
                    }
                    #endregion

                    return thisEvent;
                }
            }
            catch (Exception ex)
            {
                WriteError("Error in RuleApplicationParameterInspector BeforeCall: " + ex.Message, "BEFORE CALL EVENT - ");
                if (ex.Message.Contains("CANNOT APPLY LABEL"))
                    throw ex;
            }

            return null;
        }
        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
        {
            try
            {
                // If the correlation state wasn't set in BeforeCall, it's an action type we don't need to create an event for
                if (correlationState != null && correlationState is ExpandoObject eventData)
                {
                    HandleAfterCallAsync(eventData, returnValue);
                }
            }
            catch (Exception ex)
            {
                WriteError("Error in RuleApplicationParameterInspector AfterCall: " + ex.Message, "AFTER CALL EVENT - ");
            }
        }

        // Helpers
        private void HandleAfterCallAsync(ExpandoObject eventDataSource, object returnValue)
        {
            var processingTask = Task.Run(() =>
            {
                try
                {
                    var eventData = (dynamic)eventDataSource;
                    //if (eventData.RequestorUsername.ToString().ToLower() != FilterByUser)
                    //    return;

                    var filterByUsers = FilterByUser.ToLower().Split(' ').ToList();

                    if (filterByUsers.Any(u => u.Length > 0) && returnValue.GetType().Name != "ApplyLabelRequest")
                        if (!filterByUsers.Contains(eventData.RequestorUsername.ToString().ToLower()))
                            return;

                    eventData.ProcessingTimeInMs = (DateTime.UtcNow - ((DateTime)eventData.UtcTimestamp)).TotalMilliseconds;

                    #region Try to retrieve the RuleApp Name from RuleAppXml or maintenance action result information
                    string ruleAppXml = null;
                    if (returnValue is CheckinRuleAppResponse checkinResponse)
                        ruleAppXml = checkinResponse.RuleAppXml.Xml;
                    else if (returnValue is CreateRuleAppResponse createResponse)
                        ruleAppXml = createResponse.AppXml.Xml;
                    else if (returnValue is DeleteRuleAppResponse deleteResponse)
                        ruleAppXml = deleteResponse.RuleAppXml.Xml;
                    else if (returnValue is DeleteWorkspaceResponse deleteWorkspaceResponse)
                        ruleAppXml = deleteWorkspaceResponse.RuleAppXml.Xml;
                    else if (returnValue is OverwriteRuleAppResponse overwriteResponse)
                        ruleAppXml = overwriteResponse.AppXml.Xml;
                    else if (returnValue is PromoteRuleAppResponse promoteResponse)
                        ruleAppXml = promoteResponse.AppXml.Xml;
                    else if (returnValue is RepairCatalogResponse repairResponse)
                        eventData.ResultData = JsonConvert.SerializeObject(repairResponse.Info);
                    else if (returnValue is RunDiagnosticsResponse diagnosticResponse)
                        eventData.ResultData = JsonConvert.SerializeObject(diagnosticResponse.Info);
                    else if (returnValue is SaveRuleAppResponse saveResponse)
                        ruleAppXml = saveResponse.RuleAppXml.Xml;
                    else if (returnValue is UndoCheckoutResponse undoCheckoutResponse)
                        ruleAppXml = undoCheckoutResponse.RuleAppXml.Xml;
                    else if (returnValue is UpdateStaleDefsResponse updateStaleDefsResponse)
                        ruleAppXml = updateStaleDefsResponse.AppXml.Xml;
                    else if (returnValue is UpgradeCatalogRuleAppSchemaVersionResponse upgradeResponse)
                        eventData.ResultData = JsonConvert.SerializeObject(upgradeResponse.Info);
                    else if (returnValue is UpgradeStatusResponse upgradeStatusResponse)
                        eventData.ResultData = JsonConvert.SerializeObject(upgradeStatusResponse.Status);

                    if (ruleAppXml != null)
                        LoadRuleAppNameFromXml(eventData, ruleAppXml);
                    #endregion

                    // Output final event to desired listener(s)
                    SendToCICDService(eventDataSource);
                }
                catch (Exception ex)
                {
                    WriteError("Error processing data in AfterCall: " + ex.Message, "AFTER CALL EVENT - ");
                }
            });
        }
        private void LoadRuleAppNameFromXml(ExpandoObject eventData, string ruleAppXml)
        {
            try
            {
                XmlDocument ruleAppDoc = new XmlDocument();
                ruleAppDoc.LoadXml(ruleAppXml);
                XmlNode defTag = ruleAppDoc.GetElementsByTagName("RuleApplicationDef")[0];
                ((dynamic)eventData).Name = defTag.Attributes["Name"].Value;
                //((dynamic)eventData).RuleAppRevision = defTag.Attributes["Revision"].Value;
            }
            catch (Exception ex)
            {
                // Lighter-weight log, because this isn't that significant
                Console.WriteLine("Error retrieving RuleApplicationDef Name attribute: " + ex.Message);
            }
        }
        private void WriteError(string message, string prefix)
        {
            EventLog.WriteEntry("Application", message, EventLogEntryType.Error);
        }

        // Event Listener Publishing Methods

        private void WriteEventToEventLog(string eventType, object data)
        {
            try
            {
                EventLog.WriteEntry("InRule", JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented), EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                WriteError($"Error writing {eventType} event out to Event Log: {ex.Message}\r\n" + JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented), "PUBLISH TO EVENT LOG - ");
            }
        }

        private void SendToCICDService(ExpandoObject data)
        {
            try
            {
                JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                string eventData = javaScriptSerializer.Serialize((dynamic)data);
                string encryptedMessage = CryptoHelper.EncryptString(string.Empty, eventData);
                var request = (HttpWebRequest)WebRequest.Create($"{InRuleCICDServiceUri}/ProcessInRuleEventI?data={encryptedMessage}");
                var configApiKey = SettingsManager.Get("ApiKeyAuthentication.ApiKey");
                request.Headers["Authorization"] = $"APIKEY {configApiKey}";
                request.Method = "GET";

                var webRequestResponse = request.GetResponse();
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                WriteError($"Error sending event to InRule CI/CD service: " + ex.Message, string.Empty);
            }
        }
    }
}