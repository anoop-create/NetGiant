using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace netGiant.Intranet.Models
{
    public class Email
    {
        public static void SendEmail(string subject, string body, bool isHTML, MailPriority priority,
                                        List<string> toAddresses, string fromAddress)
        {
            SmtpClient smtp = new SmtpClient();

            if (!HttpContext.Current.Request.IsLocal)
            {
                smtp.Host = "localhost";
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
            }
            else
            {
                smtp.Host = "10.101.1.1";
                NetworkCredential nc = new NetworkCredential();
                nc.UserName = "NetGiantAppAdmin";
                nc.Password = "netg@pp@dm!N";
                smtp.Credentials = nc;
            }

            MailMessage message = new MailMessage();
            message.IsBodyHtml = isHTML;
            message.Body = body;
            message.Subject = subject;

            foreach(string add in toAddresses)
            {
                message.To.Add(new MailAddress(add));
            }

            message.From = new MailAddress(fromAddress);
            message.Priority = priority;
            smtp.Send(message);
            smtp.Dispose();
        }
    }
}