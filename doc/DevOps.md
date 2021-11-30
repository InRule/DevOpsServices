### Start DevOps Build Pipeline on Catalog Event

As the [Microsoft® documentation](https://docs.microsoft.com/en-us/azure/devops/pipelines/?view=azure-devops) describes them, "*Azure® Pipelines automatically builds and tests code projects to make them available to others. It works with just about any language or project type. Azure Pipelines combines continuous integration (CI) and continuous delivery (CD) to constantly and consistently test and build your code and ship it to any target.*".

Many companies use Azure DevOps for various processes, mostly around source code based CI/CD builds and releases.  Through InRule® CI/CD configuration, it is possible to start an Azure DevOps pipeline as a result of a catalog event.

With rule applications being part of projects, but not treated as source code because of InRule specific handling, such a pipeline can be used for the more specialized steps required to incorporate rule applications into other build processes.  [One such example](../devops) is offered with this release of the InRule CI/CD framework, with which a pipeline can be started to run regression tests and promote a rule application between two catalogs.

---
#### Configuration

The DevOps action is configurable in the CI/CD config file, specifying the pipeline coordinates under the section labeled with the "DevOps" moniker.  The same moniker can then be listed under the actions triggered for a catalog event, under the corresponding handler entry in the same configuration file.

This is a [sample of minimal configuration](../config/InRuleCICD_DevOps.config) for generating the Java JAR file for the rule application being checked in, which is **applicable for a local deployment**.  **For the Azure CI/CD app service**, the configuration follows the format in the [starter cloud config file](../config/InRule.CICD.Runtime.Service.config.json).

````
  <add key="CatalogEvents" value="CheckinRuleApp"/>
  <add key="OnCheckinRuleApp" value="DevOps"/>

  <add key="DevOps.DevOpsOrganization" value="Contoso"/>
  <add key="DevOps.DevOpsProject" value="InRule"/>
  <add key="DevOps.DevOpsPipelineID" value="1"/>
  <add key="DevOps.DevOpsToken" value="*********************"/>
````

The steps for setting a personal access token (PAT) are described [here](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate).

|Configuration Key | Comments
--- | ---
|DevOps.**DevOpsOrganization**| This value is shown in the DevOps URL following this pattern: https://dev.azure.com/**Organization**/Project
|DevOps.**DevOpsProject**| Similarly, the second component in https://dev.azure.com/Organization/**Project**.
|DevOps.**DevOpsPipelineID**| The ID of the build pipeline.  Easy to find in the URL of the edit pipeline page, like https://dev.azure.com/Organization/Project/_build?definitionId=**3**.
|DevOps.**DevOpsToken**| A personal access token (PAT) is used as an alternate password to authenticate into Azure DevOps.
