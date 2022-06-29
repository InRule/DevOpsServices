using System.Web.Http;

namespace InRule.DevOps.Promote.Service
{
    public static class IOC_Run
    {
        public static void Run()
        {
            SetAutofacWebAPIServices();
        }

        private static void SetAutofacWebAPIServices()
        {
            var configuration = GlobalConfiguration.Configuration;
            configuration.DependencyResolver = IOC.AutofacWebApiDependencyResolver();

        }
    }
}