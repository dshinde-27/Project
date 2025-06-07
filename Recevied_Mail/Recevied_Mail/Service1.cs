using System;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Timers;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace Recevied_Mail
{
    public partial class Service1 : ServiceBase
    {
        private Timer dailyTimer;

        public Service1()
        {
            //OnStart(null);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            dailyTimer = new Timer
            {
                Interval = TimeSpan.FromDays(1).TotalMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            dailyTimer.Elapsed += DailyTimer_Elapsed;
            dailyTimer.Start();

            Task.Run(() => FetchAndStoreEmails());
        }


        protected override void OnStop()
        {
            dailyTimer?.Stop();
            dailyTimer?.Dispose();
        }

        private void DailyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FetchAndStoreEmails();
        }

        private void FetchAndStoreEmails()
        {
            try
            {
                using (var client = new ImapClient())
                {
                    // Gmail IMAP settings
                    client.Connect("imap.gmail.com", 993, true);

                    // Use app password or OAuth2 (for production)
                    client.Authenticate("dhanashreeshinde1027@gmail.com", "xrsk hyjf jvlx keeo");

                    client.Inbox.Open(FolderAccess.ReadOnly);

                    // Get last 1000 messages
                    var uids = client.Inbox.Search(SearchQuery.All);
                    var lastUids = uids.Skip(Math.Max(0, uids.Count - 1000)).ToList();
                    var messages = client.Inbox.Fetch(lastUids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);

                    foreach (var summary in messages)
                    {
                        var message = client.Inbox.GetMessage(summary.UniqueId);
                        SaveEmailToDatabase(message);
                    }

                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log or handle exception
            }
        }

        private void SaveEmailToDatabase(MimeMessage message)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MailDb"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var attachmentNames = new List<string>();
                foreach (var attachment in message.Attachments)
                {
                    var name = attachment.ContentDisposition?.FileName ?? attachment.ContentType?.Name;
                    if (!string.IsNullOrEmpty(name))
                        attachmentNames.Add(name);
                }
                string joinedNames = string.Join(",", attachmentNames);

                var cmd = new SqlCommand(@"
            INSERT INTO [Marketa].[dbo].[InboxEmails]
                ([ReceivedTime], [FromEmail], [ToEmail], [Subject], [Body], [AttachmentNames], [MessageId])
            VALUES
                (@ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @AttachmentNames, @MessageId)", conn);

                cmd.Parameters.AddWithValue("@ReceivedTime", message.Date.DateTime);
                cmd.Parameters.AddWithValue("@FromEmail", message.From.ToString());
                cmd.Parameters.AddWithValue("@ToEmail", message.To.ToString());
                cmd.Parameters.AddWithValue("@Subject", message.Subject ?? string.Empty);
                cmd.Parameters.AddWithValue("@Body", message.TextBody ?? message.HtmlBody ?? string.Empty);
                cmd.Parameters.AddWithValue("@AttachmentNames", joinedNames);
                cmd.Parameters.AddWithValue("@MessageId", message.MessageId ?? Guid.NewGuid().ToString());

                cmd.ExecuteNonQuery();
            }
        }

    }
}
