using System;
using System.Configuration;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Timers;
using MailKit.Net.Imap;
using MailKit;
using MimeKit;
using System.Linq;
using NLog;

namespace Recevied_Mail
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer;
        private string connectionString;
        private string imapHost;
        private int imapPort;
        private string emailAddress;
        private string emailPassword;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Service1()
        {
           // OnStart(null);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            logger.Info("Service is starting...");

            try
            {
                LoadConfig();

                timer = new Timer(60000); // 1 minute
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = true;
                timer.Start();

                logger.Info("Timer started successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during OnStart");
            }
        }

        protected override void OnStop()
        {
            timer?.Stop();
            timer?.Dispose();
            logger.Info("Service stopped.");
        }

        private void LoadConfig()
        {
            connectionString = ConfigurationManager.ConnectionStrings["MarketaDb"]?.ConnectionString
                               ?? throw new InvalidOperationException("Missing MarketaDb connection string.");
            imapHost = ConfigurationManager.AppSettings["ImapHost"]
                       ?? throw new InvalidOperationException("ImapHost is missing.");
            if (!int.TryParse(ConfigurationManager.AppSettings["ImapPort"], out imapPort))
                throw new InvalidOperationException("Invalid ImapPort.");
            emailAddress = ConfigurationManager.AppSettings["EmailAddress"]
                           ?? throw new InvalidOperationException("EmailAddress is missing.");
            emailPassword = ConfigurationManager.AppSettings["EmailPassword"]
                            ?? throw new InvalidOperationException("EmailPassword is missing.");

            logger.Info("Configuration loaded successfully.");
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                logger.Info($"Timer triggered at {DateTime.Now}");
                FetchAndStoreEmails();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in Timer_Elapsed");
            }
        }

        private void FetchAndStoreEmails()
        {
            int successCount = 0, failCount = 0;

            try
            {
                using (var client = new ImapClient())
                {
                    client.Connect(imapHost, imapPort, true);
                    client.Authenticate(emailAddress, emailPassword);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);

                    var summaries = inbox.Fetch(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope);

                    foreach (var summary in summaries)
                    {
                        try
                        {
                            var message = inbox.GetMessage(summary.UniqueId);

                            if (InsertEmailAndInboxIfNotExists(message))
                            {
                                successCount++;
                            }
                            else
                            {
                                logger.Info($"Skipped duplicate email with MessageId: {message.MessageId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            logger.Error(ex, $"Error processing message UID {summary.UniqueId}");
                        }
                    }

                    client.Disconnect(true);
                }

                logger.Info($"Email fetch complete. Success: {successCount}, Fail: {failCount}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Critical error in FetchAndStoreEmails");
            }
        }

        private bool InsertEmailAndInboxIfNotExists(MimeMessage message)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var checkCmd = new SqlCommand(
                        "SELECT COUNT(1) FROM Emails WHERE MessageId = @MessageId", conn);
                    checkCmd.Parameters.AddWithValue("@MessageId", message.MessageId ?? "");

                    if ((int)checkCmd.ExecuteScalar() > 0)
                    {
                        return false; // Email already exists
                    }

                    var insertEmailCmd = new SqlCommand(@"
                        INSERT INTO Emails
                        (SentTime, ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames, MessageId)
                        VALUES
                        (@SentTime, @ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @Attachments, @MessageId);
                        SELECT SCOPE_IDENTITY();", conn);

                    insertEmailCmd.Parameters.AddWithValue("@SentTime", message.Date.UtcDateTime);
                    insertEmailCmd.Parameters.AddWithValue("@ReceivedTime", DateTime.UtcNow);
                    insertEmailCmd.Parameters.AddWithValue("@FromEmail", message.From.ToString());
                    insertEmailCmd.Parameters.AddWithValue("@ToEmail", message.To.ToString());
                    insertEmailCmd.Parameters.AddWithValue("@Subject", message.Subject ?? "");
                    insertEmailCmd.Parameters.AddWithValue("@Body", message.TextBody ?? "");
                    insertEmailCmd.Parameters.AddWithValue("@Attachments",
                        string.Join(",", message.Attachments.Select(a => a.ContentDisposition?.FileName ?? "")));
                    insertEmailCmd.Parameters.AddWithValue("@MessageId", message.MessageId ?? "");

                    int emailId = Convert.ToInt32(insertEmailCmd.ExecuteScalar());

                    logger.Info($"Inserted Emails record ID {emailId} for MessageId {message.MessageId}");

                    var insertInboxCmd = new SqlCommand(@"
                        INSERT INTO InboxEmails
                        (ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames, MessageId)
                        VALUES
                        (@ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @Attachments, @MessageId)", conn);

                    insertInboxCmd.Parameters.AddWithValue("@ReceivedTime", DateTime.UtcNow);
                    insertInboxCmd.Parameters.AddWithValue("@FromEmail", message.From.ToString());
                    insertInboxCmd.Parameters.AddWithValue("@ToEmail", message.To.ToString());
                    insertInboxCmd.Parameters.AddWithValue("@Subject", message.Subject ?? "");
                    insertInboxCmd.Parameters.AddWithValue("@Body", message.TextBody ?? "");
                    insertInboxCmd.Parameters.AddWithValue("@Attachments",
                        string.Join(",", message.Attachments.Select(a => a.ContentDisposition?.FileName ?? "")));
                    insertInboxCmd.Parameters.AddWithValue("@MessageId", message.MessageId ?? "");

                    insertInboxCmd.ExecuteNonQuery();

                    logger.Info($"Inserted InboxEmails record for MessageId {message.MessageId}");

                    return true;
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                logger.Warn($"Duplicate key for MessageId {message.MessageId}. Skipping.");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error inserting MessageId {message.MessageId}");
                return false;
            }
        }
    }
}
