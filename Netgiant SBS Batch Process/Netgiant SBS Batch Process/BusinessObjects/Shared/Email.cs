using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace ngSBSBatchProcesses.BusinessObjects.Shared
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
            var settings = Properties.Settings.Default;
            SmtpClient smtp;

            smtp = new SmtpClient("localhost");

            MailMessage message = new MailMessage();
            message.BodyEncoding = UTF8Encoding.UTF8;
            message.Body = body;
            message.Subject = subject;
            message.Priority = mp;
            message.From = new MailAddress(settings.smtpFrom);

            foreach (string add in emailAddresses)
            {
                message.To.Add(new MailAddress(add));
            }

            if (attachmentFilePath != "")
            {
                Attachment messageAttachment = new Attachment(attachmentFilePath);
                message.Attachments.Add(messageAttachment);
            }

            smtp.Send(message);
        }
    }
}
