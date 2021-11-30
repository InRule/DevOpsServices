### CI/CD Architecture with SaaS / InRule Catalog Poller

For scenarios where the catalog service binaries or configuration cannot be changed, like for SaaS, we offer an alternative method of querying the catalog service for new rule application check in events and relaying the details to the InRule CI/CD service, which then can perform the actions assigned to the corresponding handler.

This poller comes under the form of an Azure function that can be set to periodically check for new revisions saved to the catalog service at which it is pointed.  With this configuration, no artifacts are required on the catalog service instance, while the CI/CD service is still required for processing.  At this time, out of all the possible catalog service events, only check-ins are being tracked when using the poller.

![CI/CD Architecture with SaaS](../images/InRuleCICD_SaaS_arch.png)

The catalog poller is a proxy between the catalog service generating the events and the CI/CD service handling them.  Therefore, the poller has a dependency on having both these services installed and configured in Azure.

**Pre-requisites**

* [InRule CI/CD Deployment and Configuration](deployment.md)
* [Azure irCatalog service](ircatalog-azure.md)
* [InRule CI/CD app service](InRuleCICDService.md)

It is important to note that each poller query will impact the catalog service, directly depending on the set configuration.  For instance, only checking a single rule application will make for a "lighter" call. Leaving the RuleApps configuration parameter empty, for a catalog with many rule applications, will cause a larger number of calls to the catalog service.

If the frequency of poller checks is low and the rule applications are updated many times over the corresponding interval, then more check in events will be processed, taking longer and causing more load on the catalog service.  Also, because most of our customers are interested in doing something with each new rule application revision, the rule application content is extracted from the catalog and included in the message sent to the CI/CD service.  The poller should be configured for the specifics of the catalog, to optimize for the combination of rule application number and sizes, updates frequency, etc.

---
## Deploy CI/CD catalog poller to Azure

This Azure Functions sample script creates the poller function app, which is a container for the function polling the catalog on a schedule. The function app is created using the Consumption plan, which is ideal for event-driven serverless workloads.

### Sign in to Microsoft Azure
First, [open a PowerShell prompt](https://docs.microsoft.com/en-us/powershell/scripting/setup/starting-windows-powershell) and use the Microsoft Azure® CLI to [sign in](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) to your Microsoft Azure subscription:
```powershell
az login
```

### Set active subscription
If your Microsoft Azure account has access to multiple subscriptions, you will need to [set your active subscription](https://docs.microsoft.com/en-us/cli/azure/account#az-account-set) to where you create your Microsoft Azure resources:
```powershell
# Example: az account set --subscription "Contoso Subscription 1"
az account set --subscription SUBSCRIPTION_NAME
```

### Create resource group
Create the resource group (one resource group per environment is typical) that will contain the InRule®-related Microsoft Azure resources with the [az group create](https://docs.microsoft.com/en-us/cli/azure/group#az-group-create) command:
```powershell
# Example: az group create --name inrule-prod-rg --location eastus
az group create --name RESOURCE_GROUP_NAME --location LOCATION
```

### Create storage account

Azure Functions requires an Azure Storage account when you create a function app instance. Function app and storage account names must be unique. [Azure Files](https://docs.microsoft.com/en-us/azure/storage/files/storage-files-introduction) must be used to store and run your function app code in a [Consumption Plan](https://docs.microsoft.com/en-us/azure/azure-functions/consumption-plan) and [Premium Plan](https://docs.microsoft.com/en-us/azure/azure-functions/functions-premium-plan).
```powershell
# Example: az storage account create --name storageaccount-cicd --location eastus --resource-group inrule-prod-rg --sku Standard_LRS
az astorage account create --name STORAGE_ACCOUNT_NAME --location LOCATION --resource-group RESOURCE_GROUP_NAME --sku Standard_LRS
```

### Create Azure function

Command for creating the poller serverless function app in the resource group.  Note that the functions version value used here is 2, but it must be changed to 1, in function's Azure configuration, once the function is deployed. 
```powershell
# Example: az functionapp create --name inrule-cicd-catalog-poller --storage-account storageaccount-cicd --consumption-plan-location eastus --resource-group inrule-prod-rg --functions-version 2
az functionapp create --name POLLER_FUNCTION_NAME --storage-account STORAGE_ACCOUNT_NAME --consumption-plan-location LOCATION --resource-group RESOURCE_GROUP_NAME --functions-version 2
```

### Deploy package
First, [download](https://github.com/InRule/InRuleCICD/tree/main/Deployment/releases) the latest irServer® Rule Execution Service package (`InRule.CICD.CatalogPoller.zip`) from GitHub. Then [deploy the zip file](InRule.CICD.CatalogPoller) package to the Azure function created with the previous step, from the folder where the zip file was downloaded:
```powershell
# Example: az functionapp deployment source config-zip -g inrule-prod-rg -n inrule-cicd-catalog-poller --src InRule.CICD.CatalogPoller.zip
az functionapp deployment source config-zip -g RESOURCE_GROUP_NAME -n POLLER_FUNCTION_NAME --src InRule.CICD.CatalogPoller.zip
```


---
## Configuration

The configuration follows the format in the [starter cloud config file](../config/InRule.CICD.CatalogPoller.config.json).

|Configuration Key | Comments
--- | ---
|**IsCloudBased**| Accepts values "true" or "false".  Must be set to "true" for the currently available Azure deployment option.
|**InRuleCICDServiceUri**| Complete URL for the CI/CD service, where event data are sent and processed.
|**ApiKeyAuthentication.ApiKey**| A string added to the authorization header on the request made by the poller component to the CI/CD service. The value can be any string and we recommend using randomly generated GUID values. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of Azure function poller and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.
|**AesEncryptDecryptKey**| A string value used for symmetric encryption/decryption of the payload sent by the catalog poller component to the CI/CD service. It must be between 16 and 32 characters long, with a combination of letters and numbers. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of Azure function poller and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.
|**CatalogUri**| Complete URL, including "/service.svc", of the catalog service instance paired with the catalog poller.
|**CatalogUsername**| Username value for irCatalog credentials that can be used by the CI/CD catalog poller. The catalog user must have permissions to query for rule applications and their history and, for the option that uses a label to "remember" the last check date and time, enough permissions to create, update, and delete labels in the catalog.
|**CatalogPassword**| Password value for irCatalog credentials that can be used by the CI/CD catalog poller.
|**RuleApps**| Optional parameter.  When present, it should contain a space separated list of rule application names.  When checking the catalog service for new versions, the poller limits the querying to the rule applications with the names specified here.  When not present, all rule applications are checked every time.
|**LookBackPeriodInMinutes**| Optional parameter.  When present, the timer uses this value in minutes for how far back in time to "look" when checking for new check in events.  Once the Azure poller function is started, in order to cover 100% of the time and not miss future events, this value should coincide with the value in minutes of the ScheduleAppSetting parameter.  When not present, a mechanism is used for knowing the start of the time interval for which to query the service, based on persisting a specific label to the catalog. 
|**ScheduleAppSetting**| A [CRON expression](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions) that decides the frequency at which the poller function runs and checks the catalog service.  When triggered, the query is either looking back the number of minutes specified in the LookBackPeriodInMinutes value or, if this parameter is not populated, it will query for the time interval since the date and time extracted from the poller related label.  For the first run when the label is not present, the service will look back for the last 5 minutes, then the label is set with the current date and time to be used for the next call. This is the CRON expression for a frequency of every 5 minutes: "0 */5 * * * *". 
