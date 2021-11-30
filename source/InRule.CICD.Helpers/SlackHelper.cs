using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static InRule.CICD.Helpers.IHelper;

namespace InRule.CICD.Helpers
{
    public class SlackHelper : IHelper
    {
        private readonly string[] _slackWebHooks;

        public string Moniker { get; set; } = "Slack";
        public InRuleEventHelperType EventType { get; set; } = InRuleEventHelperType.Slack;

        public SlackHelper() : this("Slack")
        {
        }

        public SlackHelper(string moniker)
        {
            Moniker = moniker;
            _slackWebHooks = SettingsManager.Get($"{moniker}.SlackWebhookUrl").Split(' ');
        }
        public void PostSimpleMessage(string message, string messagePrefix, string moniker = "Slack")
        {

            if (_slackWebHooks.Length == 0)
                return;

            foreach (var slackWebHook in _slackWebHooks)
            {
                var slackClient = new SlackClient(slackWebHook.Trim());

                var slackMessage = new SlackMessage
                {
                    Text = messagePrefix + message,
                    IconEmoji = Emoji.Ghost,
                };

                slackClient.Post(slackMessage);
                //}
            }
        }

        public void PostMarkdownMessage(string message, string messagePrefix)
        {
            if (_slackWebHooks.Length == 0)
                return;

            foreach (var slackWebHook in _slackWebHooks)
            {
                var slackClient = new SlackClient(slackWebHook.Trim());
                var slackMessage = new SlackMessage
                {
                    Text = messagePrefix + message,
                    IconEmoji = Emoji.Ghost
                };

                slackMessage.Blocks = new List<Block>
                    {
                        new Section
                        {
                            Text = new TextObject(messagePrefix + message)
                            {
                                Type = TextObject.TextType.Markdown
                            }
                        }
                    };

                var divider = new Divider();
                slackMessage.Blocks.Add(divider);

                slackClient.Post(slackMessage);
            }
        }

        public static void PostMarkdownMessage(string message, string messagePrefix, string handlerMoniker)
        {
            if (SettingsManager.GetHandlerType(handlerMoniker) == InRuleEventHelperType.Slack)
            {
                string[] slackWebHooks = SettingsManager.Get($"{handlerMoniker}.SlackWebhookUrl").Split(' ');

                if (slackWebHooks.Length == 0)
                    return;

                foreach (var slackWebHook in slackWebHooks)
                {
                    PostMarkdownMessageForWebHook(message, messagePrefix, slackWebHook);
                }
            }
        }

        private static void PostMarkdownMessageForWebHook(string message, string messagePrefix, string slackWebHook)
        {
            if (slackWebHook.Length == 0)
                return;

            var slackClient = new SlackClient(slackWebHook.Trim());
            var fixedMessage = messagePrefix + "\n" + (messagePrefix.Length + message.Length + 1 > 3000 ? ">..." + message.Substring(message.Length - 3000 + messagePrefix.Length + 6) : message);

            var slackMessage = new SlackMessage
            {
                Text = fixedMessage,
                IconEmoji = Emoji.Ghost
            };

            slackMessage.Blocks = new List<Block>
                {
                    new Section
                    {
                        Text = new TextObject(fixedMessage)
                        {
                            Type = TextObject.TextType.Markdown
                        }
                    }
                };

            var divider = new Divider();
            slackMessage.Blocks.Add(divider);

            slackClient.Post(slackMessage);
        }

        public void PostMessageWithDownloadButton(string message, string buttonText, string downloadUrl, string messagePrefix)
        {
            if (_slackWebHooks.Length == 0)
                return;

            foreach (var slackWebHook in _slackWebHooks)
            {
                PostMessageWithDownloadButton(message, buttonText, downloadUrl, messagePrefix, slackWebHook);
            }
        }

        public static void PostMessageWithDownloadButton(string message, string buttonText, string downloadUrl, string messagePrefix, string handlerMoniker)
        {
            if (SettingsManager.GetHandlerType(handlerMoniker) == InRuleEventHelperType.Slack)
            {
                string[] slackWebHooks = SettingsManager.Get($"{handlerMoniker}.SlackWebhookUrl").Split(' ');

                if (slackWebHooks.Length == 0)
                    return;

                PostMessageWithDownloadButton(message, buttonText, downloadUrl, messagePrefix, slackWebHooks);
            }
        }

        public static void PostMessageWithDownloadButton(string message, string buttonText, string downloadUrl, string messagePrefix, string[] slackWebHooks)
        {
            //var hooks = slackWebHooks.Split(' ');
            if (slackWebHooks.Length == 0)
                return;

            foreach (var slackWebHook in slackWebHooks)
            {
                PostMessageWithDownloadButtonForWebHook(message, buttonText, downloadUrl, messagePrefix, slackWebHook);
            }
        }


        private static void PostMessageWithDownloadButtonForWebHook(string message, string buttonText, string downloadUrl, string messagePrefix, string slackWebHook)
        {
            if (slackWebHook.Length == 0)
                return;

            var slackClient = new SlackClient(slackWebHook.Trim());
            var slackMessage = new SlackMessage
            {
                Text = messagePrefix + message,
                IconEmoji = Emoji.Ghost
            };

            var reportFile = new Section
            {
                Text = new TextObject(messagePrefix + message)
                {
                    Type = TextObject.TextType.Markdown
                },
                Accessory = new Button()
                {
                    Text = new TextObject(buttonText)
                    {
                        Type = TextObject.TextType.PlainText
                    },
                    Url = downloadUrl,
                    ActionId = "button-action"
                }
            };

            slackMessage.Blocks = new List<Block>
                {
                    new Divider{}
                };

            slackMessage.Blocks.Add(reportFile);

            var divider = new Divider();
            slackMessage.Blocks.Add(divider);

            slackClient.Post(slackMessage);
        }

        public static async Task SendEventToSlackAsync(string eventType, object data, string messagePrefix, string handlerMoniker)
        {
            try
            {
                var map = data as IDictionary<string, object>;

                var textBody = string.Empty;
                string repositoryUri = ((dynamic)data).RepositoryUri;
                string repositoryManagerUri = repositoryUri.Replace(repositoryUri.Substring(repositoryUri.LastIndexOf('/')), "/InRuleCatalogManager"); //, repositoryUri.LastIndexOf('/') - 1)), "/InRuleCatalogManager");

                if (map.ContainsKey("OperationName"))
                    textBody = $"*{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}*\n";

                textBody += $">*Catalog:* {((dynamic)data).RepositoryUri}\n";

                textBody += $">*Catalog Manager (likely location):* {repositoryManagerUri}\n";

                if (map.ContainsKey("Name"))
                    textBody += $">*Rule application:* {((dynamic)data).Name}\n";

                if (map.ContainsKey("RuleAppRevision"))
                    textBody += $">*Revision:* {((dynamic)data).RuleAppRevision}\n";

                if (map.ContainsKey("Label"))
                    textBody += $">*Label:* { ((dynamic)data).Label}\n";

                if (map.ContainsKey("Comment"))
                    textBody += $">*Comment:* { ((dynamic)data).Comment}\n";


                //SlackHelper.PostMessageHeaderBody($"{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}", textBody);
                SlackHelper.PostMarkdownMessage(textBody, messagePrefix, handlerMoniker);
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Error writing {eventType} event out to Slack: {ex.Message}", "PUBLISH TO SLACK", "Debug");
            }
        }
    }
}