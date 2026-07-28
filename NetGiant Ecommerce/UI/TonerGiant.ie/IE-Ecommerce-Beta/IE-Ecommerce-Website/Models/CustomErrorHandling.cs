using BusinessLogic;
using System;
using System.Configuration;
using System.Web;
using System.Web.Management;
using System.Web.Mvc;
using System.Web.Routing;

namespace IE_Ecommerce_Website.Models
{
    public class CustomErrorHandling : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsLocal)
                return;

            filterContext.ExceptionHandled = true;
            int id = 500;
            string furtherInfo = "";

            // Exclude Maintenance errors
            if (filterContext.Exception is WebsiteInMaintenanceException)
            {
                id = 9000;
            }
            else
            {
                // Don't log errors of a certain types
                string exclString = ConfigurationManager.AppSettings["ExcludeDebugging"];
                if (!exclString.Contains(filterContext.Exception.GetType().ToString()))
                {
                    // Log error
                    string urlString = ConfigurationManager.AppSettings["EnhancedDebugging"];
                    if (HttpContext.Current.Request.Url.ToString().Contains(urlString))
                    {
                        furtherInfo += Utilities.FormatViewData(filterContext.Controller.ViewData);
                        furtherInfo += Utilities.FormatSession(HttpContext.Current.Session);
                        furtherInfo += Utilities.FormatRequest(HttpContext.Current.Request);
                    }
                    Utilities.ProcessException(filterContext.Exception, furtherInfo.ToString());
                }
            }

            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                Utilities.SendAnalyticsEvent(
                            "System Error"
                            , filterContext.Exception.GetType().ToString()
                            , HttpContext.Current.Request.Url.AbsolutePath.ToString().Length > 200 ? HttpContext.Current.Request.Url.AbsolutePath.ToString().Substring(0, 200) : HttpContext.Current.Request.Url.AbsolutePath.ToString());
            }

            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary(
                    new
                    {
                        action = "Index",
                        controller = "Error",
                        id = id
                    }));

        }

        public class CustomErrorEvent : WebErrorEvent
        {
            public CustomErrorEvent(
              string msg, object eventSource,
              int eventCode, Exception exception)
                : base(msg, eventSource, eventCode, exception)
            {
            }

        }
    }
}
