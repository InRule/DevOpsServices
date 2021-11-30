# irCatalog Service with CI/CD Features - Web App Deployment

If you have not done so already, please read the [prerequisites](deployment.md#prerequisites) before you get started.

### Sign in to Azure
First, [open a PowerShell prompt](https://docs.microsoft.com/en-us/powershell/scripting/setup/starting-windows-powershell) and use the Azure CLI to [sign in](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) to your Azure subscription:
```powershell
az login
```

### Set active subscription
If your Azure account has access to multiple subscriptions, you will need to [set your active subscription](https://docs.microsoft.com/en-us/cli/azure/account#az-account-set) to where you create your Azure resources:
```powershell
# Example: az account set --subscription "Contoso Subscription 1"
az account set --subscription SUBSCRIPTION_NAME
```

### Create resource group
Create the resource group (one resource group per environment is typical) that will contain the InRule-related Azure resources with the [az group create](https://docs.microsoft.com/en-us/cli/azure/group#az-group-create) command:
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
Create the [Azure App Service Web App](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-overview) for the Catalog Service with the [az webapp create](https://docs.microsoft.com/en-us/cli/azure/webapp#az-webapp-create) command:
```powershell
# Example: az webapp create --name contoso-catalog-prod-wa --plan inrule-prod-sp --resource-group inrule-prod-rg
az webapp create --name WEB_APP_NAME --plan APP_SERVICE_PLAN_NAME --resource-group RESOURCE_GROUP_NAME
```

### Deploy package
First, [download](../releases/InRule.Catalog.Service_CICD.zip) the latest irCatalog CI/CD package (`InRule.Catalog.Service_CICD.zip`) from GitHub. Then [deploy the zip file](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-zip) package to the Web App with the [az webapp deployment source](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment/source#az-webapp-deployment-source-config-zip) command:
```powershell
# Example: az webapp deployment source config-zip --name contoso-catalog-prod-wa --resource-group inrule-prod-rg --src InRule.Catalog.Service.zip
az webapp deployment source config-zip --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --src FILE_PATH
```

### Upload valid license file
In order for the irCatalog service to properly function, a valid license file must be uploaded to the web app. The simplest way to upload the license file is via FTP.

First, retrieve the FTP deployment profile (url and credentials) with the [az webapp deployment list-publishing-profiles](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment#az-webapp-deployment-list-publishing-profiles) command and put the values into a variable:
```powershell
# Example: az webapp deployment list-publishing-profiles --name contoso-catalog-prod-wa --resource-group inrule-prod-rg --query "[?contains(publishMethod, 'FTP')].{publishUrl:publishUrl,userName:userName,userPWD:userPWD}[0]" | ConvertFrom-Json -OutVariable creds | Out-Null
az webapp deployment list-publishing-profiles --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --query "[?contains(publishMethod, 'FTP')].{publishUrl:publishUrl,userName:userName,userPWD:userPWD}[0]" | ConvertFrom-Json -OutVariable creds | Out-Null
```

Then, upload the license file using those retrieved values:
```powershell
# Example: $client = New-Object System.Net.WebClient;$client.Credentials = New-Object System.Net.NetworkCredential($creds.userName,$creds.userPWD);$uri = New-Object System.Uri($creds.publishUrl + "/InRuleLicense.xml");$client.UploadFile($uri, "$pwd\InRuleLicense.xml");
$client = New-Object System.Net.WebClient;$client.Credentials = New-Object System.Net.NetworkCredential($creds.userName,$creds.userPWD);$uri = New-Object System.Uri($creds.publishUrl + "/InRuleLicense.xml");$client.UploadFile($uri, "LICENSE_FILE_ABSOLUTE_PATH")
```

### Change the connection string
The irCatalog application now needs to be configured to point to your irCatalog database.
```powershell
# Example: az webapp config appsettings set --name contoso-catalog-prod-wa --resource-group inrule-prod-rg --settings inrule:repository:service:connectionString="Server=tcp:contoso-catalog-prod-sql.database.windows.net,1433;Initial Catalog=catalog-prod-db;Persist Security Info=False;User ID=admin;Password=%14TVpB*g$4b;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
az webapp config appsettings set --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --settings inrule:repository:service:connectionString="Server=tcp:SERVER_NAME.database.windows.net,1433;Initial Catalog=DATABASE_NAME;Persist Security Info=False;User ID=USER_NAME;Password=USER_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
```