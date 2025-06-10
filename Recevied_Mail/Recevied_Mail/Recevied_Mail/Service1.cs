using System;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Timers;
using MailKit.Net.Imap;
using MailKit;
using MimeKit;
using System.Linq;

namespace Recevied_Mail
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer;
        private string connectionString = "Server=localhost;Database=Marketa;Integrated Security=True;";

        public Service1()
        {
            OnStart(null);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer = new Timer(60000); // Run every 1 minute
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FetchAndStoreEmails();
        }

        private void FetchAndStoreEmails()
        {
            using (var client = new ImapClient())
            {
                client.Connect("imap.gmail.com", 993, true);
                client.Authenticate("dhanashreeshinde1027@gmail.com", "xrsk hyjf jvlx keeo");

                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadOnly);

                foreach (var message in inbox.Fetch(0, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
                {
                    var fullMessage = inbox.GetMessage(message.UniqueId);

                    InsertIntoEmails(fullMessage);
                    InsertIntoInboxEmails(fullMessage);
                }

                client.Disconnect(true);
            }
        }

        private void InsertIntoEmails(MimeMessage message)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    INSERT INTO Emails (SentTime, ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames)
                    VALUES (@SentTime, @ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @Attachments)", conn);

                cmd.Parameters.AddWithValue("@SentTime", message.Date.DateTime);
                cmd.Parameters.AddWithValue("@ReceivedTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@FromEmail", message.From.ToString());
                cmd.Parameters.AddWithValue("@ToEmail", message.To.ToString());
                cmd.Parameters.AddWithValue("@Subject", message.Subject ?? "");
                cmd.Parameters.AddWithValue("@Body", message.TextBody ?? "");
                cmd.Parameters.AddWithValue("@Attachments", string.Join(",", message.Attachments.Select(a => a.ContentDisposition?.FileName)));

                cmd.ExecuteNonQuery();
            }
        }

        private void InsertIntoInboxEmails(MimeMessage message)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    INSERT INTO InboxEmails (ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames, MessageId)
                    VALUES (@ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @Attachments, @MessageId)", conn);

                cmd.Parameters.AddWithValue("@ReceivedTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@FromEmail", message.From.ToString());
                cmd.Parameters.AddWithValue("@ToEmail", message.To.ToString());
                cmd.Parameters.AddWithValue("@Subject", message.Subject ?? "");
                cmd.Parameters.AddWithValue("@Body", message.TextBody ?? "");
                cmd.Parameters.AddWithValue("@Attachments", string.Join(",", message.Attachments.Select(a => a.ContentDisposition?.FileName)));
                cmd.Parameters.AddWithValue("@MessageId", message.MessageId ?? "");

                cmd.ExecuteNonQuery();
            }
        }

        protected override void OnStop()
        {
            timer?.Stop();
        }
    }
}
