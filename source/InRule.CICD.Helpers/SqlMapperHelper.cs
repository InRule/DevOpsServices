using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InRule.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InRule.CICD.Helpers
{
    public static class SqlMapperHelper
    {
        private static readonly string moniker = "SqlRuleSetMapper";
        public static string Prefix = "SqlRuleSetMapper - ";
        public static async Task MapToDatabase(ExpandoObject EventDataSource, RuleApplicationDef RuleAppDef)
        {
            var databaseConnectionString = SettingsManager.Get($"{moniker}.DatabaseConnectionString");
            try
            {
                if (!string.IsNullOrEmpty(databaseConnectionString))
                {
                    var ruleAppDef = RuleAppDef;
                    var ruleAppName = RuleAppDef.Name;
                    var revision = RuleAppDef.Revision;

                    var RuleAppName = SettingsManager.Get($"{moniker}.RuleAppNameColumnName");
                    var Revision = SettingsManager.Get($"{moniker}.RevisionColumnName");
                    var RuleSets = SettingsManager.Get($"{moniker}.RuleSetsColumnName");
                    var dataBaseName = SettingsManager.Get($"{moniker}.DataBaseName");
                    var format = SettingsManager.Get($"{moniker}.Format");

                    var ruleSets = string.Empty;

                    foreach (var entity in RuleAppDef.Entities)
                    {
                        var e = (dynamic) entity;
                        var ruleElements = e.RuleElements;
                        foreach (var ruleElement in ruleElements)
                        {
                            var r = (dynamic) ruleElement;
                            // AuthoringElementPath = entity.ruleset
                            // AuthoringContextName = ruleset
                            var ruleOutputFormat = format switch
                            {
                                "entity.ruleset" => ruleSets += r.AuthoringElementPath + ",",
                                "ruleset" => ruleSets += r.AuthoringContextName + ",",
                                _ => ruleSets += r.AuthoringElementPath + ","
                            };
                        }
                    }

                    ruleSets = ruleSets.Substring(0, ruleSets.Length - 1);
                    var query = $"SELECT {RuleSets} FROM [dbo].[{dataBaseName}] WHERE {RuleAppName} = '{ruleAppName}';";
                    var updateCheck = true;
                    using (var connection = new SqlConnection(databaseConnectionString))
                    {
                        connection.Open();
                        using var command = new SqlCommand(query, connection);
                        SqlDataReader reader = command.ExecuteReader();
                        try
                        {
                            while (reader.Read())
                            {
                                if (reader[$"{RuleSets}"] != DBNull.Value)
                                    updateCheck = true;
                                else updateCheck = false;
                            }
                        }
                        catch (Exception e)
                        {
                            NotificationHelper.NotifyAsync($"SQL query error: {e.Message}", Prefix,"Debug");
                        }
                        finally { reader.Close(); }
                    }

                    if (updateCheck)
                    {
                        using var connection = new SqlConnection(databaseConnectionString);
                        var update = $"UPDATE dbo.{dataBaseName} SET {Revision} = {revision}, {RuleSets} = '{ruleSets}' WHERE {RuleAppName} = '{ruleAppName}';";
                        connection.Open();
                        using var com = new SqlCommand(update, connection);
                        SqlDataReader read = com.ExecuteReader();
                        try
                        {
                            while (read.Read()) { }
                        }
                        catch (Exception e)
                        {
                            NotificationHelper.NotifyAsync($"SQL update error: {e.Message}", Prefix, "Debug");
                        }
                        finally
                        {
                            NotificationHelper.NotifyAsync($"SQL database updated successfully with rulesets.", Prefix, "Debug");
                            read.Close();
                        }
                    }
                    else
                    {
                        using var connection = new SqlConnection(databaseConnectionString);
                        var insert = $"INSERT INTO dbo.{dataBaseName}({RuleAppName}, {Revision}, {RuleSets}) VALUES('{ruleAppName}', '{revision}', '{ruleSets}');";
                        connection.Open();
                        using var com = new SqlCommand(insert, connection);
                        SqlDataReader read = com.ExecuteReader();
                        try
                        {
                            while (read.Read()) { }
                        }
                        catch (Exception e)
                        {
                            NotificationHelper.NotifyAsync($"SQL insert error: {e.Message}", Prefix, "Debug");
                        }
                        finally
                        {
                            NotificationHelper.NotifyAsync($"SQL inserted successfully.", Prefix, "Debug");
                            read.Close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NotificationHelper.NotifyAsync($"Error writing event out to database: {e.Message}", Prefix, "Debug");
            }
        }
    }
}
