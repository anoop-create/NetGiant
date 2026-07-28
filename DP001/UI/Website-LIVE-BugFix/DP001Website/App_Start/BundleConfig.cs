using DP001BusinessLogic.Shared;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Web;
using System.Web.Optimization;

namespace DP001Website
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/jquery.validate*",
                        "~/Scripts/expressive.annotations.validate.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/bootstrap-toggle.css",
                      "~/Content/awesome-bootstrap-checkbox.css",
                      "~/Content/bootstrap-select.css",
                      "~/Content/font-awesome.css",
                      "~/Content/jquery-ui.css",
                      "~/Content/jquery.timeentry.css",
                      "~/Content/Gridmvc.css",
                      "~/Content/Site.css"));
           
            bundles.Add(new ScriptBundle("~/bundles/alljs").Include(
                BuildAllJsBundle()));

            bundles.Add(new ScriptBundle("~/bundles/idletimer").Include(
                "~/Scripts/idle-timer.js"));

            BundleTable.EnableOptimizations = Debugger.IsAttached ? false : true;
        }

        private static string[] BuildAllJsBundle()
        {
            var allJS = new List<string>()
            {
                "~/Scripts/jquery-{version}.js",
                "~/Scripts/jquery.validate*",
                "~/Scripts/expressive.annotations.validate.js",
                "~/Scripts/jquery.unobtrusive-ajax.js",
                "~/Scripts/jquery-ui.js",
                "~/Scripts/jquery.plugin.js",
                "~/Scripts/jquery.timeentry.js",
                "~/Scripts/bootstrap.js",
                "~/Scripts/bootstrap-toggle.js",
                "~/Scripts/bootstrap-select.js",
                "~/Scripts/jquery.stickytableheaders.js",
                "~/Scripts/site.js",
                "~/Scripts/respond.js",
                "~/Scripts/gridmvc.js",
                "~/Scripts/gridmvc.customwidgets.js"
            };

            if (ConfigurationManager.AppSettings["Environment"] == "Live")
                allJS.Add("~/Scripts/application-insights.js");

            return allJS.ToArray();
        }
    }
}
