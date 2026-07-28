using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Objects;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;

namespace netGiant.Intranet.BusinessLayer.Utilities
{
    public class EmailUtilities
    {
        public static async Task SendEmail(string subject, string body, bool isHTML, MailPriority priority,
            List<string> toAddresses, string fromAddress)
        {
            if (HttpContext.Current.Request.IsLocal)
                return;

            if (ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                // Via SendGrid
                var apikey = SharedFunctions.GetConfigurationSetting("Website Application Variables", "SendGridApiKey");
                var client = new SendGridClient(apikey);
                var message = new SendGridMessage()
                {
                    From = new EmailAddress(fromAddress)
                    , Subject = subject
                    //, PlainTextContent = body
                    //, HtmlContent = body
                };
                message.AddContent(isHTML ? MimeType.Html : MimeType.Text, body);
                //if (!String.IsNullOrEmpty(attachmentFilePath))
                //{
                //    var bytes = System.IO.File.ReadAllBytes(attachmentFilePath);
                //    var file = Convert.ToBase64String(bytes);
                //    message.AddAttachment(attachmentFilePath, file);
                //}
                if (ConfigurationManager.AppSettings["Environment"] != "Live")
                {
                    message.AddTo("devteam@netgiant.com");
                    message.AddCc("stuart.deavall@netgiant.com");
                    string subj = "GENERATED FROM TEST SYSTEM: EmailTo: " + toAddresses[0] + ": " + subject;
                    message.Subject = subj.Length < 79 ? subj : subj.Substring(0, 78);
                }
                else
                {
                    for (int i = 0; i < toAddresses.Count; i++)
                    {
                        message.AddTo(toAddresses[i]);
                    }
                    //if (bcc != "")
                    //{
                    //    message.AddBcc(bcc);
                    //}
                }

                var response = await client.SendEmailAsync(message);
                if (!response.IsSuccessStatusCode)
                {
                    throw new ApplicationException("ERROR Unable to send email from SendGrid");
                }
            }
            else
            {
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                    Subject = subject,
                    Body = body,
                };
                foreach (string add in toAddresses)
                {
                    mail.To.Add(new MailAddress(add));
                }

                if (ConfigurationManager.AppSettings["Environment"] != "Live")
                {
                    mail.To.Clear();
                    mail.To.Add(new MailAddress("devteam@netgiant.com"));
                    mail.CC.Add(new MailAddress("stuart.deavall@netgiant.com"));
                    mail.Subject = "GENERATED FROM TEST SYSTEM: EmailTo: " + String.Join(",", toAddresses) + ": " + subject;
                }

                mail.IsBodyHtml = isHTML;
                SmtpClient smtp = new SmtpClient("localhost");
                try
                {
                    smtp.Send(mail);
                    await smtp.SendMailAsync(mail);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message + ex.StackTrace);
                }
            }
        }
    }
}
