using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Management;
using Microsoft.ApplicationInsights;

namespace DP001Website.Models
{
    public class CustomErrorHandling : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsLocal || IsIgnoredException(filterContext.Exception))
                return;

            //var environment = ConfigurationManager.AppSettings["Environment"];

            CustomErrorEvent errorEvent = new CustomErrorEvent("*Error* in Priceology", this, WebEventCodes.WebExtendedBase + 5, filterContext.Exception);
            errorEvent.Raise();

            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine("<html><body>");
            //sb.AppendLine("<b>Event Time</b>: " + errorEvent.EventTime + "<br/><br/>");
            //sb.AppendLine("<b>Exception Message</b>: " + errorEvent.ErrorException.Message + "<br/><br/>");
            //sb.AppendLine("<b>Exception Type</b>: " + errorEvent.ErrorException.GetType() + "<br/><br/>");
            //sb.AppendLine("<b>Request URL</b>: " + errorEvent.RequestInformation.RequestUrl + "<br/><br/>");
            //sb.AppendLine("<b>Request Path</b>: " + errorEvent.RequestInformation.RequestPath + "<br/><br/>");
            //sb.AppendLine("<b>User</b>: " + HttpContext.Current.User.Identity.Name + "<br/><br/>");
            //sb.AppendLine("<b>Stack Trace</b>: " + errorEvent.ErrorException.StackTrace + "<br/><br/>");
            //sb.AppendLine("<b>Inner Exception</b>: " + errorEvent.ErrorException.InnerException + "<br/><br/>");
            //sb.AppendLine("</body></html>");

            //List<string> toAdds = new List<string>();
            //toAdds.Add("service.admin@priceology.io");

            //Email.SendEmail(
            //    sb.ToString(),
            //    "*Error* in " + environment + " Priceology",
            //    toAdds,
            //    "error@priceology.io",
            //    null,
            //    "",
            //    MailPriority.High);

            //Log with Application Insights
            var ai = new TelemetryClient();
            var propDictionary = new Dictionary<string, string>();
            propDictionary.Add("User", HttpContext.Current.User.Identity.Name);

            ai.TrackException(filterContext.Exception, propDictionary);
        }

        private bool IsIgnoredException(Exception exception)
        {
            var isIgnored = false;

            if (exception is HttpAntiForgeryException)
            {
                isIgnored = true;
            }

            return isIgnored;
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