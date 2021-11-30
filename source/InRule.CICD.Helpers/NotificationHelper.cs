using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace InRule.CICD.Helpers
{

    public class NotificationHelper
    {
        static string Channels = SettingsManager.Get("DebugNotifications");

        private enum NotificationChannel
        {
            Slack,
            Teams,
            Email,
            EventLog
        }

        //
        // Summary:
        //     Specifies the event type of an event log entry.
        public enum NotificationType
        {
            Error,
            Warning,
            Information,
            Debug
        }

        public static async Task NotifyAsync(string message, string prefix, string type)
        {
            if (string.IsNullOrEmpty(Channels))
                Channels = "EventLog";

            try
            {
                List<string> channels = Channels.Split(' ').ToList();

                foreach (var channel in channels)
                {
                    NotificationChannel channelType = new NotificationChannel();
                    string configType;

                    if (Enum.IsDefined(typeof(NotificationChannel), channel))
                        Enum.TryParse(channel, out channelType);
                    else
                    {
                        configType = SettingsManager.Get($"{channel}.Type");
                        if (Enum.IsDefined(typeof(NotificationChannel), configType))
                            Enum.TryParse(configType, out channelType);
                    }

                    switch (channelType)
                    {
                        case NotificationChannel.Slack:
                            //var slackHelper = new SlackHelper(channel);
                            //slackHelper.PostMarkdownMessage(message, $"{prefix} Inrule CI/CD {type} - ");
                            SlackHelper.PostMarkdownMessage(">" + message, $"{prefix} Inrule CI/CD ({type})\n", channel);
                            break;
                        case NotificationChannel.Teams:
                            TeamsHelper.PostSimpleMessage(message, $"<b>{prefix} Inrule CI/CD ({type})</b><br>", channel);
                            break;
                        case NotificationChannel.Email:
                            await SendGridHelper.SendEmail($"Inrule CI/CD {type}", message, string.Empty, channel);
                            break;
                        case NotificationChannel.EventLog:
                            EventLog.WriteEntry("Application", message, EventLogEntryType.Information);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", message + "\r\n\r\n" + ex.Message, EventLogEntryType.Error);
            }
        }
    }
}

namespace InRule.CICD.Helpers
{

}