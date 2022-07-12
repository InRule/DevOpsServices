using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InRule.DevOps.Helpers.Models;
using InRule.Repository;
using InRule.Repository.Infos;
using InRule.Repository.RuleElements;

namespace InRule.DevOps.Helpers;

/// <summary>
/// Maps all fields used in a rule set to a SQL database
/// </summary>
public class RuleSetDbMapper
{
    #region ConfigParams

    private static readonly string moniker = "RuleSetDbMapper";
    public static string Prefix = "RuleSetDbMapper - ";

    #endregion
    public static async Task RunRuleSetDbMapper(RuleApplicationDef ruleAppDef, dynamic eventData)
    {
        var connectionString = SettingsManager.Get($"{moniker}.ConnectionString");
        var destinationTableName = SettingsManager.Get($"{moniker}.DestinationTableName");
        var inLineTableName = SettingsManager.Get($"{moniker}.InLineTableName");
        var filterByLabels = SettingsManager.Get($"{moniker}.FilterByLabels");
        if (connectionString.Length == 0 || destinationTableName.Length == 0 || inLineTableName.Length == 0)
            return;
        
        try
        {
            var label = eventData.Label;
            if (filterByLabels is not null && filterByLabels.Length > 0)
            {
                if (!filterByLabels.Contains(label)) return;
            }
            var inRuleDevOpsHelper = (DataElementDef)ruleAppDef.DataElements[inLineTableName];
            if (inRuleDevOpsHelper is not TableDef tableDef)
            {
                return;
            }
            var network = DefUsageNetwork.Create(ruleAppDef);
            RuleSetList ruleSetList = new()
            {
                RuleSets = new List<RuleSetMap>()
            };
            foreach (EntityDef entity in ruleAppDef.Entities)
            {
                var fields = entity.GetAllFields();
                List<string> fieldBackendNames = new();
                fieldBackendNames.AddRange(fields.Select(field => field.AuthoringContextName));
                foreach (var ruleSet in entity.GetRuleSets().Where(rs => rs.FireMode == RuleSetFireMode.Explicit))
                {
                    RuleSetMap ruleSetMap = new()
                    {
                        RuleSetName = ruleSet.AuthoringContextName,
                        Fields = new List<string>()
                    };
                    var usages = network.GetDefUsages(ruleSet.Guid, true);
                    foreach (var usage in usages)
                    {
                        if (usage.UsageType != DefUsageType.Consumes) continue;
                        var consumedField = usage.TraceStack.Substring(usage.TraceStack.IndexOf("[Consumes]", StringComparison.Ordinal) + 11);
                        if (!fieldBackendNames.Any(s => consumedField.Contains(s))) continue;
                        if (consumedField.Contains(" "))
                        {
                            consumedField = consumedField.Substring(0, consumedField.IndexOf(" ", StringComparison.Ordinal));
                        }
                        ruleSetMap.Fields.Add(consumedField);
                    }
                    if (ruleSetMap.Fields.Count <= 0) continue;
                    ruleSetList.RuleSets.Add(ruleSetMap);
                }
            }

            var clientId = ""; var tableName = "";
            var columnNames = (from object? col in tableDef.TableSettings.InlineDataTable.Columns select col.ToString()).ToList()!;
            foreach (DataRow dr in tableDef.TableSettings.InlineDataTable.Rows)
            {
                foreach (var colName in columnNames)
                {
                    switch (colName)
                    {
                        case "ClientId":
                            clientId = dr[colName].ToString();
                            break;
                        case "TableName":
                            tableName = dr[colName].ToString();
                            break;
                    }
                }
            }

            var ruleSetMapOutput = new RuleSetMapOutput()
            {
                RuleSetMapOutputFields = new List<RuleSetMapOutputFields>()
            };
            var i = 1;
            foreach (var outputFields in from item in ruleSetList.RuleSets
                                       from field in item.Fields
                                       select new RuleSetMapOutputFields()
                                       {
                                           Id = i++.ToString(),
                                           ClientId = clientId,
                                           RuleSetName = item.RuleSetName,
                                           FieldName = field,
                                           TableName = tableName
                                       })
            {
                ruleSetMapOutput.RuleSetMapOutputFields.Add(outputFields);
            }

            var deleteValues = $"DELETE FROM RuleSetMapper WHERE TableName = '{tableName}'";
            using (var connectionT = new SqlConnection(connectionString))
            {
                connectionT.Open();
                using (var command = new SqlCommand(deleteValues, connectionT))
                using (var reader = command.ExecuteReader())
                {
                }
            }

            if (string.IsNullOrEmpty(connectionString)) return;
            var ruleSetMapOutputFields = ConvertToDataTable(ruleSetMapOutput.RuleSetMapOutputFields);
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.DestinationTableName = destinationTableName;
            bulkCopy.BatchSize = ruleSetMapOutputFields.Rows.Count;
            bulkCopy.WriteToServer(ruleSetMapOutputFields);
            bulkCopy.Close();
            
            NotificationHelper.NotifyAsync($"Successfully updated database with new rulesets.", Prefix, "Debug");
        }

        catch (Exception ex)
        {
            await NotificationHelper.NotifyAsync($"Error mapping fields from rulesets: {ex.Message}", Prefix, "Debug");
        }

    }
    public static DataTable ConvertToDataTable<T>(IList<T> data)
    {
        var properties = TypeDescriptor.GetProperties(typeof(T));
        var table = new DataTable();
        foreach (PropertyDescriptor prop in properties)
            table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        foreach (var item in data)
        {
            var row = table.NewRow();
            foreach (PropertyDescriptor prop in properties)
                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
            table.Rows.Add(row);
        }
        return table;
    }
}


