using System;
using System.Collections.Specialized;
using System.IO;
using System.Net.Mail;
using System.Web.UI.WebControls;

namespace NGS.BusinessLayer.BusinessObjects.Shared
{
    public partial class Email
    {
        public string SendFrom { get; set; }
        public string SendTo { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }

        public void Send()
        {   
            MailMessage email = new MailMessage(SendFrom, SendTo);

            try
            {
                email.Subject = Subject;
                email.Body = Body;
                email.IsBodyHtml = true;
                email.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                using (SmtpClient server = new SmtpClient())
                {
                    server.DeliveryMethod = SmtpDeliveryMethod.Network;
                    server.UseDefaultCredentials = false;
                    server.Host = "localhost";
                    server.Send(email);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.GetBaseException().Message);
            }
        }

        public void SendQAEmail(string productTitle, string url, string brand, string supportEmailAddress)
        {
            MailDefinition md = new MailDefinition();
            md.From = this.SendFrom;
            md.IsBodyHtml = true;
            md.Subject = this.Subject;

            ListDictionary replacements = new ListDictionary();
            replacements.Add("<%ProductTitle%>", productTitle);
            replacements.Add("<%Url%>", url);
            replacements.Add("<%Brand%>", brand);
            replacements.Add("<%SupportEmailAddress%>", supportEmailAddress);

            MailMessage msg = md.CreateMailMessage(this.SendTo, replacements, this.Body, new System.Web.UI.Control());

            using (SmtpClient server = new SmtpClient())
            {
                server.DeliveryMethod = SmtpDeliveryMethod.Network;
                server.UseDefaultCredentials = false;
                server.Host = "localhost";
                server.Send(msg);
            }
        }
    }
}
