using ExpressiveAnnotations.MvcUnobtrusive.Providers;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace netGiant.Intranet
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ModelValidatorProviders.Providers.Remove(
                ModelValidatorProviders.Providers
                .FirstOrDefault(x => x is DataAnnotationsModelValidatorProvider));
            ModelValidatorProviders.Providers.Add(
                new ExpressiveAnnotationsModelValidatorProvider());

            //DbInterception.Add(new NoLockInterceptor());
        }
    }
}
