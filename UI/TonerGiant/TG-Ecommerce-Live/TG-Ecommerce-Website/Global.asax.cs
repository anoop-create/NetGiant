using BusinessLogic;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ExpressiveAnnotations.Attributes;
using ExpressiveAnnotations.MvcUnobtrusive.Validators;

namespace TG_Ecommerce_Website
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            try
            {
                Utilities.LoadApplicationVariables();

                DataAnnotationsModelValidatorProvider.RegisterAdapter(
                    typeof(RequiredIfAttribute), typeof(RequiredIfValidator));
                DataAnnotationsModelValidatorProvider.RegisterAdapter(
                    typeof(AssertThatAttribute), typeof(AssertThatValidator));
            }
            catch
            {
                //ignore this element and move on to the next
            }
        }

        protected void Session_Start()
        {
            try
            {
                Authentication.LoadCookie();
                Basket.LoadCookie();
                Utilities.SetDeliveryDate();

                if (Authentication.IsAuthenticated())
                {
                    HttpContext.Current.Session["M_GoogleRevisit"] = true;
                }

                HttpContext.Current.Session["U_IsFirstTime"] = true;
                HttpContext.Current.Session["U_IsFromPPC"] = false;

                 string landingPage = HttpContext.Current.Request.Path;

                if (!string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["gclid"]) && landingPage != "/")
                {
                    HttpContext.Current.Session["U_IsFromPPC"] = true;
                }
                HttpContext.Current.Session["U_AffiliateNo"] = "";
            }
            catch
            {
                //ignore this element and move on to the next
            }
        }

        //public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        //{
        //    filters.Add(new CheckForDownPage());
        //}
    }

    //public sealed class CheckForDownPage : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(ActionExecutingContext filterContext)
    //    {
    //        var path = System.Web.Hosting.HostingEnvironment.MapPath("~/Down.htm");

    //        if (System.IO.File.Exists(path) && IpAddress != "1.2.3.4")
    //        {
    //            filterContext.HttpContext.Response.Clear();
    //            filterContext.HttpContext.Response.Redirect("~/Down.htm");
    //            return;
    //        }

    //        base.OnActionExecuting(filterContext);
    //    }
    //}
}