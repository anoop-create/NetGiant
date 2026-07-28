using BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Management;
using System.Web.Mvc;
using System.Web.Routing;

namespace CM_Ecommerce_Website.Models
{
    public class CustomErrorHandling : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsLocal)
                return;

            filterContext.ExceptionHandled = true;
            int id = 500;
            if (filterContext.Exception is WebsiteInMaintenanceException)
            {
                id = 9000;
            }
            else
            {
                Utilities.ProcessException(filterContext.Exception);
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
