using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace InRule.CICD.Helpers
{
    public class AzureAppInsightsHelper
    {
        private static readonly string moniker = "AppInsights";
        private static TelemetryClient _appInsights = null;

        public static void PublishEventToAppInsights(string eventType, object data)
        {
            PublishEventToAppInsights(eventType, data, moniker);
        }

        public static void PublishEventToAppInsights(string eventType, object data, string moniker)
        {
            string AppInsightsInstrumentationKey = SettingsManager.Get($"{moniker}.AppInsightsInstrumentationKey");
            try
            {
                if (!string.IsNullOrEmpty(AppInsightsInstrumentationKey))
                {
                    if (_appInsights == null)
                    {
                        var configuration = TelemetryConfiguration.Active;
                        if (string.IsNullOrEmpty(configuration.InstrumentationKey))
                            configuration.InstrumentationKey = AppInsightsInstrumentationKey;
                        _appInsights = new TelemetryClient(configuration);
                    }

                    var eventData = (dynamic)data;
                    var requestData = new RequestTelemetry(eventData.OperationName, eventData.UtcTimestamp, TimeSpan.FromMilliseconds(eventData.ProcessingTimeInMs), "200", true);

                    // This logic allows us to get a key/value pair dictionary from the dynamic object
                    var json = JsonConvert.SerializeObject(eventData);
                    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    foreach (var member in dictionary)
                        requestData.Properties.Add(member.Key, member.Value);

                    _appInsights.TrackRequest(requestData);
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.NotifyAsync($"Error writing {eventType} event out to AppInsights: {ex.Message}", "APPINSIGHTS", "Debug").Wait();
            }
        }
    }
}
