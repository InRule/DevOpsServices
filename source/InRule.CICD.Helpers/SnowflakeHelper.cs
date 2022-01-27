using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InRule.Repository;
using InRule.Runtime;
using Snowflake.Data.Client;

namespace InRule.CICD.Helpers
{
    public class SnowflakeHelper
    {
        #region ConfigParams 
        private static readonly string moniker = "Snowflake";
        public static string Prefix = "Snowflake - ";
        #endregion

        public static async Task CallSnowflakeHelper(string jsContent, RuleApplicationDef ruleApplicationRef)
        {
            try
            {
                string account = SettingsManager.Get($"{moniker}.Account");
                string user = SettingsManager.Get($"{moniker}.User");
                string password = SettingsManager.Get($"{moniker}.Password");
                string host = SettingsManager.Get($"{moniker}.Host");

                if (account.Length == 0 || user.Length == 0 || password.Length == 0 || host.Length == 0)
                    return;

                string warehouse = "";
                string database = "";
                string schema = "";
                string snowflakeConnectionTable = "";
                string ruleAppName = ruleApplicationRef.Name;
                string type = "";
                int revision = 0;
                if (SettingsManager.Get($"{moniker}.Warehouse") != null)
                { warehouse = SettingsManager.Get($"{moniker}.Warehouse"); }
                if (SettingsManager.Get($"{moniker}.Database") != null)
                { database = SettingsManager.Get($"{moniker}.Database"); }
                if (SettingsManager.Get($"{moniker}.Schema") != null)
                { schema = SettingsManager.Get($"{moniker}.Schema"); }
                if (SettingsManager.Get($"{moniker}.ConnectionTableName") != null)
                { snowflakeConnectionTable = SettingsManager.Get($"{moniker}.ConnectionTableName"); }
                if (SettingsManager.Get($"{moniker}.Name") != null)
                { type = SettingsManager.Get($"{moniker}.Type").ToLower(); }
                if (SettingsManager.Get($"{moniker}.RevisionTracking") != null)
                {
                    if(SettingsManager.Get($"{moniker}.RevisionTracking").ToLower() == "true")
                    {
                        revision = ruleApplicationRef.Revision;
                        ruleAppName = ruleAppName + "_" + revision.ToString();
                    }
                }

                string connectionString = $"account={account};user={user};password={password};host={host}";

                if (database != null) connectionString += $";DB={database}";
                if (schema != null) connectionString += $";schema={schema}";
                if (warehouse != null) connectionString += $";warehouse={warehouse}";
                
                try { 
                    using (IDbConnection conn = new SnowflakeDbConnection())
                    {
                        conn.ConnectionString = connectionString;
                        conn.Open();
                    }
                    await NotificationHelper.NotifyAsync($"Snowflake connection successful. Creating {type} named {ruleAppName}...", Prefix, "Debug");
                }
                catch (Exception ex) {
                    await NotificationHelper.NotifyAsync($"Error connecting to Snowflake: {ex.Message}", Prefix, "Debug");
                }
                
                if (ruleAppName != "" && type != "")
                {
                    switch (type)
                    {
                        case "function":
                            await UpdateFunction(connectionString, ruleAppName, jsContent);
                            break;

                        case "procedure":
                            await UpdateStoredProc(connectionString, ruleAppName, jsContent);
                            break;
                    }
                }
                else
                {
                    await NotificationHelper.NotifyAsync($"Missing type or name of function/stored procedure.", Prefix, "Debug");
                }
                return;
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Snowflake error: {ex.Message}", Prefix, "Debug");
            }
        }


        public static async Task UpdateStoredProc(string connectionString, string ruleAppName, string jsPackage)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine($"CREATE OR REPLACE PROCEDURE {ruleAppName}(rootEntityTypeName string, rootEntity variant, ruleSetName string)");
            query.AppendLine("RETURNS variant");
            query.AppendLine("LANGUAGE JAVASCRIPT");
            query.AppendLine("as $$");
            query.AppendLine(jsPackage);
            query.AppendLine("var rootEntityTypeName = ROOTENTITYTYPENAME");
            query.AppendLine("var rootEntity = ROOTENTITY");
            query.AppendLine("var ruleSetName = RULESETNAME");
            query.AppendLine("var session = inrule.createRuleSession();");
            query.AppendLine("var entity = session.createEntity(rootEntityTypeName, rootEntity);");
            query.AppendLine("if(ruleSetName) { entity.executeRuleSet(ruleSetName, [], function(log) {}); }");
            query.AppendLine("else { session.applyRules(function(log) {}); }");
            query.AppendLine("return rootEntity;");
            query.AppendLine("$$");
            query.AppendLine(";");
            await NotificationHelper.NotifyAsync($"query {query}", Prefix, "Debug");
            try {
                await ExecuteSnowflakeQuery(connectionString, query.ToString(), r => r.GetString(0));
                await NotificationHelper.NotifyAsync($"Procedure named {ruleAppName} was successfully created.", Prefix, "Debug");
            }
            catch (Exception ex) {
                await NotificationHelper.NotifyAsync($"Snowflake execution error: {ex.Message}", Prefix, "Debug");
            }
        }
        public static async Task UpdateFunction(string connectionString, string ruleAppName, string jsPackage)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine($"CREATE OR REPLACE FUNCTION {ruleAppName}(rootEntityTypeName string, rootEntity variant, ruleSetName string)");
            query.AppendLine("RETURNS VARIANT");
            query.AppendLine("LANGUAGE JAVASCRIPT");
            query.AppendLine("as $$");
            query.AppendLine(jsPackage);
            query.AppendLine("var rootEntityTypeName = ROOTENTITYTYPENAME");
            query.AppendLine("var rootEntity = ROOTENTITY");
            query.AppendLine("var ruleSetName = RULESETNAME");
            query.AppendLine("var session = inrule.createRuleSession();");
            query.AppendLine("var entity = session.createEntity(rootEntityTypeName, rootEntity);");
            query.AppendLine("if(ruleSetName) { entity.executeRuleSet(ruleSetName, [], function(log) {}); }");
            query.AppendLine("else { session.applyRules(function(log) {}); }");
            query.AppendLine("return rootEntity;");
            query.AppendLine("$$");
            query.AppendLine(";");
            try {
                await ExecuteSnowflakeQuery(connectionString, query.ToString(), r => r.GetString(0));
                await NotificationHelper.NotifyAsync($"Function named {ruleAppName} was successfully created.", Prefix, "Debug");
            }
            catch (Exception ex) {
                await NotificationHelper.NotifyAsync($"Snowflake execution error: {ex.Message}", Prefix, "Debug");
            }
        }

        public static async Task ExecuteSnowflakeQuery<T>(string connectionString, string query, Func<IDataReader, T> memberConstructor)
        {
            var results = new List<T>();

            //https://github.com/snowflakedb/snowflake-connector-net
            using IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connectionString;
            conn.Open();

            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = query;
            IDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                results.Add(memberConstructor(reader));
            }
            conn.Close();
        }
    }
}
