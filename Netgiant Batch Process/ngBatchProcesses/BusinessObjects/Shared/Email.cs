using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.IO.Compression;
using System.IO;
using System.Configuration;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Google.Apis.Util;
using ngBatchProcesses.BusinessObjects.EcommerceWebsite;
using System.Web;
using MailChimp.Net.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    class Email
    {
        public static async Task EmailTest(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();

            if (ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                // Via SendGrid
                var apikey = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "Website Application Variables" && x.settingName == "SendGridApiKey").FirstOrDefault().settingValue;
                var client = new SendGridClient(apikey);
                var message = new SendGridMessage()
                {
                    From = new EmailAddress("batch.program@netgiant.com")
                    ,
                    Subject = "Test Email"
                    //, PlainTextContent = "Test Body"
                    //, HtmlContent = "Test Body"
                };
                if (!String.IsNullOrEmpty(parms["filea"]))
                {
                    var bytes = System.IO.File.ReadAllBytes(parms["filea"]);
                    var file = Convert.ToBase64String(bytes);
                    message.AddAttachment(parms["filea"], file);
                }
                message.AddTo(parms["output"]);
                message.AddContent(MimeType.Text, "Test Body");

                var response = await client.SendEmailAsync(message);
                if (!response.IsSuccessStatusCode)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to send email from SendGrid", ErrorCode = "ERROR" });
                }
            }
            else
            {
                MailMessage message = new MailMessage();

                message.To.Add(new MailAddress(parms["output"]));
                message.From = new MailAddress("batch.program@netgiant.com");
                message.Subject = "Test Email";
                message.Body = "Test Body";
                message.IsBodyHtml = false;
                if (!String.IsNullOrEmpty(parms["filea"]))
                {
                    message.Attachments.Add(new System.Net.Mail.Attachment(parms["filea"]));
                }

                SmtpClient smtp = new SmtpClient("localhost");
                smtp.Send(message);
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
        public static void SendEmail(string switchUsed,
                                        string body, 
                                        string subject, 
                                        List<string> emailAddresses,
                                        string attachmentFilePath = "",
                                        MailPriority mp = MailPriority.Normal)
        {
            var smtpFrom = (string)Properties.Settings.Default["smtpFrom"];
            var zipFilePath = Properties.Settings.Default.LocalDirectory + "PMSTempData\\" + Guid.NewGuid() + ".zip";

            using (SmtpClient smtp = new SmtpClient())
            using (MailMessage message = new MailMessage())
            {
                smtp.Host = "localhost";

                message.BodyEncoding = UTF8Encoding.UTF8;
                message.Body = body;
                message.Subject = subject;
                message.Priority = mp;
                message.From = new MailAddress(smtpFrom);

                foreach (string add in emailAddresses)
                {
                    message.To.Add(new MailAddress(add));
                }

                if (attachmentFilePath != "")
                {
                    using (ZipArchive newZip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                    {
                        newZip.CreateEntryFromFile(attachmentFilePath, Path.GetFileName(attachmentFilePath));
                    }

                    System.Net.Mail.Attachment messageAttachment = new System.Net.Mail.Attachment(zipFilePath);
                    message.Attachments.Add(messageAttachment);
                }

                smtp.Send(message);
            }

            if (attachmentFilePath != "")
                File.Delete(zipFilePath);
        }

        public static void SendEmail(List<string> toAddresses,
                                        string from,
                                        string subject,
                                        string body,
                                        bool isHTML,
                                        string attachmentFilePath = "",
                                        string bcc = "")
        {
            if (ConfigurationManager.AppSettings["Environment"] == "Local")
                return;

            if (ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                SendEmailAzureAsync(from, toAddresses, subject, body, bcc, isHTML, attachmentFilePath);
            }
            else
            {
                SendEmailSmtp(from, toAddresses, subject, body, bcc, isHTML, attachmentFilePath);
            }            
        }

        private static void SendEmailSmtp(string from, List<string> toAddresses, string subject, string body, string bcc, bool isHTML, string attachmentFilePath)
        {
            MailMessage mail = new MailMessage();

            mail.From = new MailAddress(from);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = isHTML;
            for (int i = 0; i < toAddresses.Count; i++)
            {
                mail.To.Add(new MailAddress(toAddresses[i]));
            }
            if (!String.IsNullOrEmpty(attachmentFilePath))
            {
                mail.Attachments.Add(new System.Net.Mail.Attachment(attachmentFilePath));
            }

            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                mail.To.Clear();
                mail.To.Add(new MailAddress("devteam@netgiant.com"));
                mail.CC.Add(new MailAddress(EntityFunctions.GetNgmdCMSEntry(1, "EmailData", "TestEmail")));
                string subj = "GENERATED FROM TEST SYSTEM: EmailTo: " + toAddresses[0] + ": " + subject;
                mail.Subject = subj.Length < 79 ? subj : subj.Substring(0, 78);
            }
            else if (bcc != "")
            {
                mail.Bcc.Add(new MailAddress(bcc));
            }

            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient("localhost");
            try
            {
                smtp.Send(mail);
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to send email", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(e);
            }
        }

        private static async Task SendEmailAzureAsync(string from, List<string> toAddresses, string subject, string body, string bcc, bool isHTML, string attachmentFilePath)
        {
            // Via SendGrid
            string apikey = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "Website Application Variables" && x.settingName == "SendGridApiKey").FirstOrDefault().settingValue;
            var client = new SendGridClient(apikey);
            var message = new SendGridMessage()
            {
                From = new EmailAddress(from)
                , Subject = subject
                //, PlainTextContent = body
                //, HtmlContent = body
            };
            message.AddContent(isHTML ? MimeType.Html : MimeType.Text, body);
            if (!String.IsNullOrEmpty(attachmentFilePath))
            {
                var bytes = System.IO.File.ReadAllBytes(attachmentFilePath);
                var file = Convert.ToBase64String(bytes);
                message.AddAttachment(attachmentFilePath, file);
            }
            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                message.AddTo("devteam@netgiant.com");
                message.AddCc(EntityFunctions.GetNgmdCMSEntry(1, "EmailData", "TestEmail"));
                string subj = "GENERATED FROM TEST SYSTEM: EmailTo: " + toAddresses[0] + ": " + subject;
                message.Subject = subj.Length < 79 ? subj : subj.Substring(0, 78);
            }
            else
            {
                for (int i = 0; i < toAddresses.Count; i++)
                {
                    message.AddTo(toAddresses[i]);
                }
                if (bcc != "")
                {
                    message.AddBcc(bcc);
                }
            }

            var response = await client.SendEmailAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to send email from SendGrid", ErrorCode = "ERROR" });
            }
        }
    }
}
