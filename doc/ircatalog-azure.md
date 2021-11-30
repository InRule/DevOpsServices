# irCatalog Service with CI/CD Features - Azure Deployment

irCatalog® is a business rule management tool that provides centralized management of rules to ensure the integrity of business rules, keep everyone working on the latest version of rules, and promote sharing of common rules across customers, processes or applications.

The CI/CD solution requires a number of binaries and configuration parameters to be deployed to the Azure® irCatalog service instance. There are two options for deploying these components:

### Create and configure a new instance of irCatalog app service

* [Database Deployment](ircatalog-azure-db.md)
* [irCatalog Web App Deployment](ircatalog-azure-cicd.md)
* [Configure CI/CD Catalog Service](#configure-catalog-service-with-cicd)

### Update an existing instance of irCatalog app service

* This option applies if you first [deployed the standard Azure irCatalog App Service](https://github.com/InRule/AzureAppServices).
* [Add CI/CD Artifacts](#add-cicd-artifacts-to-an-existing-catalog-service)
* [Configure CI/CD Catalog Service](#configure-catalog-service-with-cicd)

---
## Add CICD Artifacts to an Existing Catalog Service

This section applies when deploying only the CI/CD add-on to an existing instance of the irCatalog App Service. The steps to configure the Azure app service with the CI/CD features are:

* Download [InRule.Catalog.Service_CICD.zip](../releases/InRule.Catalog.Service_CICD.zip) and unzip in a folder on the local file system.
* Copy the content of the bin folder to the existing bin folder in App Service Editor. Accept to overwrite files, if prompted.

---
## Configure Catalog Service with CICD

This section applies to both deployment options: new irCatalog service with CI/CD or existing irCatalog service.  Once either app service was created and the binaries deployed or updated, the configuration must be updated using [Azure portal](https://portal.azure.com): 
* Download the starter configuration file [InRule.Catalog.Service_CICD.config.json](../config/InRule.Catalog.Service_CICD.config.json) and save it to the local file system. Edit the values for *AesEncryptDecryptKey* and *ApiKeyAuthentication.ApiKey* to match the values set on the InRule CI/CD service.

|Configuration Key | Comments
--- | ---
|**IsCloudBased**| Accepts values "true" or "false".  Must be set to "true" for Azure deployments and "false" for on-premise. Used for both the client and server components.
|**FilterEventsByUser**| List of space separated catalog user names.  This value is empty by default, meaning that events from all catalog users will be intercepted by CI/CD.  If usernames are listed here, the CI/CD solution will only react to events triggered by these users. Used for both the client and server components.
|**ApprovalFlow.ApplyLabelApprover**| The user designated as label assignment approver.  If this user attempts to assign a label, it will be accepted directly.  If a different user attempts the same, [the approval flow](ApprovalFlow.md) will kick in.
|**InRuleCICDServiceUri**| Complete URL for the CI/CD service, where event data are sent and processed.  For on-premise deployments, this parameter is not used.  Used only for the client component.
|**ApiKeyAuthentication.ApiKey**| A string added to the authorization header on the request made by the listener component to the CI/CD service. The value can be any string and we recommend using randomly generated GUID values. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of catalog and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.
|**AesEncryptDecryptKey**| A string value used for symmetric encryption/decryption of the payload sent by the catalog listener component to the CI/CD service. It must be between 16 and 32 characters long, with a combination of letters and numbers. For on-premise deployments, this parameter is not used.   Used for both the client and server components.  For a pair of catalog and CI/CD services that are set to work together, **this parameter must be set to the same value on both services**.

* In Azure portal, navigate to the App Service Editor:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn1.png)
* Open the bulk configuration editor, by clicking "Advanced edit", and merge the items in the file downloaded and edited before.  You must maintain the validity of the JSON array content, following the format in the two files to merge only the new configuration entries:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn2.png)
* Click Save and agree with the action that restarts the app service:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn3.png)
* Restart the app service and confirm that the irCatalog service works properly: browse to the URL in browser, open a rule application in irAuthor.

* If the InRule CI/CD App Service was created before the creation of the irCatalog App Service, it is necessary to update the CI/CD App Service configuration with the credentials required for accessing irCatalog App Service.  This can be done by navigating to the CI/CD App Service in [Azure portal](https://portal.azure.com) and setting the value of the CatalogUsername and CatalogPassword parameters.  Make sure to include "/Service.svc/api", like in the example below.  Saving the configuration and restarting the irCatalog App Service are required.

    ```
    {
        "name": "CatalogPassword",
        "value": "",
        "slotSetting": false
    },
    {
        "name": "CatalogUsername",
        "value": "admin",
        "slotSetting": false
    }
    ```

---
### Verify using irAuthor®
Using irAuthor you should now be able to connect to your catalog using the url [https://WEB_APP_NAME.azurewebsites.net/service.svc](https://WEB_APP_NAME.azurewebsites.net/service.svc).