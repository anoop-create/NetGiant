using netGiant.Api.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Filters;
using System.Web.Management;

namespace netGiant.Api.ErrorHandling
{
    public class CustomErrorHandling : ExceptionFilterAttribute
    {

        public override void OnException(HttpActionExecutedContext context)
        {
            if (Debugger.IsAttached)
                return;

            CustomErrorEvent errorEvent = new CustomErrorEvent("*Error* in Intranet", this, WebEventCodes.WebExtendedBase + 5, context.Exception);
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
                                toAdds, "web.api@netgiant.com");
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