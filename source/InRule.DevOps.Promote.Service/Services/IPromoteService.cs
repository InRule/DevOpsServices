using InRule.DevOps.Promote.Service.Model;
using InRule.Repository;
using InRule.Repository.Service.Data;

namespace InRule.DevOps.Promote.Service.Services
{
    public interface IPromoteService
    {
        RuleAppRef GetRuleAppRef(PromoteModel model);
        RuleApplicationDef SendRequest(RuleAppRef ruleAppRef, PromoteModel model);
    }
}
