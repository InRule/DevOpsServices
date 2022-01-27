# InRule CI/CD Service - Azure

If you have not done so already, please read the [prerequisites](deployment.md#prerequisites) before you get started.

#### Steps to setup the CI/CD service:

* Deployment options
  * [ARM Template Deployment](#ARM-Template-Deployment)
  * [Using PowerShell and Azure CLI](#Deployment-using-PowerShell-and-Azure-CLI)
    
* Install or update irCatalog® Service
  * [Install or update irCatalog® Service](ircatalog-azure.md)

* Enable WCF Service
  * [Configuring irCatalog WCF Service for the Event Listener](InRuleCICD_WcfBehaviorExtension.md)
  
* Configure the service:
  * [App Service Configuration](#configure-inrule-cicd-service)

## ARM Template Deployment
Deploying CI/CD with an ARM template is the easiest route as all of it can be done directly in the Azure portal.

* First download the [CI/CD ARM template zip file](../releases/InRule.CICD.ARMTemplates).
* From the Azure Portal search for **Deploy a custom template**.
* Once the custom deployment screen loads select **Build your own template in the editor**.

    ![Azure App Service Editor](../images/InRUleCICD_ARM_BuildYourOwnTemplate.png)

* Select **Load file** on the next screen and upload the **InRule.CICD.Runtime.Service.json** from the zip file downloaded above. Once loaded click **Save** at the bottom of the screen.

    ![LoadFile](../images/InRUleCICD_ARM_LoadFile.png)

* Now load the paramaters file by clicking **Edit parameters**. On the next screen select **Load file** again and load the **InRule.CICD.Runtime.Service.parameters.json** file. Click **Save** once complete. 
  
    ![EditParameters](../images/InRUleCICD_ARM_EditParameters.png)

* Choose the resource group to be used and Iif installing a new service leave **Create Or Update CICD Service Plan** as **true** and type the desired CI/CD app service name, app service plan and plan Sku. If upgrading set to **false** and enter the existing app service plan and app service name where CI/CD was previosuly installed. A finished example is below:

    ![ARMTemplateScreen](../images/InRUleCICD_ARM_ARMTemplateScreen.png)

* The last step is to deploy the license file to the newly created CI/CD app service. This can easily be done by using the App Service Editor from within the CI/CD app service. Under the Development Tools in the left hand navigation of the app service click the app service editor as shown below, then click **Go** when that page loads. Right click on some blank space under WWWROOT and click **Upload Files** to upload the InRuleLicense.xml file. 


    ![AppServiceEditor](../images/InRUleCICD_ARM_AppServiceEditor.png)

* The app service editor can also be launched from the KUDU console by adding .scm before the base URL and add dev at the end. Here is an example: https://mysite.scm.azurewebsites.net/dev
---
## PowerShell and Azure CLI Deployment

### Sign in to Microsoft Azure
First, [open a PowerShell prompt](https://docs.microsoft.com/en-us/powershell/scripting/windows-powershell/starting-windows-powershell) and use the Microsoft Azure® CLI to [sign in](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) to your Microsoft Azure subscription:
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

### Create App Service plan
Create the [App Service plan](https://docs.microsoft.com/en-us/azure/app-service/azure-web-sites-web-hosting-plans-in-depth-overview) that will host the InRule-related web apps with the [az appservice plan create](https://docs.microsoft.com/en-us/cli/azure/appservice/plan#az-appservice-plan-create) command:
```powershell
# Example: az appservice plan create --name inrule-prod-sp --resource-group inrule-prod-rg --location eastus
az appservice plan create --name APP_SERVICE_PLAN_NAME --resource-group RESOURCE_GROUP_NAME --location LOCATION
```

### Create Web App
Create the [Microsoft Azure App Service Web App](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-overview) for InRule CI/CD Service with the [az webapp create](https://docs.microsoft.com/en-us/cli/azure/webapp#az-webapp-create) command:
```powershell
# Example: az webapp create --name contoso-inrule-cicd-prod-wa --plan inrule-prod-sp --resource-group inrule-prod-rg
az webapp create --name WEB_APP_NAME --plan APP_SERVICE_PLAN_NAME --resource-group RESOURCE_GROUP_NAME
```

### Deploy package
First, [download](../releases) the latest InRule CI/CD Service package (`InRule.CICD.Runtime.Service.zip`) from GitHub. Then [deploy the zip file](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-zip) package to the Web App with the [az webapp deployment source](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment/source#az-webapp-deployment-source-config-zip) command:
```powershell
# Example: az webapp deployment source config-zip --name contoso-inrule-cicd-prod-wa --resource-group inrule-prod-rg --src InRule.CICD.Runtime.Service.zip
az webapp deployment source config-zip --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --src FILE_PATH
```

### Upload valid license file
In order for InRule CI/CD Service to properly function, a valid license file must be uploaded to the web app. The simplest way to upload the license file is via FTP.

First, retrieve the FTP deployment profile (url and credentials) with the [az webapp deployment list-publishing-profiles](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment#az-webapp-deployment-list-publishing-profiles) command and put the values into a variable:
```powershell
# Example: az webapp deployment list-publishing-profiles --name contoso-inrule-cicd-prod-wa --resource-group inrule-prod-rg --query "[?contains(publishMethod, 'FTP')].{publishUrl:publishUrl,userName:userName,userPWD:userPWD}[0]" | ConvertFrom-Json -OutVariable creds | Out-Null
az webapp deployment list-publishing-profiles --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --query "[?contains(publishMethod, 'FTP')].{publishUrl:publishUrl,userName:userName,userPWD:userPWD}[0]" | ConvertFrom-Json -OutVariable creds | Out-Null
```

Then, upload the license file using those retrieved values:
```powershell
# Example: $client = New-Object System.Net.WebClient;$client.Credentials = New-Object System.Net.NetworkCredential($creds.userName,$creds.userPWD);$uri = New-Object System.Uri($creds.publishUrl + "/InRuleLicense.xml");$client.UploadFile($uri, "$pwd\InRuleLicense.xml");
$client = New-Object System.Net.WebClient;$client.Credentials = New-Object System.Net.NetworkCredential($creds.userName,$creds.userPWD);$uri = New-Object System.Uri($creds.publishUrl + "/InRuleLicense.xml");$client.UploadFile($uri, "LICENSE_FILE_ABSOLUTE_PATH");
```
---
## Configure InRule CICD service
The service requires a set of key value pairs in order to function properly, like the subscription key provided by InRule and the symmetric encryption/decryption key used to secure the communication with the irCatalog® Service.

For now, it is possible to [download the starter config file](../config/InRule.CICD.Runtime.Service.config.json), in the format that is accepted when updating the app service via the Azure portal, and edit it. The starter file has only a few keys enabled, enough to ensure the encryption of the communication with the catalog and have the service react to a number of catalog events with a Slack message. The Slack webhook URL would have to be replaced with the correct value needed to send messages to the channel chosen and configured by the user.

For all the available actions, follow the corresponding details available at the links below, which include how :

* [Understanding and using notifications](Notifications.md)
* [Slack integration](InRuleCICD_Slack.md)
* [Azure DevOps integration](DevOps.md)
* [Trigger a DevOps pipeline running regression tests and promoting rule application](../devops)
* [Azure Event Grid integration](AzureEventGrid.md)
* [Azure Service Bus integration](AzureServiceBus.md)
* [Generate Rule Application Report](RuleAppReport.md)
* [Generate Rule Application Difference Report](RuleAppDiffReport.md)
* [Generate Java Rule Application (JAR file) with irDistribution Service](Java.md)
* [Generate JavaScript Rule Application with irDistribution Service](JavaScript.md)
* [CI/CD Approval Flow](ApprovalFlow.md)

The encryption being symmetric, the same key value must be set in the Azure catalog app service's configuration (**AesEncryptDecryptKey**). Similarly, an authentication key (**ApiKeyAuthentication.ApiKey**) is required to communicate with the CI/CD service, which has to match the value set for the catalog service.

|Configuration Key | Comments
--- | ---
|**ApiKeyAuthentication.ApiKey**| A string added to the authorization header on the request made by the listener component to the CI/CD service. The value can be any string and we recommend using randomly generated GUID values. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of catalog and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.
|**AesEncryptDecryptKey**| A string value used for symmetric encryption/decryption of the payload sent by the catalog listener component to the CI/CD service. It must be between 16 and 32 characters long, with a combination of letters and numbers. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of catalog and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.

![Azure configuration for keys](../images/InRuleCICD_configkeys.PNG)

Next, edit the json config files with all the pertinent configuration parameters to drive the runtime behavior, like which actions to run on events and necessary configuration for each action.

* In [Azure portal](https://portal.azure.com), navigate to the App Service Editor:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn1.png)
* Open the bulk configuration editor, by clicking "Advanced edit", and merge the items in the file downloaded and edited before.  You must maintain the validity of the JSON array content, following the format in the two files to merge only the new configuration entries:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn2.png)
* Click Save and agree with the action that restarts the app service:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn3.png)

* If the InRule CI/CD App Service was created and configured after setting up the CI/CD components on the irCatalog App Service, it is necessary to update the irCatalog App Service configuration with the newly created InRule CI/CD App Service URI.  This can be done by navigating to the irCatalog App Service in [Azure portal](https://portal.azure.com) and setting the value of the InRuleCICDServiceUri parameter.  Make sure to include "/Service.svc/api", like in the example below.  Saving the configuration and restarting the irCatalog App Service are required.

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn4.png)

* To confirm the integration with the irCatalog App Service, generate an event for which a handler was configured and validate that the triggered actions are correct.
