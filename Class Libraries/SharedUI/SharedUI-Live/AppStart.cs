using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using RazorGenerator.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Hosting;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(SharedUI.AppStart), "Start")]

namespace SharedUI
{
    public static class AppStart
    {
        public static void Start()
        {
            ConfigureRoutes();
            ConfigureBundles();
        }

        private static void ConfigureBundles()
        {
            BundleTable.VirtualPathProvider = new EmbeddedVirtualPathProvider(HostingEnvironment.VirtualPathProvider);
            BundleTable.Bundles.Add(new StyleBundle("~/SharedUI/Embedded/Css")
                .Include("~/SharedUI/Embedded/bootstrap.css",
                    "~/SharedUI/Embedded/common.css")
                );
        }

        private static void ConfigureRoutes()
        {
            RouteTable.Routes.Insert(0,
                new Route("SharedUI/Embedded/{file}.{extension}",
                    new RouteValueDictionary(new { }),
                    new RouteValueDictionary(new { extension = "css|js" }),
                    new EmbeddedResourceRouteHandler()
                ));
        }

    }
}
