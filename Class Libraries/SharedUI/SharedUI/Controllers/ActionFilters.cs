using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SharedUI.Controllers
{
    public class AuthorizeIpAddressAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var ipAddress = HttpContext.Current.Request.UserHostAddress ?? "";
            var forwardedFor = HttpContext.Current.Request.Headers["X-FORWARDED-IP"];
            if (forwardedFor != null)
            {
                // If X-Forwarded-For exsts use it to check for allowed addresses (with StackPath can happen on LIVE and BETA)
                if (!IsIpAddressAllowed(forwardedFor.Trim()))
                    context.Result = new HttpStatusCodeResult(403);
            }
            else
            {
                // If there's no X-Forwarded-For
                if (!ipAddress.StartsWith("10.101.1"))
                {
                    if (!IsIpAddressAllowed(ipAddress.Trim()))
                        context.Result = new HttpStatusCodeResult(403);
                }
            }

            base.OnActionExecuting(context);
        }

        private bool IsIpAddressAllowed(string forwardedFor)
        {
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                string[] addresses = Convert.ToString(ConfigurationManager.AppSettings["AllowedIPAddresses"])
                    .Split(',');
                return addresses.Any(a => forwardedFor.Contains(a.Trim()));
            }
            return false;
        }
    }

    public class SiteOfflineCheckAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int alertLevel = Convert.ToInt32(ConfigurationManager.AppSettings["AlertLevel"]);
            string controller = Convert.ToString(context.RouteData.Values["controller"]).ToLower();
            string action = Convert.ToString(context.RouteData.Values["action"]).ToLower();

            if (alertLevel == 5 || (alertLevel == 4 && (controller == "checkout" || controller == "myaccount" || (controller == "misc" && action == "newslettersignup"))))
            {
                var ipAddress = HttpContext.Current.Request.UserHostAddress ?? "";
                var forwardedFor = HttpContext.Current.Request.Headers["X-FORWARDED-IP"];
                if (forwardedFor != null)
                {
                    // Live 
                    if (!IsIpAddressAllowed(forwardedFor.Trim()) && action != "sagepaynotification")
                    {
                        context.Result = new RedirectToRouteResult(
                            new RouteValueDictionary
                            {
                                {"controller", "Error"},
                                {"action", "Alert"},
                                {"AlertLevel", alertLevel}
                            }
                        );
                    }
                }
                else
                {
                    // Dev
                    if (!ipAddress.StartsWith("10.101.1"))
                    {
                        if (!IsIpAddressAllowed(ipAddress.Trim()) && action != "sagepaynotification")
                        {
                            context.Result = new RedirectToRouteResult(
                                new RouteValueDictionary
                                {
                                    {"controller", "Error"},
                                    {"action", "Alert"},
                                    {"AlertLevel", alertLevel}
                                }
                            );
                        }
                    }
                }
            }

            base.OnActionExecuting(context);
        }

        private bool IsIpAddressAllowed(string forwardedFor)
        {
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                string[] addresses = Convert.ToString(ConfigurationManager.AppSettings["AllowedIPAddresses"])
                    .Split(',');
                return addresses.Any(a => forwardedFor.Contains(a.Trim()));
            }
            return false;
        }
    }

    public class SessionExpiredFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext ctx = HttpContext.Current;

            // check if session is supported
            if (ctx.Session != null)
            {
                // check if a new session id was generated
                if (ctx.Session.IsNewSession)
                {
                    // If it says it is a new session, but an existing cookie exists, then it must
                    // have timed out
                    string sessionCookie = ctx.Request.Headers["Cookie"];
                    if ((null != sessionCookie) && (sessionCookie.IndexOf("ASP.NET_SessionId") >= 0))
                    {
                        string controller = filterContext.RouteData.Values["controller"].ToString().ToLower();
                        string action = filterContext.RouteData.Values["action"].ToString().ToLower();
                        if (controller == "checkout" &&
                            (action == "stage1" || action == "stage2" || action == "stage3"))
                        {
                            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                            {
                                controller = "Checkout",
                                action = "Index",
                                pm = "SessionExpired"
                            }));
                        }
                    }
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }

    public class BasicAuthenticationAttribute : ActionFilterAttribute
    {
        public string BasicRealm { get; set; }
        protected string Username { get; set; }
        protected string Password { get; set; }

        public BasicAuthenticationAttribute(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var req = filterContext.HttpContext.Request;
            var auth = req.Headers["Authorization"];
            if (!String.IsNullOrEmpty(auth))
            {
                var cred = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');
                var user = new { Name = cred[0], Pass = cred[1] };
                if (user.Name == Username && user.Pass == Password) return;
            }
            var res = filterContext.HttpContext.Response;
            res.StatusCode = 401;
            res.AddHeader("WWW-Authenticate", String.Format("Basic realm=\"{0}\"", BasicRealm ?? "NG"));
            res.End();
        }
    }
}