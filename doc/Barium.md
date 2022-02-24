### Barium Live Integration with CI/CD

InRule CI/CD now supports performing actions in Barium Live. 

#### Create an instance from a process
A process defined in Barium Live can have a instance created based on a CI/CD catalog check in event.

---
#### Configuration

All relevant aspects of this integration are set in the configuration, under the default Barium moniker.

Create instance is an extension of the Barium configuration to further define specific activities that can take place in Barium.

This is a [sample of minimal configuration](../config/InRuleCICD_BariumCreateInstance.config) for creating an instance in Barium when a rule application has been checked in. This configuration example is **applicable for a local deployment**.  **For the Azure CI/CD app service**, the configuration follows the format in the [starter cloud config file](../config/InRule.CICD.Runtime.Service.config.json).

````
    <add key="CatalogEvents" value="CheckinRuleApp"/>
    <add key="OnCheckinRuleApp" value="Barium"/>
  
    <add key="Barium.Host" value="************************************************"/>
    <add key="Barium.APIVersion" value="v1.0"/>
    <add key="Barium.Username" value="C:\Temp\"/>
    <add key="Barium.Password" value="C:\Temp\"/>
    <add key="Barium.Apikey" value="Slack"/>
    <add key="Barium.Webticket" value="GitHub"/>

    <add key="Barium.CreateInstance" value="true"/>
	<add key="Barium.CreateInstance.Template" value="form"/>
	<add key="Barium.CreateInstance.Message" value="START"/>
	<add key="Barium.CreateInstance.ProcessName" value="IntegrationTesting"/>
````

|Configuration Key | Comments
--- | ---
|Barium.**Host**| The host name of the Barium Live environment.
|Barium.**APIVersion**| The version of the API. Default is v1.0.
|Barium.**Username**| The username of the integration user with permissions to access the API and the process to create an instance from.
|Barium.**Password**| The password of the integration user.
|Barium.**Apikey**| The API key that has been created in Barium.
|Barium.**Webticket**| Set to true to authenticate with Barium.
|Barium.**CreateInstance**| Set to true to enable the create instance activity.
|Barium.**CreateInstance.Template**| The template defined in the Barium process to create an instance from.
|Barium.**CreateInstance.Message**| The start message configured in the Barium process.
|Barium.**CreateInstance.ProcessName**| The name of the Barium process to create an instance from.


