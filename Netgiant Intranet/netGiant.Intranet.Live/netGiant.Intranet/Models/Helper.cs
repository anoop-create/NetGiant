using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using System.Web.UI.WebControls;

namespace netGiant.Intranet.Models
{
    public class Helper
    {
        public Helper()
        {
            originalPath = new Uri(HttpContext.Current.Request.Url.AbsoluteUri).OriginalString;
        }

        public string originalPath { get; set; }

        public static string GetProductURL(string domain, int? productID)
        {
            string responseFromServer;
            Helper helper = new Helper();

            if (helper.originalPath.ToLower().Contains("localhost"))
                return string.Empty;

            string url = string.Format("https://{0}/ajaxFunctions.asp?a=getProductURL&b={1}", domain, productID.ToString());

            try
            {
                WebRequest request = WebRequest.Create(url);

                if (url.Contains("beta"))
                {
                    CredentialCache cc = new CredentialCache();
                    cc.Add(new Uri(url), "Basic", new NetworkCredential("webadmin", "shadow"));
                    request.Credentials = cc;
                }
                else
                {
                    request.Credentials = CredentialCache.DefaultCredentials;
                }

                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();

                reader.Close();
                response.Close();
            }

            catch (InvalidOperationException e)
            {
                return e.Message + e.StackTrace;
            }

            return responseFromServer;
        }

        public static string GetProductTitle(string domain, int productID)
        {
            string responseFromServer = string.Empty;
            Helper helper = new Helper();

            if (helper.originalPath.ToLower().Contains("localhost"))
                return responseFromServer;

            string url = string.Format("https://{0}/ajaxFunctions.asp?a=getProductTitle&b={1}", domain, productID.ToString());

            try
            {
                WebRequest request = WebRequest.Create(url);

                if (url.Contains("beta"))
                {
                    CredentialCache cc = new CredentialCache();
                    cc.Add(new Uri(url), "Basic", new NetworkCredential("webadmin", "shadow"));
                    request.Credentials = cc;
                }
                else
                {
                    request.Credentials = CredentialCache.DefaultCredentials;
                }

                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();

                reader.Close();
                response.Close();
            }
            catch (InvalidOperationException e)
            {
                return e.Message + e.StackTrace;
            }

            return responseFromServer;
        }

        public static void SendQAEmail(int productID, Website sourceWebsite, string sendTo)
        {
            string emailBody = string.Empty;
            string productURL = GetProductURL(sourceWebsite.WebURL, productID);
            string productTitle = GetProductTitle(sourceWebsite.WebURL, productID);

            try
            {
                //Create email body
                using (StreamReader reader = new StreamReader(string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "Content\\emailTemplates\\QAMail.html")))
                {
                    emailBody = reader.ReadToEnd();
                }

                string supportEmail = sourceWebsite.WebsiteName.Equals("tonergiant", StringComparison.InvariantCultureIgnoreCase) ? "support@tonergiant.co.uk"
                    : sourceWebsite.WebsiteName.Equals("cartridgemonkey", StringComparison.InvariantCultureIgnoreCase) ? "support@cartridgemonkey.com"
                    : "support@netgiant.com";

                MailDefinition md = new MailDefinition();
                md.From = supportEmail;
                md.IsBodyHtml = true;
                md.Subject = string.Format("{0} - You asked a question", sourceWebsite.FriendlyName);

                ListDictionary replacements = new ListDictionary();
                replacements.Add("<%ProductTitle%>", productTitle ?? "");
                replacements.Add("<%Url%>", productURL ?? "");
                replacements.Add("<%Brand%>", sourceWebsite.FriendlyName ?? "");
                replacements.Add("<%SupportEmailAddress%>", supportEmail ?? "");

                MailMessage msg = md.CreateMailMessage(sendTo, replacements, emailBody, new System.Web.UI.Control());

                using (SmtpClient server = new SmtpClient())
                {
                    server.DeliveryMethod = SmtpDeliveryMethod.Network;
                    server.UseDefaultCredentials = false;
                    server.Host = "localhost";
                    server.Send(msg);
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static bool CheckUserIsInRoles(string rolesString)
        {
            bool returnValue = false;

            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(rolesString))
                {
                    string[] strRoles = rolesString.Split(',');
                    foreach (string role in strRoles)
                    {
                        if (HttpContext.Current.User.IsInRole(role))
                        {
                            returnValue = true;
                        }
                    }
                }
                else
                {
                    returnValue = true;
                }
            }

            return returnValue;    
        }
    }

    public static class LinkExtensions
    {
        public static MvcHtmlString ParentActionLink(
            this HtmlHelper htmlHelper,
            string linkText,
            string action,
            string controller
        )
        {
            var anchor = new TagBuilder("a");
            anchor.Attributes["href"] = "#";
            anchor.AddCssClass("activeMenu");
            anchor.SetInnerText(linkText);
            return MvcHtmlString.Create(anchor.ToString());
        }

        public static MvcHtmlString SubActionLink(
            this HtmlHelper htmlHelper,
            string linkText,
            string action,
            string controller,
            int parentLinkID
        )
        {
            var currentAction = htmlHelper.ViewContext.RouteData.GetRequiredString("action");
            var currentController = htmlHelper.ViewContext.RouteData.GetRequiredString("controller");
            if (action == currentAction && controller == currentController)
            {
                var anchor = new TagBuilder("a");
                anchor.Attributes["href"] = "#";
                anchor.AddCssClass("helperActive");
                anchor.SetInnerText(linkText);
                anchor.Attributes.Add("parentLinkID", parentLinkID.ToString());

                return MvcHtmlString.Create(anchor.ToString());
            }
            return htmlHelper.ActionLink(linkText, action, controller, null, new { @parentLinkID = parentLinkID });
        }
    }

    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString EditorForMany<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, IEnumerable<TValue>>> expression, string htmlFieldName = null) where TModel : class
        {
            var items = expression.Compile()(html.ViewData.Model);
            var sb = new StringBuilder();

            if (String.IsNullOrEmpty(htmlFieldName))
            {
                var prefix = html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix;

                htmlFieldName = (prefix.Length > 0 ? (prefix + ".") : String.Empty) + ExpressionHelper.GetExpressionText(expression);
            }

            foreach (var item in items)
            {
                var dummy = new { Item = item };
                var guid = Guid.NewGuid().ToString();

                var memberExp = Expression.MakeMemberAccess(Expression.Constant(dummy), dummy.GetType().GetProperty("Item"));
                var singleItemExp = Expression.Lambda<Func<TModel, TValue>>(memberExp, expression.Parameters);

                sb.Append("<tr>");
                //sb.Append(String.Format(@"<input type=""hidden"" name=""{0}.Index"" value=""{1}"" />", htmlFieldName, guid));
                sb.Append(html.EditorFor(singleItemExp, null, String.Format("{0}[{1}]", htmlFieldName, guid)));
                sb.Append("<td>" + String.Format(@"<input type=""hidden"" name=""{0}.Index"" value=""{1}"" />", htmlFieldName, guid) + "</td>");
                sb.Append("</tr>");
            }

            return new MvcHtmlString(sb.ToString());
        }
    }

    public class JsonModel
    {
        public JsonModel()
        {
            InfoBoxMessage = string.Empty;
        }

        public string HTMLString { get; set; }
        public bool NoMoreData { get; set; }
        public int Count { get; set; }
        public string InfoBoxMessage { get; set; } 
    }

}