using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using InRule.DevOps.Promote.Service.Model;
using InRule.DevOps.Promote.Service.Services;

namespace InRule.DevOps.Promote.Service.Controllers
{
    public class PromoteController : ApiController
    {
        private readonly IPromoteService _promoteService;
        public PromoteController(IPromoteService promoteService)
        {
            _promoteService = promoteService;
        }

        [HttpPost]
        public IHttpActionResult Post([FromBody] PromoteModel model)
        {
            var validationErrors = ValidateRequest(model);
            if (validationErrors != null)
            {
                foreach (var error in validationErrors)
                {
                    return BadRequest(error.Value.FirstOrDefault());
                }
            }

            var appReference =  _promoteService.GetRuleAppRef(model);
            var result  = _promoteService.SendRequest(appReference, model);
            return Ok(result.Name);
        }

        private IDictionary<string, IEnumerable<string>> ValidateRequest(PromoteModel model)
        {
            var errorDictionary = new Dictionary<string, IEnumerable<string>>();
            if (string.IsNullOrEmpty(model.TargetCatalogUri))
            {
                errorDictionary.Add($"{nameof(model.TargetCatalogUri)}", new List<string> { "Please provide Target catalog uri" });
            }
            if (string.IsNullOrEmpty(model.SourceCatalogUri))
            {
                errorDictionary.Add($"{nameof(model.TargetCatalogUri)}", new List<string> { "Please provide Source catalog uri" });
            }
            if (string.IsNullOrEmpty(model.TargetCatalogPassword))
            {
                errorDictionary.Add($"{nameof(model.TargetCatalogPassword)}", new List<string> { "Please provide Target catalog password" });
            }
            if (string.IsNullOrEmpty(model.TargetCatalogUserName))
            {
                errorDictionary.Add($"{nameof(model.TargetCatalogUserName)}", new List<string> { "Please provide Target catalog userName" });
            }
            if (string.IsNullOrEmpty(model.SourceCatalogPassword))
            {
                errorDictionary.Add($"{nameof(model.SourceCatalogPassword)}", new List<string> { "Please provide source catalog password" });
            }
            if (string.IsNullOrEmpty(model.SourceCatalogUserName))
            {
                errorDictionary.Add($"{nameof(model.SourceCatalogUserName)}", new List<string> { "Please provide source catalog userName" });
            }
            if (string.IsNullOrEmpty(model.RuleAppName))
            {
                errorDictionary.Add($"{nameof(model.RuleAppName)}", new List<string> { "Please provide Rule app name" });
            }
            if (model.Revision==0 && string.IsNullOrEmpty(model.Label))
            {
                errorDictionary.Add($"{nameof(model.Revision)}{nameof(model.Label)}", new List<string> { "Please provide either Revision or Label" });
            }
           
            return errorDictionary.Any() ? errorDictionary : null;
        }
    }
}