using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace GreenQloud
{
    public class SendMail
    {

        public void SendBugMessage(string explanation) {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress(ConfigFile.GetInstance().Read("email_sender"));
            mail.To.Add("qloudsync.bugs@greenqloud.com");
            mail.Subject = "Bug Report";
            mail.Body = explanation;
            string snapshot = RuntimeSettings.ConfigPath + Path.DirectorySeparatorChar + "log_snapshot.txt";
            File.Delete(snapshot);
            File.Copy(RuntimeSettings.LogFilePath, snapshot);
            System.Net.Mail.Attachment attachment;
            attachment = new System.Net.Mail.Attachment(snapshot);
            mail.Attachments.Add(attachment);

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential(ConfigFile.GetInstance().Read("email_sender"), ConfigFile.GetInstance().Read("email_password"));
            SmtpServer.EnableSsl = true;

            SmtpServer.Send(mail);
        
        }
    }
}
