using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Management;
using System.Net.Mail;
using System.Web.Mvc;
using System.Text;
using netGiant.Intranet.Models;

namespace netGiant.Intranet.Models
{
    public class CustomErrorHandling : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsLocal)
                return;

            CustomErrorEvent errorEvent = new CustomErrorEvent("*Error* in Intranet", this, WebEventCodes.WebExtendedBase + 5, filterContext.Exception);
            errorEvent.Raise();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<b>Event Time</b>: " + errorEvent.EventTime + "<br/><br/>");
            sb.AppendLine("<b>Exception Message</b>: " + errorEvent.ErrorException.Message + "<br/><br/>");
            sb.AppendLine("<b>Exception Type</b>: " + errorEvent.ErrorException.GetType() + "<br/><br/>");
            sb.AppendLine("<b>Request URL</b>: " + errorEvent.RequestInformation.RequestUrl + "<br/><br/>");
            sb.AppendLine("<b>Request Path</b>: " + errorEvent.RequestInformation.RequestPath + "<br/><br/>");
            sb.AppendLine("<b>User</b>: " + HttpContext.Current.User.Identity.Name + "<br/><br/>");
            sb.AppendLine("<b>Stack Trace</b>: " + errorEvent.ErrorException.StackTrace + "<br/><br/>");
            sb.AppendLine("<b>Inner Exception</b>: " + errorEvent.ErrorException.InnerException + "<br/><br/>");
            sb.AppendLine("</body></html>");

            List<string> toAdds = new List<string>();
            toAdds.Add("devteam@netgiant.com");

            Email.SendEmail(errorEvent.ErrorException.GetType().ToString(), sb.ToString(), true, MailPriority.High,
                                toAdds, "intranet@netgiant.com");
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