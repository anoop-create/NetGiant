using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.EntityClient;
using System.Data.SqlClient;
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
            // NOTE: the previous "if (Request.IsLocal) return;" guard here has been removed. IsLocal
            // compares REMOTE_ADDR to LOCAL_ADDR, which is unreliable behind a reverse proxy/load balancer
            // (this server's IIS logs show s-ip 10.0.0.8 fronting the real client IP) - it was very likely
            // evaluating true on this beta server itself, causing this entire method to return immediately
            // on every single error, before the database write or email below ever ran. That would explain
            // why dbo.IntranetErrorLog has stayed empty and no error email has ever arrived, regardless of
            // how this method's internals were hardened - none of that code was ever being reached.

            filterContext.ExceptionHandled = true;
            Exception ex = filterContext.Exception;

            // Carry the real exception across the redirect via TempData (survives exactly one redirect -
            // this is what it's for) so AdminController.Error() can show it directly.
            try
            {
                filterContext.Controller.TempData["LastError"] = ex;
            }
            catch
            {
            }

            // Everything below is best-effort diagnostics/notification and must NEVER throw uncaught -
            // there is no filter above this one to catch a secondary failure, so it would escape straight
            // to IIS's own <customErrors>, masking the REAL exception (ex) behind an unrelated one.

            // 1. Log to the database FIRST, directly from filterContext.Exception, before anything else
            // that could fail - so there's always a record even if every other step below fails.
            string dbLogError = LogErrorToDatabase(ex, filterContext.HttpContext);

            // 2. Raise the health-monitoring event - isolated, since this alone can throw depending on
            // server configuration and must never be allowed to mask the real exception.
            try
            {
                CustomErrorEvent errorEvent = new CustomErrorEvent("*Error* in Intranet", this, WebEventCodes.WebExtendedBase + 5, ex);
                errorEvent.Raise();
            }
            catch
            {
                // Health monitoring's own failure must never become the exception that reaches the user.
            }

            // 3. Email devteam@netgiant.com - also isolated for the same reason.
            try
            {
                SendErrorEmail(ex, filterContext.HttpContext, dbLogError);
            }
            catch
            {
            }

            // area = "" is required here - without it, MVC inherits whatever Area the failing request
            // was executing in (e.g. "PMS") and tries to redirect to a non-existent "/PMS/Admin/Error"
            // instead of the real root "/Admin/Error", which then 404s and triggers a second, confusing
            // IIS-level customErrors redirect on top of this one.
            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary(
                    new
                    {
                        area = "",
                        action = "Error",
                        controller = "Admin"
                    }));
        }

        private void SendErrorEmail(Exception ex, HttpContextBase httpContext, string dbLogError)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<b>Event Time</b>: " + DateTime.Now + "<br/><br/>");
            sb.AppendLine("<b>Exception Message</b>: " + ex.Message + "<br/><br/>");
            sb.AppendLine("<b>Exception Type</b>: " + ex.GetType() + "<br/><br/>");
            sb.AppendLine("<b>Request URL</b>: " + httpContext.Request.Url + "<br/><br/>");

            sb.AppendLine("<b>ServerVariables REMOTE_ADDR</b>: " + httpContext.Request.ServerVariables["REMOTE_ADDR"] + "<br/>");
            sb.AppendLine("<b>Header X-Real-IP</b>: " + httpContext.Request.Headers["X-Real-IP"] + "<br/>");
            sb.AppendLine("<b>Request UserHostAddress</b>: " + httpContext.Request.UserHostAddress + "<br/>");
            sb.AppendLine("<b>Headers X-FORWARDED-FOR</b>: " + httpContext.Request.Headers["X-FORWARDED-FOR"] + "<br/>");
            sb.AppendLine("<b>Headers X-Forwarded-IP</b>: " + httpContext.Request.Headers["X-Forwarded-IP"] + "<br/>");
            sb.AppendLine("<b>ServerVariables HTTP_CLIENT_IP</b>: " + httpContext.Request.ServerVariables["HTTP_CLIENT_IP"] + "<br/><br />");

            sb.AppendLine("<b>User Agent</b>: " + httpContext.Request.ServerVariables["http_user_agent"] + "<br/><br/>");
            sb.AppendLine("<b>Request Path</b>: " + httpContext.Request.Path + "<br/><br/>");
            sb.AppendLine("<b>User</b>: " + (httpContext.User?.Identity?.Name ?? "(none)") + "<br/><br/>");
            sb.AppendLine("<b>Stack Trace</b>: " + ex.StackTrace + "<br/><br/>");
            sb.AppendLine("<b>Inner Exception</b>: " + ex.InnerException + "<br/><br/>");

            // Surfaced here (rather than swallowed) so a problem writing to dbo.IntranetErrorLog is visible
            // in the one channel we already know works, instead of disappearing silently.
            if (dbLogError != null)
            {
                sb.AppendLine("<b>DB Logging Error</b> (failed to write to dbo.IntranetErrorLog): " + dbLogError + "<br/><br/>");
            }

            sb.AppendLine("</body></html>");

            List<string> toAdds = new List<string>();
            toAdds.Add("devteam@netgiant.com");

            EmailUtilities.SendEmail(ex.GetType().ToString(), sb.ToString(), true, MailPriority.High,
                                toAdds, "intranet@netgiant.com");
        }

        // Writes the exception to dbo.IntranetErrorLog (see CreateIntranetErrorLog.sql) so it's queryable
        // without waiting on the devteam@netgiant.com email. Deliberately reuses the "ngmdEntities"
        // connection string that already resolves correctly everywhere else in this app (it lives in
        // machine.config on the server, not in this project's Web.config) rather than hardcoding a
        // separate connection string here - that way this always points at the same database as the rest
        // of the app, even if the real credentials on the server ever change.
        // Returns null on success, or the caught exception's message on failure - the caller surfaces that
        // in the email instead of it disappearing silently, so a DB-logging problem is diagnosable without
        // needing a second round trip.
        private string LogErrorToDatabase(Exception ex, HttpContextBase httpContext)
        {
            try
            {
                string entityConnStr = ConfigurationManager.ConnectionStrings["ngmdEntities"].ConnectionString;
                string sqlConnStr = new EntityConnectionStringBuilder(entityConnStr).ProviderConnectionString;

                using (SqlConnection conn = new SqlConnection(sqlConnStr))
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO dbo.IntranetErrorLog
                        (ExceptionType, ExceptionMessage, StackTrace, InnerException, RequestUrl, RequestPath, UserName, RemoteAddr, UserAgent)
                    VALUES
                        (@ExceptionType, @ExceptionMessage, @StackTrace, @InnerException, @RequestUrl, @RequestPath, @UserName, @RemoteAddr, @UserAgent)", conn))
                {
                    cmd.Parameters.AddWithValue("@ExceptionType", (object)ex.GetType().ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ExceptionMessage", (object)ex.Message ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@StackTrace", (object)ex.StackTrace ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@InnerException", (object)ex.InnerException?.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RequestUrl", (object)httpContext?.Request?.Url?.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RequestPath", (object)httpContext?.Request?.Path ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", (object)httpContext?.User?.Identity?.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RemoteAddr", (object)httpContext?.Request?.ServerVariables["REMOTE_ADDR"] ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserAgent", (object)httpContext?.Request?.ServerVariables["http_user_agent"] ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return null;
            }
            catch (Exception logEx)
            {
                return logEx.GetType() + ": " + logEx.Message +
                    (logEx.InnerException != null ? " | Inner: " + logEx.InnerException.Message : "");
            }
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
