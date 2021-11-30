using Newtonsoft.Json;
using System;

namespace InRule.CICD.Helpers
{
    public class SqlDatabaseHelper
    {
        private static readonly string moniker = "Sql";

        public static void WriteEvent(string eventType, object data)
        {
            WriteEvent(eventType, data, moniker);
        }
        public static void WriteEvent(string eventType, object data, string moniker)
        {
            string DatabaseConnectionString = SettingsManager.Get($"{moniker}.DatabaseConnectionString");
            try
            {
                if (!string.IsNullOrEmpty(DatabaseConnectionString))
                {
                    using (var connection = new System.Data.SqlClient.SqlConnection(DatabaseConnectionString))
                    {
                        connection.Open();

                        var query = $"INSERT INTO [dbo].[CatalogEvents] ([EventType] ,[EventData]) VALUES ('{eventType}' , '{JsonConvert.SerializeObject(data)}')";
                        using (var command = new System.Data.SqlClient.SqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.NotifyAsync($"Error writing {eventType} event out to database: {ex.Message}", "PUBLISH TO SQL", "Debug").Wait();
            }
        }
    }
}
