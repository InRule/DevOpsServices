using System.Web.Http;
using InRule.DevOps.Promote.Service.Services;

namespace InRule.DevOps.Promote.Service.Controllers
{
    public class ConnectorController : ApiController
    {
        private readonly IConnectorService _connectorService;
        public ConnectorController(IConnectorService connectorService)
        {
            _connectorService = connectorService;
        }

        [HttpPost]
        public IHttpActionResult Post()
        {
            return Ok("method accessed");
        }
    }
}