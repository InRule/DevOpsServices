## Deployment options

### Azure
InRule® provides cloud deployment options that allow you to run irCatalog®, irCatalog Manager website, and irServer® Rule Execution Service inside of the Microsoft® Azure® App Service environment with minimal configuration and setup. If you are already familiar with Microsoft Azure and App Service Web Apps, then you are just a few steps away from deploying InRule CI/CD.

The InRule CI/CD solution only requires the Azure irCatalog app service configured with the new event behavior and the new dedicated CI/CD service configured to respond to events from the associated irCatalog service.  It is also possible to use the same instance of an InRule CI/CD app service for multiple irCatalog app service deployments.

Before proceeding with this option, please read the [prerequisites](deployment.md#prerequisites) before you get started, the follow the [instructions for deploying to a new or existent instance of Azure irCatalog App Service](ircatalog-azure.md).

### On premises
All the features of the InRule CI/CD solution can be made available when deploying both the irCatalog service and the CI/CD service on premises instead of Azure.  The [deployment steps and configuration](ircatalog-local.md) are very similar, with a small number of exceptions.  For this choice, both services are hosted in IIS.

### Hybrid - On premises irCatalog service + Azure CI/CD service
This configuration is for an on premises irCatalog service instance set up to use the Azure hosted CI/CD app service for processing most of the actions available, with a couple of exceptions.  While this configuration is possible, it is not necessarily recommended for most scenarios and it comes with a few drawbacks, like having to configure two-way access between on premises and Azure services.

## Prerequisites

Before you get started, you'll need the make sure you have the following:

* Knowledge and familiarity with Microsoft Azure, specifically around [Azure Resource Management](https://docs.microsoft.com/en-us/azure/azure-resource-manager/), and [Azure App Service Web Apps](https://docs.microsoft.com/en-us/azure/app-service/).

* A Microsoft Azure Subscription. If you do not have an Azure subscription, create a [free account](https://azure.microsoft.com/en-us/free/) before you begin.

* A valid InRule® license file, usually named `InRuleLicense.xml`.  This license file is required for applications that depend on InRule® irSDK.  If you do not have a valid license file for InRule® irSDK, please contact [Support](mailto:support@inrule.com?subject=InRule®%20for%20Microsoft%20Azure%20-%20App%20Service%20Web%20Apps).

* [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) version 2.0.21 or later is installed. To see which version you have, run `az --version` command in your terminal window.

* [PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/powershell-scripting) version 3.0 or later is installed. To see which version you have, run `$PSVersionTable.PSVersion.ToString()` command in your PowerShell terminal window.
