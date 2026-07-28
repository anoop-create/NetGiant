using System;
using System.Configuration;
using System.Diagnostics;
using System.Web.Mvc;
using TG_Ecommerce_Website.Models;

namespace TG_Ecommerce_Website
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new CustomErrorHandling());
            switch (ConfigurationManager.AppSettings["Environment"])
            {
                case "Live":
                case "Dev":
                {
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["RequireHttps"]))
                    {
                        filters.Add(new CustomRequireHttpsFilter());
                    }
                    break;
                }
            }
        }
    }

    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class CustomRequireHttpsFilter : RequireHttpsAttribute
    {
        protected override void HandleNonHttpsRequest(AuthorizationContext filterContext)
        {
            // The base only redirects GET, but we added HEAD as well. This avoids exceptions for bots crawling using HEAD.
            // The other requests will throw an exception to ensure the correct verbs are used. 
            // We fall back to the base method as the mvc exceptions are marked as internal. 

            if (!String.Equals(filterContext.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                && !String.Equals(filterContext.HttpContext.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = new RedirectResult("https://" + filterContext.HttpContext.Request.Url.Host, true);
            }

            // Redirect to HTTPS version of page
            // We updated this to redirect using 301 (permanent) instead of 302 (temporary).
            string url = "https://" + filterContext.HttpContext.Request.Url.Host + filterContext.HttpContext.Request.RawUrl;

            if (string.Equals(filterContext.HttpContext.Request.Url.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // For localhost requests, default to IISExpress https default port (44300)
                url = "https://" + filterContext.HttpContext.Request.Url.Host + ":44300" + filterContext.HttpContext.Request.RawUrl;
            }

            filterContext.Result = new RedirectResult(url, true);
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (filterContext.HttpContext.Request.IsSecureConnection)
            {
                return;
            }

            if (string.Equals(filterContext.HttpContext.Request.Headers["X-FORWARDED-PROTO"],
                "https",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (filterContext.HttpContext.Request.Url != null && filterContext.HttpContext.Request.Url.ToString().ToLower().Contains("siteup"))
            {
                return;
            }

            if (filterContext.HttpContext.Request.IsLocal)
            {
                return;
            }

            HandleNonHttpsRequest(filterContext);
        }
    }
}
