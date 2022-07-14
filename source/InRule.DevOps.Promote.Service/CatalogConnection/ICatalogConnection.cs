using System;
using InRule.Repository.Client;

namespace InRule.DevOps.Promote.Service.CatalogConnection
{
    public interface ICatalogConnection
    { 
        RuleCatalogConnection ConnectToCatalog(Uri catalogUri, TimeSpan time, string userName, string password);
    }
}