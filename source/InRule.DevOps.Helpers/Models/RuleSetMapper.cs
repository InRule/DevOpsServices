using System.Collections.Generic;

namespace SdkRunner573.Models
{
    public class EntityRuleSet
    {
        public List<string> FieldBackendNames { get; set; }
        public string EntityName { get; set; }
    }
    public class Fields
    {
        public string FieldName { get; set; }
        public string EntityName { get; set; }
    }
    public class RuleSetMap
    {
        public string RuleSetName { get; set; }
        public string EntityContext { get; set; }
        public List<Fields> Fields { get; set; }

    }
    public class RuleSetMapOutputFields
    {
        public int? Id { get; set; }
        public string RuleAppName { get; set; }
        public string RuleAppLabel { get; set; }
        public string RuleSetName { get; set; }
        public string FieldName { get; set; }
        public string EntityContext { get; set; }
        public string EntityFieldIsFrom { get; set; }
        public string DateTimeUpdated { get; set; }
    }
}
