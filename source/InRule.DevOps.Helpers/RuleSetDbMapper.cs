using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using InRule.Repository;
using InRule.Repository.Infos;
using SdkRunner573.Models;

namespace InRule.DevOps.Helpers;

/// <summary>
/// Maps all fields used in a rule set to a SQL database
/// </summary>
public class RuleSetDbMapper
{
    #region ConfigParams

    private const string moniker = "RuleSetDbMapper";
    public static string Prefix = "RuleSetDbMapper - ";

    #endregion
    public static async Task RunRuleSetDbMapper(RuleApplicationDef ruleAppDef, dynamic eventData)
    {
        var connectionString = SettingsManager.Get($"{moniker}.ConnectionString");
        var destinationTableName = SettingsManager.Get($"{moniker}.DestinationTableName");
        var filterByLabels = SettingsManager.Get($"{moniker}.FilterByLabels");
        if (connectionString.Length == 0 || destinationTableName.Length == 0)
            return;
        
        try
        {
            if (filterByLabels is not null && filterByLabels.Length > 0)
                if (!filterByLabels.Contains(eventData.Label)) return;

            var network = DefUsageNetwork.Create(ruleAppDef);
            List<RuleSetMap> ruleSets = new(); List<EntityRuleSet> entityRuleSets = new();
            foreach (EntityDef entity in ruleAppDef.Entities)
            {
                EntityRuleSet entityRuleSet = new()
                {
                    FieldBackendNames = new List<string>(),
                    EntityName = entity.AuthoringContextName
                };
                var fields = entity.GetAllFields();
                entityRuleSet.FieldBackendNames.AddRange(fields.Select(field => field.AuthoringContextName));
                entityRuleSets.Add(entityRuleSet);
            }
            foreach (EntityDef entity in ruleAppDef.Entities)
            {
                foreach (var ruleSet in entity.GetAllRuleSets())
                {
                    RuleSetMap ruleSetMap = new()
                    {
                        RuleSetName = ruleSet.AuthoringContextName,
                        EntityContext = entity.AuthoringContextName,
                        Fields = new List<Fields>()
                    };
                    var usages = network.GetDefUsages(ruleSet.Guid, true);
                    foreach (var usage in usages)
                    {
                        if (usage.UsageType != DefUsageType.Consumes) continue;
                        var consumedField =
                            usage.TraceStack.Substring(usage.TraceStack.IndexOf("[Consumes]", StringComparison.Ordinal) + 11);
                        if (consumedField.Contains(" "))
                            consumedField = consumedField.Substring(0,
                                consumedField.IndexOf(" ", StringComparison.Ordinal));
                        foreach (var fields in from entityRuleSet in entityRuleSets
                                 where entityRuleSet.FieldBackendNames.Contains(consumedField)
                                 select new Fields()
                                 {
                                     FieldName = consumedField,
                                     EntityName = entityRuleSet.EntityName
                                 })
                        {
                            ruleSetMap.Fields.Add(fields);
                        }
                    }
                    if (ruleSetMap.Fields.Count <= 0) continue;
                    ruleSets.Add(ruleSetMap);
                }
            }

            var ruleSetMapOutputFields = (from item in ruleSets from field in item.Fields
                select new RuleSetMapOutputFields()
                {
                    Id = null,
                    RuleAppName = ruleAppDef.Name,
                    RuleAppLabel = "Live",
                    RuleSetName = item.RuleSetName,
                    FieldName = field.FieldName,
                    EntityContext = item.EntityContext,
                    EntityFieldIsFrom = field.EntityName,
                    DateTimeUpdated = DateTime.Now.ToString(CultureInfo.InvariantCulture)
                }).ToList();

            for (var j = 1; j < ruleSetMapOutputFields.Count(); j++)
            {
                if (ruleSetMapOutputFields[j].FieldName == ruleSetMapOutputFields[j - 1].FieldName)
                    ruleSetMapOutputFields.RemoveAt(j - 1);
            }
            for (var j = 0; j < ruleSetMapOutputFields.Count(); j++)
            {
                ruleSetMapOutputFields[j].Id = j;
            }
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using var command = new SqlCommand($"DELETE FROM {destinationTableName} WHERE RuleAppName ='{ruleAppDef.Name}' AND RuleAppLabel = 'Live';", connection);
                using var reader = command.ExecuteReader();
            }

            if (string.IsNullOrEmpty(connectionString)) return;
            var ruleSetMapOutputFieldsDt = ConvertToDataTable(ruleSetMapOutputFields);
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var bulkCopy = new SqlBulkCopy(conn);
            bulkCopy.DestinationTableName = destinationTableName;
            bulkCopy.BatchSize = ruleSetMapOutputFieldsDt.Rows.Count;
            bulkCopy.WriteToServer(ruleSetMapOutputFieldsDt);
            bulkCopy.Close();

            await NotificationHelper.NotifyAsync($"Successfully updated database with new rulesets from {ruleAppDef.Name} rule application", Prefix, "Debug");
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


