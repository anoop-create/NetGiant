using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DP001Website.Models
{
    public class CheckUserPermission : ActionFilterAttribute, IActionFilter
    {
        public string FieldName { get; set; }
        public TenantPermissonCheck Check { get; set; }

        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            var cm = new CommonModel();
            var hasPermission = cm.TenantPermissionCheck(FieldName, Check);

            if (!hasPermission)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(
                    new
                    {
                        controller = "Dashboard",
                        action = "Index"
                    }));
            }
        }
    }
}

