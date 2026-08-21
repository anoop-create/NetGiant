using System.Configuration;
using System.Web.Optimization;

namespace TG_Ecommerce_Website
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jqueryBundle.js").Include(
                "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryvalBundle.js").Include(
                "~/Scripts/jquery.unobtrusive-ajax.js",
                "~/Scripts/jquery.validate*",
                "~/Scripts/expressive.annotations.validate.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrapBundle.js").Include(
                "~/Scripts/bootstrap.js",
                "~/Scripts/jquery.dotdotdot.min.js",
                "~/Scripts/jquery-confirm.js",
                "~/Scripts/bootstrap-select.min.js",
                "~/Scripts/jquery.jscrollpane.min.js",
                "~/Scripts/jquery.lazyload.js",
                "~/Scripts/jquery.hoverIntent.js",
                "~/Scripts/jquery.number.min.js",
                "~/Scripts/site.js",
                "~/Scripts/custom.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrapBundleM.js").Include(
                "~/Scripts/bootstrap.js",
                "~/Scripts/jquery.dotdotdot.min.js",
                "~/Scripts/jquery-confirm.js",
                "~/Scripts/bootstrap-select.min.js",
                "~/Scripts/jquery.jscrollpane.min.js",
                "~/Scripts/jquery.lazyload.js",
                "~/Scripts/jquery.hoverIntent.js",
                "~/Scripts/jquery.number.min.js",
                "~/Scripts/site.js",
                "~/Scripts/custom.js"));

            bundles.Add(new LessBundle("~/Content/cssBundle.css").Include(
                "~/Content/bootstrap.css",
                "~/Content/bootstrap-select.min.css",
                "~/Content/awesome-bootstrap-checkbox.css",
                "~/Content/kendo.common.min.css",
                "~/Content/kendo.bootstrap.min.css",
                "~/Content/jquery-confirm.css", 
                "~/Content/font-awesome.css",
                "~/Content/site.less"));

            bundles.Add(new ScriptBundle("~/bundles/portaljsBundle.js").Include(
                "~/Scripts/bootstrap.js",
                "~/Scripts/bootstrap-select.min.js",
                "~/Scripts/jquery-confirm.js",
                "~/Scripts/portal.js"));

            bundles.Add(new ScriptBundle("~/bundles/portalhdrjsBundle.js").Include(
                "~/Scripts/kendo.all.min.js",
                "~/Scripts/kendo.aspnetmvc.min.js",
                "~/Scripts/kendo.culture.en-GB.min.js"));

            bundles.Add(new LessBundle("~/Content/portalcssBundle.css").Include(
                "~/Content/bootstrap.css",
                "~/Content/bootstrap-select.min.css",
                "~/Content/awesome-bootstrap-checkbox.css",
                "~/Content/kendo.common.min.css",
                "~/Content/kendo.bootstrap.min.css",
                "~/Content/jquery-confirm.css",
                "~/Content/site.less",
                "~/Content/font-awesome.css",
                "~/Content/style.css",
                "~/Content/minicart.css"));

            // Bundles By Controller
            bundles.Add(new ScriptBundle("~/bundles/checkoutBundle.js").Include(
                "~/Scripts/kendo.core.min.js",
                "~/Scripts/kendo.userevents.min.js",
                "~/Scripts/kendo.numerictextbox.min.js", 
                "~/Scripts/checkout.js"));

            bundles.Add(new ScriptBundle("~/bundles/wizardBundle.js").Include(
                "~/Scripts/kendo.core.min.js",
                "~/Scripts/kendo.data.min.js",
                "~/Scripts/kendo.popup.min.js",
                "~/Scripts/kendo.list.min.js",
                "~/Scripts/kendo.fx.min.js",
                "~/Scripts/kendo.userevents.min.js",
                "~/Scripts/kendo.mobile.scroller.min.js",
                "~/Scripts/kendo.dropdownlist.min.js", 
                "~/Scripts/wizard.js"));

            bundles.Add(new ScriptBundle("~/bundles/productBundle.js").Include(
                "~/Scripts/kendo.core.min.js",
                "~/Scripts/kendo.userevents.min.js",
                "~/Scripts/kendo.numerictextbox.min.js",
                "~/Scripts/product.js"));
            bundles.Add(new StyleBundle("~/Content/style.css"));
            bundles.Add(new StyleBundle("~/Content/minicart.css"));

            bundles.Add(new LessBundle("~/Content/minicart.css"));
            BundleTable.EnableOptimizations = ConfigurationManager.AppSettings["Environment"] != "Local";
        }
    }
}