namespace InRule.DevOps.Promote.Service.Model
{
    public class PromoteModel
    {
        public string SourceCatalogUserName { get; set; }
        public string SourceCatalogPassword { get; set; }
        public string TargetCatalogUserName { get; set; }   
        public string TargetCatalogPassword { get; set; }
        public string SourceCatalogUri { get; set; }
        public string TargetCatalogUri { get; set; }
        public string RuleAppName { get; set; }
        public int Revision { get; set; }
        public string Label { get; set; }
    }
}