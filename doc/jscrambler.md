### Jscrambler JavaScript Integration
 
InRule Technology® is pleased to include a Jscrambler helper in our DevOps for irCatalog offering.
 
Jscrambler is a leader in enterprise grade JavaScript client side protection that prevents web data exfiltration atacks. They offer a toolset to transform JavaScript in a way that makes it extremely difficult to reverse engineer, as well as a monitoring framework for the files ran through their service.

To enable Jscrambler protection you set the JavaScript.JscramblerEnable parameter to "true" in the config file.
 
Other parameters that are needed from your environmnet are below and can be found here : [Jscrambler Profile](https://app.jscrambler.com/profile?_ga=2.2804592.1107903799.1637167613-861089127.1632240875)
* AccessKey
* SecretKey
* ApplicationId
 
A timeout is also needed to just in case there is ever an issue downloading the final file from the Jscrambler service.
 
An example of light obfuscation is presented below that would get updated in the configuration file:
```
              <add key="JavaScript.JscramblerEnable" value="true"/>
              <add key="Jscrambler.AccessKey" value="XXXXXXXXXXXXXXXXXXXXXXXXXXX"/>
              <add key="Jscrambler.SecretKey" value="XXXXXXXXXXXXXXXXXXXXXXXXXXX"/>
              <add key="Jscrambler.ApplicationId" value="XXXXXXXXXXXXXXXXXXXXXXXXXXX"/>
              <add key="Jscrambler.Query" value="mutation createApplicationProtection ($applicationId: String!, $data: ApplicationProtectionCreate) {createApplicationProtection (applicationId: $applicationId, data: $data) {_id}}"/>
              <add key="Jscrambler.TimeOut" value="2"/>
              <add key="Jscrambler.AreSubscribersOrdered" value="false"/>
              <add key="Jscrambler.Bail" value="true"/>
              <add key="Jscrambler.DebugMode" value="false"/>
              <add key="Jscrambler.ProfilingDataMode" value="off"/>
              <add key="Jscrambler.SourceMaps" value="false"/>
              <add key="Jscrambler.TolerateMinification" value="true"/>
              <add key="Jscrambler.UseAppClassification" value="false"/>
              <add key="Jscrambler.UseRecommendedOrder" value="true"/>
              <add key="Jscrambler.UseRecommendedOrder" value="true"/>
              
