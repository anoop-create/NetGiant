using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.IO.Compression;
using System.IO;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    class Email
    {
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

                    Attachment messageAttachment = new Attachment(zipFilePath);
                    message.Attachments.Add(messageAttachment);
                }

                smtp.Send(message);
            }

            if (attachmentFilePath != "")
                File.Delete(zipFilePath);
        }

        public static void SendEmail(List<string> toAddresses,
                                        string fromAddress,
                                        string subject,
                                        string body,
                                        bool isHTML,
                                        string attachmentFilePath = "",
                                        string bccEmail = "")
        {
            MailMessage mail = new MailMessage();

            mail.From = new MailAddress(fromAddress);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = isHTML;

            for (int i = 0; i < toAddresses.Count; i++)
            {
                mail.To.Add(new MailAddress(toAddresses[i]));
            }

            if (!String.IsNullOrEmpty(attachmentFilePath))
            {
                mail.Attachments.Add(new Attachment(attachmentFilePath));
            }

            if (!String.IsNullOrEmpty(bccEmail))
            {
                mail.Bcc.Add(new MailAddress(bccEmail));
            }

            SmtpClient smtp = new SmtpClient("localhost");

            smtp.Send(mail);
            smtp.Dispose();
        }
    }
}
