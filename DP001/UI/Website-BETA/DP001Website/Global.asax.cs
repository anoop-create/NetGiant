using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ExpressiveAnnotations.MvcUnobtrusive.Providers;
using System.Data.Entity.Infrastructure.Interception;
using DP001DataAccess.Entities;
using System.Configuration;
using System.Globalization;
using System.Threading;
using DP001Website.Models;
using Microsoft.ApplicationInsights.Extensibility;

namespace DP001Website
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

            DbInterception.Add(new NoLockInterceptor());

            if (ConfigurationManager.AppSettings["Environment"] != "Live")
                TelemetryConfiguration.Active.DisableTelemetry = true;
        }
    }
}
