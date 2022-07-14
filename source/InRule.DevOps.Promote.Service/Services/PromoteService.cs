using System;
using InRule.DevOps.Promote.Service.CatalogConnection;
using InRule.DevOps.Promote.Service.Model;
using InRule.Repository;
using InRule.Repository.Service.Data;


namespace InRule.DevOps.Promote.Service.Services
{
    public class PromoteService : IPromoteService
    {
        private readonly ICatalogConnection _catalogConnection;
        public PromoteService(ICatalogConnection catalogConnection)
        {
            _catalogConnection = catalogConnection;
        }

        public RuleAppRef GetRuleAppRef(PromoteModel model)
        {
            var connection = _catalogConnection.ConnectToCatalog(new Uri(model.SourceCatalogUri),
                new TimeSpan(0, 10, 0), model.SourceCatalogUserName, model.SourceCatalogPassword);
            RuleAppRef ruleAppRef = null;
            if (model.Revision!=0 || string.IsNullOrEmpty(model.Label))
            {
                 ruleAppRef = connection.GetRuleAppRef(model.RuleAppName, model.Revision);
            }
            else if(!string.IsNullOrEmpty(model.Label) || model.Revision == 0)
            {
                 ruleAppRef = connection.GetRuleAppRef(model.RuleAppName, model.Label);
            }
            return ruleAppRef;
        }

        public RuleApplicationDef SendRequest(RuleAppRef ruleAppRef, PromoteModel model)
        {

            var sourceConn = _catalogConnection.ConnectToCatalog(new Uri(model.SourceCatalogUri),
                new TimeSpan(0, 10, 0), model.SourceCatalogUserName, model.SourceCatalogPassword);
            var sourceRuleAppDef = sourceConn.GetSpecificRuleAppRevision(ruleAppRef.Guid, ruleAppRef.PublicRevision);

            var targetConn = _catalogConnection.ConnectToCatalog(new Uri(model.TargetCatalogUri),
                new TimeSpan(0, 10, 0), model.TargetCatalogUserName, model.TargetCatalogPassword);
            var targetRuleAppDef = targetConn.PromoteRuleApplication(sourceRuleAppDef, "Description", "Comments");

            return targetRuleAppDef;
        }
    }
}