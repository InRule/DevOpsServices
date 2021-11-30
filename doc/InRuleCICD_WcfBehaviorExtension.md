### Configuring irCatalog WCF Service for the Event Listener

In order to intercept irCatalog® service events, we created a component that must reside alongside the other irCatalog binaries, in service's bin folder. Assembly CheckinRequestListener.dll comes with a number of dependent files and a corresponding configuration file that drives runtime behavior. Also, irCatalog service's web.config file requires a number of changes before the WCF behavior extension becomes active.

The InRule® irCatalog service is a Windows Communication Foundation (WCF) service application. It is possible to expand and modify its built-in behavior with custom behavior extension running alongside the standard WCF behavior elements.

There are a number of **required manual web.config modifications** necessary for activating the InRule CI/CD tools on an instance of irCatalog service.

- The **behaviorExtensions** section defines the element that can then be used in configuration.
- Then, add an entry under **endpointBehaviors** and associate it with the endpoints defined for the service, using its name and **behaviorConfiguration** attributes.
- The same **endpointBehaviors** item must have a child node matching the name of the **behaviorExtensions** added first.

```xml
  <services>
   <service name="InRule.Repository.Service.RepositoryService" behaviorConfiguration="repositoryServiceBehavior">
    <endpoint address="" binding="wsHttpBinding" bindingConfiguration="WSHttpBinding" contract="InRule.Repository.Service.ICatalogServiceContract" behaviorConfiguration="RuleAppCheckin" />
    <endpoint address="core" binding="basicHttpBinding" bindingConfiguration="BasicHttpBinding" contract="InRule.Repository.Service.ICatalogServiceContract" behaviorConfiguration="RuleAppCheckin" />
   </service>
  </services>
  ...
  <system.serviceModel>
    ...
    <behaviors>
      ...
      <endpointBehaviors>
        <behavior name="RuleAppCheckin">
          <ruleAppCheckinBehavior />
        </behavior>
      </endpointBehaviors>
    </behaviors>
    <extensions>
      <behaviorExtensions>
        <add name="ruleAppCheckinBehavior" type="CheckinRequestListener.RuleApplicationCheckinBehavior, CheckinRequestListener" />
      </behaviorExtensions>
    </extensions>
  </system.serviceModel>
 ```

**Only for the standalone InRule CI/CD deployment**, these binding redirect entries must be added to irCatalog service's web.config file, under runtime -> assemblyBinding", while keeping the existing redirects in place:

```
 <runtime>
  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
   ...
   <dependentAssembly>
    <assemblyIdentity name="Microsoft.Bcl.AsyncInterfaces" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-5.0.0.0" newVersion="5.0.0.0" />
   </dependentAssembly>
   <dependentAssembly>
    <assemblyIdentity name="System.Threading.Tasks.Extensions" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-4.2.0.1" newVersion="4.2.0.1" />
   </dependentAssembly>
   <dependentAssembly>
    <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-5.0.0.0" newVersion="5.0.0.0" />
   </dependentAssembly>
   <dependentAssembly>
    <assemblyIdentity name="System.Diagnostics.DiagnosticSource" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-5.0.0.1" newVersion="5.0.0.1" />
   </dependentAssembly>			
   <dependentAssembly>
    <assemblyIdentity name="Azure.Core" publicKeyToken="92742159e12e44c8" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-1.8.1.0" newVersion="1.10.0.0" />
   </dependentAssembly>		 
  </assemblyBinding>
 </runtime>
 ```
