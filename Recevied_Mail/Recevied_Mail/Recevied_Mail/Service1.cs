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
            OnStart(null);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            logger.Info("Service is starting...");

            try
            {
                LoadConfig();

                timer = new Timer(60000); // 1 minute interval
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

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                TimeSpan start = new TimeSpan(0, 0, 0);   // 8:00 AM
                TimeSpan end = new TimeSpan(23, 59, 59);    // 10:00 PM

                logger.Info($"Timer triggered at {DateTime.Now}");

                if (now >= start && now <= end)
                {
                    logger.Info("Within allowed time window. Fetching emails...");
                    FetchAndStoreEmails();
                }
                else
                {
                    logger.Info("Outside allowed time window. Skipping email fetch.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in Timer_Elapsed");
            }
        }

        private void LoadConfig()
        {
            try
            {
                connectionString = ConfigurationManager.ConnectionStrings["MarketaDb"]?.ConnectionString
                                   ?? throw new InvalidOperationException("Missing MarketaDb connection string.");

                imapHost = ConfigurationManager.AppSettings["ImapHost"]
                           ?? throw new InvalidOperationException("ImapHost is missing in appSettings.");

                if (!int.TryParse(ConfigurationManager.AppSettings["ImapPort"], out imapPort))
                    throw new InvalidOperationException("Invalid ImapPort in appSettings.");

                emailAddress = ConfigurationManager.AppSettings["EmailAddress"]
                               ?? throw new InvalidOperationException("EmailAddress is missing.");

                emailPassword = ConfigurationManager.AppSettings["EmailPassword"]
                                ?? throw new InvalidOperationException("EmailPassword is missing.");

                // Optional: Read SMTP settings (if sending emails)
                var smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
                var smtpPort = ConfigurationManager.AppSettings["SmtpPort"];
                var smtpSenderEmail = ConfigurationManager.AppSettings["SenderEmail"];
                var smtpSenderPassword = ConfigurationManager.AppSettings["SenderPassword"];

                logger.Info("Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading configuration");
                throw;
            }
        }
        private void FetchAndStoreEmails()
        {
            int successCount = 0;
            int failCount = 0;

            try
            {
                using (var client = new ImapClient())
                {
                    client.Connect(imapHost, imapPort, true);
                    client.Authenticate(emailAddress, emailPassword);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);

                    var summaries = inbox.Fetch(0, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);

                    foreach (var summary in summaries)
                    {
                        try
                        {
                            var fullMessage = inbox.GetMessage(summary.UniqueId);

                            InsertIntoEmails(fullMessage);
                            InsertIntoInboxEmails(fullMessage);

                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            logger.Error(ex, $"Error processing message UID {summary.UniqueId}");
                        }
                    }

                    client.Disconnect(true);
                }

                logger.Info($"Email processing completed. Success: {successCount}, Failed: {failCount}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Critical error in FetchAndStoreEmails");
            }
        }


        private void InsertIntoEmails(MimeMessage message)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string fromEmail = message.From.ToString();
                    string subject = message.Subject ?? "";
                    DateTime sentTime = message.Date.DateTime;

                    // Compute SHA256 hash for subject (same as your SQL SubjectHash)
                    byte[] subjectHashBytes = ComputeSha256Hash(subject);

                    // Check if this email already exists
                    var checkCmd = new SqlCommand(@"
                SELECT COUNT(1) FROM Emails
                WHERE FromEmail = @FromEmail AND SubjectHash = @SubjectHash AND SentTime = @SentTime", conn);

                    checkCmd.Parameters.AddWithValue("@FromEmail", fromEmail);
                    checkCmd.Parameters.Add("@SubjectHash", System.Data.SqlDbType.Binary, 32).Value = subjectHashBytes;
                    checkCmd.Parameters.AddWithValue("@SentTime", sentTime);

                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists == 0)
                    {
                        var insertCmd = new SqlCommand(@"
                    INSERT INTO Emails (SentTime, ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames)
                    VALUES (@SentTime, @ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @Attachments)", conn);

                        insertCmd.Parameters.AddWithValue("@SentTime", sentTime);
                        insertCmd.Parameters.AddWithValue("@ReceivedTime", DateTime.Now);
                        insertCmd.Parameters.AddWithValue("@FromEmail", fromEmail);
                        insertCmd.Parameters.AddWithValue("@ToEmail", message.To.ToString());
                        insertCmd.Parameters.AddWithValue("@Subject", subject);
                        insertCmd.Parameters.AddWithValue("@Body", message.TextBody ?? "");
                        insertCmd.Parameters.AddWithValue("@Attachments", string.Join(",", message.Attachments.Select(a => a.ContentDisposition?.FileName ?? "")));

                        insertCmd.ExecuteNonQuery();

                        logger.Info($"Inserted email from {fromEmail} (SentTime: {sentTime}) into Emails table.");
                    }
                    else
                    {
                        logger.Info($"Duplicate email detected for {fromEmail} (SentTime: {sentTime}). Skipping insert.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Unique constraint violation
                logger.Warn($"SQL duplicate key error on insert for {message.From}. Skipping insert.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "InsertIntoEmails error");
            }
        }


        private byte[] ComputeSha256Hash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            }
        }



        private void InsertIntoInboxEmails(MimeMessage message)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    logger.Info($"Connection string: {connectionString}");

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

                logger.Info($"Inserted email with MessageId {message.MessageId} into InboxEmails table.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "InsertIntoInboxEmails error");
            }
        }

        protected override void OnStop()
        {
            timer?.Stop();
            timer?.Dispose();
            logger.Info("Service stopped.");
        }
    }
}
