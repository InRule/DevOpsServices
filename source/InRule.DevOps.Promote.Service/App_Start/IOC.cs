using System.Reflection;
using System.Web.Http.Controllers;
using Autofac;
using Autofac.Integration.WebApi;
using InRule.DevOps.Promote.Service.Services;

namespace InRule.DevOps.Promote.Service
{
    public static class IOC
    {
        public static AutofacWebApiDependencyResolver AutofacWebApiDependencyResolver()
        {
            var builder = new ContainerBuilder();

            // Register API controllers using assembly scanning.
            builder.RegisterApiControllers(Assembly.GetCallingAssembly());
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => typeof(IHttpController).IsAssignableFrom(t) && t.Name.EndsWith("Controller"));

            builder.RegisterType<ConnectorService>().As<IConnectorService>().InstancePerRequest();

            var container = builder.Build();

            var resolver = new AutofacWebApiDependencyResolver(container);
            return resolver;
        }
    }
}