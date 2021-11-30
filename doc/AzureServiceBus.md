### Distributing Catalog Events to an Azure Service Bus Topic 

As stated in Microsoft [documentation about creating and using Azure Service Bus topics](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quickstart-topics-subscriptions-portal), _Service Bus topics and subscriptions support a publish/subscribe messaging communication model. When using topics and subscriptions, components of a distributed application do not communicate directly with each other; instead they exchange messages via a topic, which acts as an intermediary._

_In contrast with Service Bus queues, in which each message is processed by a single consumer, topics and subscriptions provide a one-to-many form of communication, using a publish/subscribe pattern. It is possible to register multiple subscriptions to a topic. When a message is sent to a topic, it is then made available to each subscription to handle/process independently. A subscription to a topic resembles a virtual queue that receives copies of the messages that were sent to the topic. You can optionally register filter rules for a topic on a per-subscription basis, which allows you to filter or restrict which messages to a topic are received by which topic subscriptions._

_Service Bus topics and subscriptions enable you to scale to process a large number of messages across a large number of users and applications._

**Before using InRule® CI/CD with Azure Service Bus topics, a number of items have to be first created under the Azure portal**, as per the steps described on page [Use the Azure portal to create a Service Bus topic and subscriptions to the topic](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quickstart-topics-subscriptions-portal).  Depending on the specifics of the intended end-to-end solution, a subscription to the topic and further connected functionality would have to be created in Azure before leveraging the irCatalog service events. 

For the InRule CI/CD configuration, we need the SB namespace connection string and the name of the topic just created.  

Sending InRule catalog events to an Azure Service Bus topic is not an action that produces an immediate outcome, at least not one that can be configured in InRule CI/CD.  By default, the effects of this integration can be seen in InRule CI/CD only as debug notifications, on the channels configured for this purpose, like in the Slack example below:


![Example debug Service Bus notification in Slack](../images/Sample9-ServiceBusSlack.PNG)

---
#### Configuration

In order to enable the distribution of catalog events to an Azure Service Bus topic, the minimal configuration can be seen in the [sample configuration file](../config/InRuleCICD_ServiceBus.config), which is **applicable for a local deployment**.  **For the Azure CI/CD app service**, the configuration follows the format in the [starter cloud config file](../config/InRule.CICD.Runtime.Service.config.json). 

```
<appSettings>
  ...
  <add key="CatalogEvents" value="CheckinRuleApp"/>

  <add key="OnCheckinRuleApp" value="ServiceBus"/>

  <add key="ServiceBus.ServiceBusConnectionString" value="Endpoint=sb://*******.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=***********************"/>
  <add key="ServiceBus.ServiceBusTopic" value="inrulecheckin"/>
</appSettings>
```

|Configuration Key | Comments
--- | ---
|ServiceBus.**ServiceBusConnectionString**| The connection string for the Azure Service Bus namespace where InRule CI/CD will post the catalog events.
|ServiceBus.**ServiceBusTopic**| A simple string that is the chosen name for the topic to which InRule CI/CD will post the events.
