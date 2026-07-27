using BusinessLogic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CommonUI.Controllers
{
    public class AuthorizeIpAddressAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var ip = Utilities.GetClientIPAddress(new HttpRequestWrapper(HttpContext.Current.Request));
            if (ip.StartsWith("10.101.1") && ip.StartsWith("172.21.224"))
            {
                ip = "::1";
            }

            if (!IsIpAddressAllowed(ip.Trim()))
                context.Result = new HttpStatusCodeResult(403);

            base.OnActionExecuting(context);
        }

        private bool IsIpAddressAllowed(string forwardedFor)
        {
            //if (ConfigurationManager.AppSettings["Environment"] != "Live")
            //{
            //    return true;
            //}
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                string[] addresses = Convert.ToString(ConfigurationManager.AppSettings["AllowedIPAddresses"])
                    .Split(',');
                return addresses.Any(a => forwardedFor.Contains(a.Trim()));
            }
            return false;
        }
    }

    public class DuoAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string csUserId = HttpContext.Current.Request.Cookies["__csuser"] == null ? "" : HttpContext.Current.Request.Cookies["__csuser"].Value;
            if (HttpContext.Current.Request.Cookies["__skipportalauth"] != null && !string.IsNullOrEmpty(csUserId))
            {
                HttpContext.Current.Session["U_CSUser"] = csUserId;
                base.OnActionExecuting(context);
                return;
            }

            var ip = Utilities.GetClientIPAddress(new HttpRequestWrapper(HttpContext.Current.Request));
            if (ip.StartsWith("10.101.1") && ip.StartsWith("172.21.224"))
            {
                ip = "::1";
            }
            if (IsIpAddressAllowed(ip.Trim()) && !string.IsNullOrEmpty(csUserId))
            {
                if (HttpContext.Current.Session["U_CSUser"] == null)
                {
                    HttpContext.Current.Session["U_CSUser"] = csUserId;
                    Authentication.WriteCookie("__skipportalauth", "y", new TimeSpan(0, 20, 0, 0));
                }

                base.OnActionExecuting(context);
                return;
            }

            // Redirect to Portal Login process
            context.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    {"controller", "Portal"},
                    {"action", "Login"},
                    {"nexturl", context.HttpContext.Request.Url.AbsoluteUri}
                }
            );

            base.OnActionExecuting(context);
        }

        private bool IsIpAddressAllowed(string ip)
        {
            //if (ConfigurationManager.AppSettings["Environment"] != "Live")
            //{
            //    return true;
            //}
            if (!string.IsNullOrWhiteSpace(ip))
            {
                string[] addresses = Convert.ToString(ConfigurationManager.AppSettings["AllowedIPAddresses"]).Split(',');
                return addresses.Any(a => ip.Contains(a.Trim()));
            }
            return false;
        }
    }

    public class AuthenticateForBetaAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                base.OnActionExecuting(context);
                return;
            }

            var ip = Utilities.GetClientIPAddress(new HttpRequestWrapper(HttpContext.Current.Request));
            if (ip.StartsWith("10.101.1") && ip.StartsWith("172.21.224"))
            {
                ip = "::1";
            }
            string skipAuth = HttpContext.Current.Request.Cookies["__skipportalauth"] == null ? "" : HttpContext.Current.Request.Cookies["__skipportalauth"].Value;
            if (IsIpAddressAllowed(ip.Trim()) && !string.IsNullOrEmpty(skipAuth))
            {
                Authentication.WriteCookie("__skipportalauth", "y", new TimeSpan(0, 20, 0, 0));
                base.OnActionExecuting(context);
                return;
            }

            // Redirect to Portal Login process
            context.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    {"controller", "Portal"},
                    {"action", "Login"},
                    {"nexturl", context.HttpContext.Request.Url.AbsoluteUri}
                }
            );

            base.OnActionExecuting(context);
        }

        private bool IsIpAddressAllowed(string ip)
        {
            //if (ConfigurationManager.AppSettings["Environment"] != "Live")
            //{
            //    return true;
            //}
            if (!string.IsNullOrWhiteSpace(ip))
            {
                string[] addresses = Convert.ToString(ConfigurationManager.AppSettings["AllowedIPAddresses"]).Split(',');
                return addresses.Any(a => ip.Contains(a.Trim()));
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
                var ipAddress = Utilities.GetClientIPAddress(new HttpRequestWrapper(HttpContext.Current.Request));
                if (ConfigurationManager.AppSettings["Environment"] == "Live")
                {
                    // Live 
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

    /// <summary>
    /// Checks that the current user has authenticated
    /// </summary>
    public class IsAuthenticatedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!Authentication.IsAuthenticated())
            {
                filterContext.Result = new HttpStatusCodeResult(401);
            }
        }
    }

    /// <summary>
    /// ReCaptcha Action Filters and utility class
    /// </summary>
    public class ValidateGoogleCaptchaAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            const string urlToPost = "https://www.google.com/recaptcha/api/siteverify";
            //const string secretKey = ConfigurationManager.AppSettings["ReCaptchaSecretKey"];
            string secretKey = ConfigurationManager.AppSettings["ReCaptchaSecretKey"];
            var captchaResponse = filterContext.HttpContext.Request.Form["g-recaptcha-response"];

            if (string.IsNullOrWhiteSpace(captchaResponse)) AddErrorAndRedirectToGetAction(filterContext);

            var validateResult = ValidateFromGoogle(urlToPost, secretKey, captchaResponse);
            if (!validateResult.Success) AddErrorAndRedirectToGetAction(filterContext);

            base.OnActionExecuting(filterContext);
        }

        private static void AddErrorAndRedirectToGetAction(ActionExecutingContext filterContext)
        {
            filterContext.Controller.TempData["InvalidCaptcha"] = "Invalid Captcha !";
            filterContext.Result = new RedirectToRouteResult(filterContext.RouteData.Values);
        }

        private static ReCaptchaResponse ValidateFromGoogle(string urlToPost, string secretKey, string captchaResponse)
        {
            var postData = "secret=" + secretKey + "&response=" + captchaResponse;

            var request = (HttpWebRequest)WebRequest.Create(urlToPost);
            request.Method = "POST";
            request.ContentLength = postData.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            Utilities.SetTlsVersion();
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                streamWriter.Write(postData);

            string result;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                    result = reader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<ReCaptchaResponse>(result);
        }
    }

    internal class ReCaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("challenge_ts")]
        public string ValidatedDateTime { get; set; }

        [JsonProperty("hostname")]
        public string HostName { get; set; }

        [JsonProperty("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }
}