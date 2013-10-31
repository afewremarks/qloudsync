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
            
            //log snapshot
            string log_snapshot = RuntimeSettings.ConfigPath + Path.DirectorySeparatorChar + "log_snapshot.txt";
            File.Delete(log_snapshot);
            File.Copy(RuntimeSettings.LogFilePath, log_snapshot);
            System.Net.Mail.Attachment log_attachment;
            log_attachment = new System.Net.Mail.Attachment(log_snapshot);
            mail.Attachments.Add(log_attachment);

            //machine snapshot
            string machine_snapshot = RuntimeSettings.ConfigPath + Path.DirectorySeparatorChar + "machine_snapshot.txt";
            RuntimeSettings.BuildMachineSnapshotFile(machine_snapshot);
            System.Net.Mail.Attachment machine_attachment;
            machine_attachment = new System.Net.Mail.Attachment(machine_snapshot);
            mail.Attachments.Add(machine_attachment);

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential(ConfigFile.GetInstance().Read("email_sender"), ConfigFile.GetInstance().Read("email_password"));
            SmtpServer.EnableSsl = true;

            SmtpServer.Send(mail);
        
        }
    }
}
