using System;
using InRule.Repository.Client;

namespace InRule.DevOps.Promote.Service.CatalogConnection
{
    public class CatalogConnection : ICatalogConnection
    {
        public RuleCatalogConnection ConnectToCatalog(Uri catalogUri, TimeSpan time, string userName, string password)
        {
           return new RuleCatalogConnection(catalogUri, time, userName, password);
        }
    }
}