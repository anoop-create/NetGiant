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
using SendGrid;

namespace DP001BusinessLogic.Shared
{
    public class Email
    {
        public static void SendEmail(string body,
            string subject,
            List<string> emailAddressesTo,
            string emailAddressFrom,
            MemoryStream attachmentStream = null,
            string attachmentName = "attachment",
            MailPriority mp = MailPriority.Normal)
        {
            var environment = ConfigurationManager.AppSettings["Environment"];
            var platform = ConfigurationManager.AppSettings["Platform"];

            if (platform != "Azure")
            {
                using (SmtpClient smtp = new SmtpClient())
                using (MailMessage message = new MailMessage())
                {
                    if (environment != "Dev")
                    {
                        smtp.Host = "localhost";
                    }
                    else
                    {
                        smtp.Host = "Server-SBS";
                        string emailServerUn = "NetGiantAppAdmin";
                        string emailServerPw = "netg@pp@dm!N";
                        NetworkCredential basicCredential = new NetworkCredential(emailServerUn, emailServerPw);
                        smtp.Credentials = basicCredential;
                    }

                    message.BodyEncoding = UTF8Encoding.UTF8;
                    message.Body = body;
                    message.Subject = subject;
                    message.Priority = mp;
                    message.IsBodyHtml = true;
                    message.From = new MailAddress(emailAddressFrom);

                    foreach (string add in emailAddressesTo)
                    {
                        message.To.Add(new MailAddress(add));
                    }

                    if (attachmentStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
                            {
                                var zipArchiveEntry = zipArchive.CreateEntry(attachmentName + ".txt");
                                using (var streamWriter = new StreamWriter(zipArchiveEntry.Open()))
                                {
                                    attachmentStream.Position = 0;
                                    using (var streamReader = new StreamReader(attachmentStream))
                                    {
                                        streamWriter.Write(streamReader.ReadToEnd());
                                    }
                                }
                            }

                            var aS = new MemoryStream(memoryStream.ToArray());
                            var aT = new Attachment(aS, attachmentName + ".zip", MediaTypeNames.Application.Zip);
                            message.Attachments.Add(aT);
                        }
                    }

                    smtp.Send(message);
                }
            }
            else
            {
                var message = new SendGridMessage();
                
                message.From = new MailAddress(emailAddressFrom);
                message.AddTo(emailAddressesTo);
                message.Subject = subject;
                message.Html = body;
                message.Text = body;

                if (attachmentStream != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
                        {
                            var zipArchiveEntry = zipArchive.CreateEntry(attachmentName + ".txt");
                            using (var streamWriter = new StreamWriter(zipArchiveEntry.Open()))
                            {
                                attachmentStream.Position = 0;
                                using (var streamReader = new StreamReader(attachmentStream))
                                {
                                    streamWriter.Write(streamReader.ReadToEnd());
                                }
                            }
                        }

                        var aS = new MemoryStream(memoryStream.ToArray());
                        message.AddAttachment(aS, attachmentName + ".zip");
                    }
                }

                var username = ConfigurationManager.AppSettings["SendGridUsername"];
                var password = ConfigurationManager.AppSettings["SendGridPassword"];

                var credentials = new NetworkCredential(username, password);
                var transportWeb = new Web(credentials);

                transportWeb.DeliverAsync(message);
            }
        }

        public async static Task SendEmailAsync(string body,
            string subject,
            List<string> emailAddressesTo,
            string emailAddressFrom,
            MemoryStream attachmentStream = null,
            string attachmentName = "attachment",
            MailPriority mp = MailPriority.Normal)
        {
            var environment = ConfigurationManager.AppSettings["Environment"];
            var platform = ConfigurationManager.AppSettings["Platform"];

            if (platform != "Azure")
            {
                using (SmtpClient smtp = new SmtpClient())
                using (MailMessage message = new MailMessage())
                {
                    if (environment != "Dev")
                    {
                        smtp.Host = "localhost";
                    }
                    else
                    {
                        smtp.Host = "Server-SBS";
                        string emailServerUn = "NetGiantAppAdmin";
                        string emailServerPw = "netg@pp@dm!N";
                        NetworkCredential basicCredential = new NetworkCredential(emailServerUn, emailServerPw);
                        smtp.Credentials = basicCredential;
                    }

                    message.BodyEncoding = UTF8Encoding.UTF8;
                    message.Body = body;
                    message.Subject = subject;
                    message.Priority = mp;
                    message.IsBodyHtml = true;
                    message.From = new MailAddress(emailAddressFrom);

                    foreach (string add in emailAddressesTo)
                    {
                        message.To.Add(new MailAddress(add));
                    }

                    if (attachmentStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
                            {
                                var zipArchiveEntry = zipArchive.CreateEntry(attachmentName + ".txt");
                                using (var streamWriter = new StreamWriter(zipArchiveEntry.Open()))
                                {
                                    attachmentStream.Position = 0;
                                    using (var streamReader = new StreamReader(attachmentStream))
                                    {
                                        streamWriter.Write(streamReader.ReadToEnd());
                                    }
                                }
                            }

                            var aS = new MemoryStream(memoryStream.ToArray());
                            var aT = new Attachment(aS, attachmentName + ".zip", MediaTypeNames.Application.Zip);
                            message.Attachments.Add(aT);
                        }
                    }

                    await smtp.SendMailAsync(message);
                }
            }
            else
            {
                var message = new SendGridMessage();

                message.From = new MailAddress(emailAddressFrom);
                message.AddTo(emailAddressesTo);
                message.Subject = subject;
                message.Html = body;
                message.Text = body;

                var username = ConfigurationManager.AppSettings["SendGridUsername"];
                var password = ConfigurationManager.AppSettings["SendGridPassword"];

                var credentials = new NetworkCredential(username, password);
                var transportWeb = new Web(credentials);

                await transportWeb.DeliverAsync(message);
            }
        }
    }
}
