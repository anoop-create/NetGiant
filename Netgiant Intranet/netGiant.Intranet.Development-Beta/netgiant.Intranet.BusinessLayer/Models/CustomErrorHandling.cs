using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Management;
using System.Net.Mail;
using System.Web.Mvc;
using System.Text;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.Web.Routing;

namespace netGiant.Intranet.BusinessLayer.Models
{
    public class CustomErrorHandling : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsLocal)
                return;

            filterContext.ExceptionHandled = true;

            CustomErrorEvent errorEvent = new CustomErrorEvent("*Error* in Intranet", this, WebEventCodes.WebExtendedBase + 5, filterContext.Exception);
            errorEvent.Raise();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<b>Event Time</b>: " + errorEvent.EventTime + "<br/><br/>");
            sb.AppendLine("<b>Exception Message</b>: " + errorEvent.ErrorException.Message + "<br/><br/>");
            sb.AppendLine("<b>Exception Type</b>: " + errorEvent.ErrorException.GetType() + "<br/><br/>");
            sb.AppendLine("<b>Request URL</b>: " + errorEvent.RequestInformation.RequestUrl + "<br/><br/>");

            sb.AppendLine("<b>ServerVariables REMOTE_ADDR</b>: " + HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"] + "<br/>");
            sb.AppendLine("<b>Header X-Real-IP</b>: " + HttpContext.Current.Request.Headers["X-Real-IP"] + "<br/>");
            sb.AppendLine("<b>Request UserHostAddress</b>: " + HttpContext.Current.Request.UserHostAddress + "<br/>");
            sb.AppendLine("<b>Headers X-FORWARDED-FOR</b>: " + HttpContext.Current.Request.Headers["X-FORWARDED-FOR"] + "<br/>");
            sb.AppendLine("<b>Headers X-Forwarded-IP</b>: " + HttpContext.Current.Request.Headers["X-Forwarded-IP"] + "<br/>");
            sb.AppendLine("<b>ServerVariables HTTP_CLIENT_IP</b>: " + HttpContext.Current.Request.ServerVariables["HTTP_CLIENT_IP"] + "<br/><br />");

            sb.AppendLine("<b>User Agent</b>: " + HttpContext.Current.Request.ServerVariables["http_user_agent"] + "<br/><br/>");
            sb.AppendLine("<b>Request Path</b>: " + errorEvent.RequestInformation.RequestPath + "<br/><br/>");
            sb.AppendLine("<b>User</b>: " + HttpContext.Current.User.Identity.Name + "<br/><br/>");
            sb.AppendLine("<b>Stack Trace</b>: " + errorEvent.ErrorException.StackTrace + "<br/><br/>");
            sb.AppendLine("<b>Inner Exception</b>: " + errorEvent.ErrorException.InnerException + "<br/><br/>");
            sb.AppendLine("</body></html>");

            List<string> toAdds = new List<string>();
            toAdds.Add("devteam@netgiant.com");

            EmailUtilities.SendEmail(errorEvent.ErrorException.GetType().ToString(), sb.ToString(), true, MailPriority.High,
                                toAdds, "intranet@netgiant.com");

            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary(
                    new
                    {
                        action = "Error",
                        controller = "Admin"
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