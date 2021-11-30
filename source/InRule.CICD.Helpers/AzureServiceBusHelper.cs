using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Text;

namespace InRule.CICD.Helpers
{
    public class AzureServiceBusHelper
    {
        private static readonly string moniker = "ServiceBus";
        static readonly string Prefix = "AZURE SERVICE BUS";

        public static void SendMessage(string message)
        {
            SendMessageAsync(message, moniker);
        }

        public static async void SendMessageAsync(string message, string moniker)
        {
            string ConnectionString = SettingsManager.Get($"{moniker}.ServiceBusConnectionString");
            string Topic = SettingsManager.Get($"{moniker}.ServiceBusTopic");

            if (ConnectionString.Length == 0 || Topic.Length == 0)
                return;

            // create a Service Bus client 
            await using (ServiceBusClient client = new ServiceBusClient(ConnectionString))
            {
                var messageTopic = new Message(Encoding.UTF8.GetBytes($"InRule CI/CD - {message}"));
                ITopicClient topicClient = new TopicClient(ConnectionString, Topic);
                await topicClient.SendAsync(messageTopic);

                await NotificationHelper.NotifyAsync(message, Prefix, "Debug");
            }
        }
    }
}