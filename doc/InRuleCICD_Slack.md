### Integrating InRule CI/CD with Slack

As per Slack documentation, incoming Webhooks are a simple way to post messages from apps into Slack. Creating an Incoming Webhook gives you a unique URL to which you send a JSON payload with the message text and some options. You can use all the usual formatting and layout blocks with incoming webhooks to make the messages stand out.

It takes only a few configuration items to start using the CI/CD framework with Slack, like in [this configuration file example](../config/InRuleCICD_Slack.config).

Before using Slack with the InRule® CI/CD framework, a Slack app has to be created and enabled for webhooks, following the steps at [Incoming Webhooks for Slack](https://slack.com/intl/en-ro/help/articles/115005265063-Incoming-webhooks-for-Slack).

#### Set up incoming webhooks

 1. [Create a new Slack app](https://api.slack.com/apps/new) in the workspace where you want to post messages.
 2. From the Features page toggle **Activate Incoming Webhooks** on.
 3. Click **Add New Webhook to Workspace**.
 4. Pick a channel that the app will post to, then click **Authorize**.
 5. Use your [Incoming Webhook URL](https://api.slack.com/incoming-webhooks#posting_with_webhooks) to post a message to Slack. 

Incoming webhooks are a simple way to post messages from external sources into Slack. They make use of normal HTTP requests with a JSON payload, which includes the message and a few other optional details. You can include message attachments to display richly-formatted messages.

Each time your app is installed, a new Webhook URL will be generated.

If you deactivate incoming webhooks, new webhook URLs will not be generated when your app is installed to your team. If you’d like to remove access to existing webhook URLs, you will need to Revoke All OAuth Tokens.

#### InRule CI/CD configuration for Slack integration

Once the Slack application is created, incoming webhooks enabled, and at least one webhook added to the application, we are ready to set up the configuration for where the CI/CD framework can send Slack messages.  The data sent to Slack by InRule CI/CD can be "normal" notifications regarding various CI/CD steps or detailed debug messages with the progress of an operation or any raised errors.

The webhook URL's are the only information the CI/CD framework requires before it can start sending notifications to a Slack channel.  The URL's must be listed separated by a space, under the **Slack.SlackWebhookUrl configuration key**.  Here is an example, with masked values, which is **applicable for a local deployment**.  **For the Azure CI/CD app service**, the configuration follows the format in the [starter cloud config file](../config/InRule.CICD.Runtime.Service.config.json).

```
<add key="Slack.SlackWebhookUrl" value="https://hooks.slack.com/services/xxxxxxxxx/xxxxxxxx/xxxxxxxxxxxxxxxxxxx https://hooks.slack.com/services/yyyyyyyyy/yyyyyyyy/yyyyyyyyyyyyyyyyyyy"/>
```

The "Slack." prefix makes the entry above the default Slack configuration used if no other ones are defined.  If different channels or combinations of webhooks are needed to respond to specific needs, any number of uniquely identified Slack configurations can be added and referenced within the configuration.

