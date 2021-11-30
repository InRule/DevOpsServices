## Orchestration by Configuration

The runtime behavior of the InRule CI/CD solution is driven through a configuration file (on-premise) or InRule CI/CD app service configuration (Azure).  For both deployment types, the configuration structure and principles are the same.  There is a "client" and a "server" side, splitting the configuration items between the irCatalog and CI/CD services.  When deployed on-premise without the CI/CD service, all configuration choices are covered in the only InRuleCICD.config file. 

The main areas covered in configuration are:

* Common settings for the client/service instance, like encryption and authentication keys, catalog credentials, and so on.
* List of catalog events to which the CI/CD solution subscribes.
* A section for event handlers mapped to the catalog events.  For each event to which we want to trigger actions, an entry is required, named using the prefix "On" followed by the event name.  The corresponding value is a space separated list of monikers, each representing an action defined later in the configuration.
* The bulk of the configuration file is taken by the sections containing action definitions.  For example, some available actions are for the integration with Slack, Teams or for more involved processes like generating rule application reports.

---
### Common Settings


|Configuration Key | Comments
--- | ---
|**IsCloudBased**| Accepts values "true" or "false".  Must be set to "true" for Azure deployments and "false" for on-premise. Used for both the client and server components.
|**FilterEventsByUser**| List of space separated catalog user names.  This value is empty by default, meaning that events from all catalog users will be intercepted by CI/CD.  If usernames are listed here, the CI/CD solution will only react to events triggered by these users. Used for both the client and server components.
|**InRuleCICDServiceUri**| Complete URL for the CI/CD service, where event data are sent and processed.  For on-premise deployments, this parameter is not used.  Used only for the client component.
|**ApiKeyAuthentication.ApiKey**| A string added to the authorization header on the request made by the listener component to the CI/CD service. The value can be any string and we recommend using randomly generated GUID values. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of catalog and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.
|**AesEncryptDecryptKey**| A string value used for symmetric encryption/decryption of the payload sent by the catalog listener component to the CI/CD service. It must be between 16 and 32 characters long, with a combination of letters and numbers. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of catalog and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.
|**CatalogUsername**| Username value for irCatalog credentials that can be used by the CI/CD service. Required for various actions that need to query or update the catalog.  Used only for the server component.
|**CatalogPassword**| Password value for irCatalog credentials that can be used by the CI/CD service.  Used only for the server component.

```
  <add key="IsCloudBased" value="false"/>
  <add key="AesEncryptDecryptKey" value="b14ca5898a4e4133bbce2ea2315a1916"/>
  <add key="FilterEventsByUser" value="admin marian"/>
  <add key="DebugNotifications" value="MySlack EventLog Teams"/>
  <add key="InRuleCICDServiceUri" value="http://localhost/InRule.CICD/Service.svc/api"/>
  
  <add key="CatalogUsername" value="admin"/>
  <add key="CatalogPassword" value="******"/>
```  

---
### Configuring Tracked Catalog Events

|Configuration Key | Comments
--- | ---
|**CatalogEvents**| The list of names for catalog event to which the CI/CD solution subscribes, space separated.  To enable actions when an event is triggered, a handler must be added later in the configuration file.
List of available events | CheckinRuleApp CheckoutDefs UndoCheckout CreateRuleApp OverwriteRuleApp DeleteRuleApp ApplyLabel CreateLabel RemoveLabel RenameLabel DeleteWorkspace PromoteRuleApp SaveRuleApp UpdateDef UpdateStaleDefs AddUser UpdateUser RemoveUser AddRole RemoveUserFromRole SetConfigParam SetUserPassword UpdateGroup UpdateRole
|**OnAny**| Entry for generic event handler, where actions can be set for any tracked catalog events that do not have a dedicated handler in the config file. The value is a space separated list of action monikers.
|**On***| Where * is the name of the event.  For example: OnCheckInRuleApp, OnCheckoutDefs. The value is a space separated list of action monikers.

```
  <add key="OnCheckinRuleApp" value="Slack MyRuleAppReport MyRuleAppDiffReport MyTestSuite JavaScript Java"/>
  <add key="OnApplyLabel" value="MySlack MyEmail MyApprovalFlow MyTeams"/>
  <add key="OnCheckoutDefs" value="MySlack Teams"/>
  <add key="OnUndoCheckout" value="Slack MyEventGrid"/>
  <add key="OnAny" value="Slack"/>
```

---
### CI/CD Actions

The CI/CD solution comes with a set of "built-in" actions and integration options, listed below.  Each action has a corresponding moniker (like "Slack", "Email"), which is mandatory to use for either marking the default configuration entries or for deciding the type of alternate sections for that action.  For instance, the first group of entries uses the "Slack" moniker, with which all configuration keys are prefixed.  Once defined in configuration, the "Slack" action can then be used with any of the handlers mapped to catalog events.

  ```
  <add key="CatalogEvents" value="CheckinRuleApp" />

  <add key="OnCheckinRuleApp" value="Slack" />

  <add key="Slack.SlackWebhookUrl" value="https://hooks.slack.com/services/AAAAA/BBBBBBBBBBBBBBBBBBBBB"/>
  ```

For other events, we may choose to direct notifications to a different Slack channel.  In that case, we can add another entry for the Slack webhook URL, using an arbitrary moniker like "MySlack".  Since the default "Slack" moniker is not used in this case, we need to set the type of "MySlack" entry as "Slack", like below:

  ```
  <add key="CatalogEvents" value="CheckinRuleApp OnUndoCheckout" />

  <add key="OnCheckinRuleApp" value="Slack" />
  <add key="OnUndoCheckout" value="MySlack" />

  <add key="Slack.SlackWebhookUrl" value="https://hooks.slack.com/services/AAAAA/BBBBBBBBBBBBBBBBBBBBB"/>

  <add key="MySlack.Type" value="Slack"/>
  <add key="MySlack.SlackWebhookUrl" value="https://hooks.slack.com/services/CCCCC/DDDDDDDDDDDDDDDDDDDDD"/>
  ```

|Configuration Key | Comments
--- | ---
**List of available actions** | AppInsights Slack Teams Email ApprovalFlow RuleAppReport RuleAppDiffReport TestSuite JavaScript Java EventGrid ServiceBus DevOps GitHub Box

