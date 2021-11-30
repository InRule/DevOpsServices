In the irCatalog service configuration file (Program Files (x86)\InRule\irServer\RepositoryService\IisService\Web.config), you will need to make 4 changes:


Just before the /system.serviceModel tag, add a new section for the extension with the following:
```html
	<extensions>
		<behaviorExtensions>
			<add name="ruleAppCheckinBehavior" type="CheckinRequestListener.RuleApplicationCheckinBehavior, CheckinRequestListener" />
		</behaviorExtensions>
	</extensions>
```

Add a new endpoint behavior to the system.serviceModel section after the /serviceBehaviors tag and before the /behaviors tag with the following:
```html
	<endpointBehaviors>
		<behavior name="RuleAppCheckin">
			<ruleAppCheckinBehavior />
		</behavior>
	</endpointBehaviors>
```

Find the system.serviceModel.Services.Service entry (there should only be one).  For each Endpoint entries inside that Service that are used for Catalog purposes (there should be only one or two), add the following property:
```html
behaviorConfiguration="RuleAppCheckin"
```
