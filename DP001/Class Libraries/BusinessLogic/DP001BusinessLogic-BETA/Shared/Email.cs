using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Net.Mime;
using System.Configuration;

namespace DP001BusinessLogic.Shared
{
    public class Email
    {
        public static void SendEmail(string body,
            string subject,
            List<string> toAddresses,
            string from,
            MemoryStream attachment = null,
            string attachmentName = "attachment",
            MailPriority mp = MailPriority.Normal)
        {
            if (ConfigurationManager.AppSettings["Platform"] != "Azure")
            {
                SendEmailSmtpAsync(from, toAddresses, subject, body, attachment, attachmentName, mp);
            }
            else
            {
                SendEmailAzureAsync(from, toAddresses, subject, body, attachment, attachmentName, mp);
            }            
        }

        private static async Task SendEmailSmtpAsync(string from, List<string> toAddresses, string subject, string body, MemoryStream attachment, string attachmentName, MailPriority mp)
        {
            using (SmtpClient smtp = new SmtpClient())
            using (MailMessage message = new MailMessage())
            {
                if (ConfigurationManager.AppSettings["Environment"] != "Dev")
                {
                    smtp.Host = "localhost";
                    foreach (string add in toAddresses)
                    {
                        message.To.Add(new System.Net.Mail.MailAddress(add));
                    }
                }
                else
                {
                    smtp.Host = "localhost";
                    message.To.Add(new System.Net.Mail.MailAddress("devteam@netgiant.com"));
                }

                message.BodyEncoding = UTF8Encoding.UTF8;
                message.Body = body;
                message.Subject = subject;
                message.Priority = mp;
                message.IsBodyHtml = true;
                message.From = new MailAddress(from);

                if (attachment != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
                        {
                            var zipArchiveEntry = zipArchive.CreateEntry(attachmentName + ".txt");
                            using (var streamWriter = new StreamWriter(zipArchiveEntry.Open()))
                            {
                                attachment.Position = 0;
                                using (var streamReader = new StreamReader(attachment))
                                {
                                    streamWriter.Write(streamReader.ReadToEnd());
                                }
                            }
                        }

                        var aS = new MemoryStream(memoryStream.ToArray());
                        var aT = new System.Net.Mail.Attachment(aS, attachmentName + ".zip", MediaTypeNames.Application.Zip);
                        message.Attachments.Add(aT);
                    }
                }

                await smtp.SendMailAsync(message);
            }
        }

        private static async Task SendEmailAzureAsync(string from, List<string> toAddresses, string subject, string body, MemoryStream attachment, string attachmentName, MailPriority mp)
        {
            // Via SendGrid
            var apikey = ConfigurationManager.AppSettings["SendGridApiKey"];
            var client = new SendGridClient(apikey);
            var message = new SendGridMessage()
            {
                From = new EmailAddress(from),
                Subject = subject,
                PlainTextContent = body,
                HtmlContent = body
            };
            foreach (string add in toAddresses)
            {
                message.AddTo(add);
            }
            if (attachment != null)
            {
                attachment.Seek(0, SeekOrigin.Begin);       // Reset stream back to beginning
                message.AddAttachment(attachmentName, System.Convert.ToBase64String(attachment.ToArray()));


                //var bytes = System.IO.File.ReadAllBytes(parms["filea"]);
                //var file = Convert.ToBase64String(bytes);
                //message.AddAttachment(parms["filea"], file);
            }
            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                message.AddTo("devteam@netgiant.com");
                message.AddCc("stuart.deavall@netgiant.com");
                string subj = "GENERATED FROM TEST SYSTEM: EmailTo: " + toAddresses[0] + ": " + subject;
                message.Subject = subj.Length < 79 ? subj : subj.Substring(0, 78);
            }
            else
            {
                foreach (string add in toAddresses)
                {
                    message.AddTo(add);
                }
            }

            var response = await client.SendEmailAsync(message);
            //if (!response.IsSuccessStatusCode)
            //{
            //    LogInformationMessage("Error sending email via SendGrid");
            //}
        }

        public async static Task SendEmailAsync(string body,
            string subject,
            List<string> emailAddressesTo,
            string emailAddressFrom,
            MemoryStream attachmentStream = null,
            string attachmentName = "attachment",
            MailPriority mp = MailPriority.Normal)
        {
            //var environment = ConfigurationManager.AppSettings["Environment"];
            //var platform = ConfigurationManager.AppSettings["Platform"];

            //if (platform != "Azure")
            //{
            //    using (SmtpClient smtp = new SmtpClient())
            //    using (MailMessage message = new MailMessage())
            //    {
            //        if (environment != "Dev")
            //        {
            //            smtp.Host = "localhost";
            //        }
            //        else
            //        {
            //            smtp.Host = "Server-SBS";
            //            string emailServerUn = "NetGiantAppAdmin";
            //            string emailServerPw = "netg@pp@dm!N";
            //            NetworkCredential basicCredential = new NetworkCredential(emailServerUn, emailServerPw);
            //            smtp.Credentials = basicCredential;
            //        }

            //        message.BodyEncoding = UTF8Encoding.UTF8;
            //        message.Body = body;
            //        message.Subject = subject;
            //        message.Priority = mp;
            //        message.IsBodyHtml = true;
            //        message.From = new MailAddress(emailAddressFrom);

            //        foreach (string add in emailAddressesTo)
            //        {
            //            message.To.Add(new MailAddress(add));
            //        }

            //        if (attachmentStream != null)
            //        {
            //            using (var memoryStream = new MemoryStream())
            //            {
            //                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
            //                {
            //                    var zipArchiveEntry = zipArchive.CreateEntry(attachmentName + ".txt");
            //                    using (var streamWriter = new StreamWriter(zipArchiveEntry.Open()))
            //                    {
            //                        attachmentStream.Position = 0;
            //                        using (var streamReader = new StreamReader(attachmentStream))
            //                        {
            //                            streamWriter.Write(streamReader.ReadToEnd());
            //                        }
            //                    }
            //                }

            //                var aS = new MemoryStream(memoryStream.ToArray());
            //                var aT = new System.Net.Mail.Attachment(aS, attachmentName + ".zip", MediaTypeNames.Application.Zip);
            //                message.Attachments.Add(aT);
            //            }
            //        }

            //        await smtp.SendMailAsync(message);
            //    }
            //}
            //else
            //{
            //    var message = new SendGridMessage();

            //    message.From = new MailAddress(emailAddressFrom);
            //    message.AddTo(emailAddressesTo);
            //    message.Subject = subject;
            //    message.Html = body;
            //    message.Text = body;

            //    var username = ConfigurationManager.AppSettings["SendGridUsername"];
            //    var password = ConfigurationManager.AppSettings["SendGridPassword"];

            //    var credentials = new NetworkCredential(username, password);
            //    var transportWeb = new Web(credentials);

            //    await transportWeb.DeliverAsync(message);
            //}
        }
    }
}
