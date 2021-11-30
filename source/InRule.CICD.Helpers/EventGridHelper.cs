using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InRule.CICD.Helpers
{
    public class EventGridHelper
    {
        private static readonly string moniker = "EventGrid";

        public static async Task PublishEventAsync(string eventType, object data)
        {
            await PublishEventAsync(eventType, data, moniker);
        }

        public static async Task PublishEventAsync(string eventType, object data, string moniker)
        {
            // To configure, create "Event Grid Partner Registration", then create "Event Grid Partner Namespace", then retrieve from Endpoint field
            string EventGridTopicEndpoint = SettingsManager.Get($"{moniker}.EventGridTopicEndpoint");
            // To configure, search for "Event Grid Topics", add one, then retrieve Key from Access Keys
            string EventGridTopicKey = SettingsManager.Get($"{moniker}.EventGridTopicKey");
            string NotificationChannel = SettingsManager.Get($"{moniker}.NotificationChannel");

            try
            {
                var channels = NotificationChannel.Split(' ');
                if (!string.IsNullOrEmpty(EventGridTopicKey) && !string.IsNullOrEmpty(EventGridTopicEndpoint))
                {
                    await NotificationHelper.NotifyAsync($"Publish Event to Azure Event Grid Topic Endpoint {EventGridTopicEndpoint}", "PUBLISH TO EVENT GRID", "Debug");
                    using (var client = new EventGridClient(new TopicCredentials(EventGridTopicKey)))
                    {
                        var events = new List<EventGridEvent>()
                        {
                            //https://docs.microsoft.com/en-us/dotnet/architecture/serverless/event-grid
                            new EventGridEvent()
                            {
                                Id = Guid.NewGuid().ToString(),
                                EventType = $"InRule.Repository.{eventType}",
                                Subject = eventType, //TODO: Consider including a config for EnvironmentName to include in the Subject
                                Data = Newtonsoft.Json.JsonConvert.SerializeObject(data),
                                EventTime = ((dynamic)data).UtcTimestamp,
                                DataVersion = "2.0"
                            }
                        };

                        client.PublishEventsAsync(new Uri(EventGridTopicEndpoint).Host, events).GetAwaiter().GetResult();
                        await NotificationHelper.NotifyAsync(eventType + " - " + Newtonsoft.Json.JsonConvert.SerializeObject(data), moniker, "Debug");
                    }
                }
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Error writing {eventType} event out to Event Grid: {ex.Message}", "PUBLISH TO EVENT GRID", "Debug");
            }
        }
    }
}
