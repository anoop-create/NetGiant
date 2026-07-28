using System.Web.Mvc;
using System.Web.Routing;

namespace TG_Ecommerce_Website
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.LowercaseUrls = true;

            // Map Specific Routes First
            
            routes.MapRoute(
                name: "Product",
                url: "product/{productname}-{id}/",
                defaults: new
                {
                    controller = "Product",
                    action = "Index"
                },
                constraints: new { id = @"\d+" }
            );

            routes.MapRoute(
                name: "Product Grid",
                url: "products/{categoryname}-{id}/",
                defaults: new
                {
                    controller = "Product",
                    action = "Grid"
                },
                constraints: new { id = @"\d+" }
            );

            routes.MapRoute(
                name: "Search",
                url: "search-results/",
                defaults: new
                {
                    controller = "Search",
                    action = "Index"
                }
            );

            routes.MapRoute(
                name: "GSearch",
                url: "gsearch/",
                defaults: new
                {
                    controller = "Search",
                    action = "Gsearch"
                }
            );

            routes.MapRoute(
                name: "Product List",
                url: "model/{equipname}/",
                defaults: new
                {
                    controller = "Product",
                    action = "ProductList"
                },
                constraints: new { equipname = @"^.*(toner-cartridges|ink-cartridges|solid-ink-cartridges|franking-cartridges)$" }
            );

            routes.MapRoute(
                name: "Catalogue",
                url: "catalogue/{catalogname}-{id}/",
                defaults: new
                {
                    controller = "Product",
                    action = "Catalogue"
                },
                constraints: new { id = @"\d+" }
            );

            routes.MapRoute(
                name: "OldPW1",
                url: "pw-printer3/{typename}/{manuname}/{familyname}/{equipname}/",
                defaults: new
                {
                    controller = "Misc",
                    action = "OldPrinterRedirect"
                }
            );

            routes.MapRoute(
                name: "OldPW2",
                url: "pw-printer3/{typename}/{manuname}/{familyname}/",
                defaults: new
                {
                    controller = "Misc",
                    action = "OldPrinterRedirect"
                }
            );

            routes.MapRoute(
                name: "OldPW3",
                url: "{pagetype}/{typename}/{manuname}/",
                defaults: new
                {
                    controller = "Misc",
                    action = "OldPrinterRedirect"
                },
                constraints: new { pagetype = @"(pw-printer|pw-printer3)" }
            );

            routes.MapRoute(
                name: "OldPW4",
                url: "{pagetype}/{typename}/",
                defaults: new
                {
                    controller = "Misc",
                    action = "OldPrinterRedirect"
                },
                constraints: new { pagetype = @"(pw-printer|pw-printer3)" }
            );

            routes.MapRoute(
                name: "Checkout",
                url: "checkout/",
                defaults: new
                {
                    controller = "Checkout",
                    action = "ViewBasket"
                }
            );

            routes.MapRoute(
                name: "Voucher",
                url: "{voucherType}voucher/{voucherCode}",
                defaults: new
                {
                    controller = "Misc",
                    action = "ApplyVoucher"
                },
                constraints: new { voucherType = @"(c|p)" }
            );

            routes.MapRoute(
                name: "Reviews",
                url: "{sitename}-reviews/",
                defaults: new
                {
                    controller = "Home",
                    action = "CustomerReviews"
                },
                constraints: new { sitename = @"(tonergiant|cartridgemonkey|netgiant)" }
            );

            routes.MapRoute(
                name: "Printer Finder",
                url: "printer-finder/",
                defaults: new
                {
                    controller = "Misc",
                    action = "PrinterFinder"
                }
            );

            routes.MapRoute(
                name: "Wizard",
                url: "{typename}/{manuname}/{familyname}/",
                defaults: new
                {
                    controller = "Equipment",
                    action = "PrinterWizard",
                    manuname = UrlParameter.Optional,
                    familyname = UrlParameter.Optional
                },
                constraints: new { typename = @"(toner-cartridges|ink-cartridges|solid-ink-cartridges|franking-cartridges)" }
            );

            // Now Map Genric Routes

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                    name: "Error",
                    url: "{url}",
                    defaults: new {
                        controller = "Error",
                        action = "NotFound"
                    }
            );
        }
    }
}
