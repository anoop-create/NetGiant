using System.Diagnostics;
using System.Web.Optimization;

namespace netGiant.Intranet
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/cssBundle.css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css",
                      "~/Content/jquery-ui.min.css",
                      "~/Content/jquery-ui.structure.min.css",
                      "~/Content/jquery-ui.theme.min.css",
                      "~/Content/kendo.common.min.css",
                      "~/Content/kendo.metro.min.css",
                      "~/Content/jquery-confirm.css"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryBundle.js").Include(
                      //"~/Scripts/jquery.lazyload.js",
                      "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrapBundle.js").Include(
                      "~/Scripts/bootstrap.min.js",
                      "~/Scripts/jquery-confirm.js",
                      "~/Scripts/respond.min.js",
                      "~/Scripts/kendo.all.min.js",
                      "~/Scripts/kendo.aspnetmvc.min.js",
                      "~/Scripts/masonry.min.js",
                      "~/Scripts/layout.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryuiBundle.js").Include(
                        "~/Scripts/jquery-ui.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryvalBundle.js").Include(
                      "~/Scripts/jquery.unobtrusive-ajax.js",
                      "~/Scripts/jquery.validate*",
                      "~/Scripts/expressive.annotations.validate.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/customBundle.js").Include(                      
                      "~/Scripts/site.js"
                ));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            // Set EnableOptimizations to false for debugging. For more information,
            // visit http://go.microsoft.com/fwlink/?LinkId=301862
            BundleTable.EnableOptimizations = Debugger.IsAttached ? false : true;
        }
    }
}
