using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using CRM_Api.Models;
using CRM_Api.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using MailKit.Search;
using MailKit.Net.Imap;

namespace CRM_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("send")]
        public IActionResult SendEmail([FromBody] Email request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.To) || !MailAddress.TryCreate(request.To, out _))
                    return BadRequest("Invalid recipient email address.");
                if (string.IsNullOrWhiteSpace(request.Subject))
                    return BadRequest("Email subject is required.");
                if (string.IsNullOrWhiteSpace(request.Body))
                    return BadRequest("Email body is required.");

                string smtpServer = _configuration["Smtp:SmtpServer"];
                int port = int.Parse(_configuration["Smtp:Port"]);
                string senderEmail = _configuration["Smtp:SenderEmail"];
                string senderPassword = _configuration["Smtp:SenderPassword"];
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                if (!MailAddress.TryCreate(senderEmail, out _))
                    return StatusCode(500, "Configured sender email is invalid.");

                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = port,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = request.Subject,
                    Body = request.Body,
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(request.To);

                string attachmentNames = null;
                if (request.Files != null && request.Files.Any())
                {
                    foreach (var file in request.Files)
                    {
                        mailMessage.Attachments.Add(new Attachment(file.OpenReadStream(), file.FileName));
                    }
                    attachmentNames = string.Join(", ", request.Files.Select(f => f.FileName));
                }

                smtpClient.Send(mailMessage);

                // Save to database via SqlHelper
                string insertQuery = @"
            INSERT INTO SentEmails (SentTime, FromEmail, ToEmail, Subject, Body, AttachmentNames)
            VALUES (@SentTime, @FromEmail, @ToEmail, @Subject, @Body, @AttachmentNames)";

                SqlHelper.ExecuteNonQueryWithParam(connectionString, insertQuery,
                    new SqlParameter("@SentTime", DateTime.Now),
                    new SqlParameter("@FromEmail", senderEmail),
                    new SqlParameter("@ToEmail", request.To),
                    new SqlParameter("@Subject", request.Subject),
                    new SqlParameter("@Body", request.Body),
                    new SqlParameter("@AttachmentNames", (object?)attachmentNames ?? DBNull.Value)
                );

                return Ok("Email sent and logged successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to send email: {ex.Message}");
            }
        }

        [HttpGet("sent")]
        public IActionResult GetSentEmails()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection");

                string query = @"
                SELECT Id, SentTime, FromEmail, ToEmail, Subject, Body, AttachmentNames
                FROM SentEmails
                ORDER BY SentTime DESC"; 

                DataSet ds = SqlHelper.ExecuteDatasetCommand(connectionString, CommandType.Text, query);

                var emails = new List<SentEmail>();

                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        emails.Add(new SentEmail
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            SentTime = Convert.ToDateTime(row["SentTime"]),
                            FromEmail = row["FromEmail"].ToString(),
                            ToEmail = row["ToEmail"].ToString(),
                            Subject = row["Subject"].ToString(),
                            Body = row["Body"].ToString(),
                            AttachmentNames = row["AttachmentNames"]?.ToString()
                        });
                    }
                }

                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving sent emails: {ex.Message}");
            }
        }

        [HttpGet("inbox")]
        public IActionResult GetInboxEmails()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection");

                string query = @"
                    SELECT Id, ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames
                    FROM InboxEmails
                    ORDER BY ReceivedTime DESC";

                DataSet ds = SqlHelper.ExecuteDatasetCommand(connectionString, CommandType.Text, query);

                var emails = new List<SentEmail>(); 

                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        emails.Add(new SentEmail
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            SentTime = Convert.ToDateTime(row["ReceivedTime"]), 
                            FromEmail = row["FromEmail"].ToString(),
                            ToEmail = row["ToEmail"].ToString(),
                            Subject = row["Subject"].ToString(),
                            Body = row["Body"].ToString(),
                            AttachmentNames = row["AttachmentNames"]?.ToString()
                        });
                    }
                }

                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving inbox emails: {ex.Message}");
            }
        }

        private void SyncInboxFromMailServer()
        {
            string senderEmail = _configuration["Smtp:SenderEmail"];
            string senderPassword = _configuration["Smtp:SenderPassword"];
            string connectionString = _configuration.GetConnectionString("Connection");

            using var client = new ImapClient();
            client.Connect("imap.gmail.com", 993, true);
            client.Authenticate(senderEmail, senderPassword);

            var inbox = client.Inbox;
            inbox.Open(MailKit.FolderAccess.ReadOnly);

            foreach (var uid in inbox.Search(SearchQuery.All))
            {
                var message = inbox.GetMessage(uid);

                // Skip duplicates using MessageId
                int existingCount = 0;
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM InboxEmails WHERE MessageId = @MessageId", conn))
                {
                    cmd.Parameters.AddWithValue("@MessageId", message.MessageId ?? Guid.NewGuid().ToString()); // fallback
                    conn.Open();
                    existingCount = (int)cmd.ExecuteScalar();
                }

                if (existingCount > 0)
                    continue;

                // Insert email
                SqlHelper.ExecuteNonQueryWithParam(
                    connectionString,
                    @"INSERT INTO InboxEmails (MessageId, ReceivedTime, FromEmail, ToEmail, Subject, Body)
              VALUES (@MessageId, @ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body)",
                    new SqlParameter("@MessageId", message.MessageId ?? Guid.NewGuid().ToString()),
                    new SqlParameter("@ReceivedTime", message.Date.DateTime),
                    new SqlParameter("@FromEmail", message.From.ToString()),
                    new SqlParameter("@ToEmail", message.To.ToString()),
                    new SqlParameter("@Subject", message.Subject),
                    new SqlParameter("@Body", message.TextBody ?? "")
                );
            }

            client.Disconnect(true);
        }


        [HttpPost("sync-inbox")]
        public IActionResult SyncInbox()
        {

            try
            {
                SyncInboxFromMailServer();
                return Ok("Inbox synced.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to sync inbox: {ex.Message}");
            }
        }
        [HttpGet("{id}")]
        public IActionResult GetEmailById(int id)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection");

                string query = @"
                    SELECT Id, ReceivedTime AS Time, FromEmail, ToEmail, Subject, Body, AttachmentNames
                    FROM InboxEmails
                    WHERE Id = @Id
                    UNION
                    SELECT Id, SentTime AS Time, FromEmail, ToEmail, Subject, Body, AttachmentNames
                    FROM SentEmails
                    WHERE Id = @Id";

                DataSet ds = SqlHelper.ExecuteDatasetCommand(connectionString, CommandType.Text, query,
                    new SqlParameter("@Id", id));

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return NotFound("Email not found.");
                }

                var row = ds.Tables[0].Rows[0];
                var email = new SentEmail
                {
                    Id = Convert.ToInt32(row["Id"]),
                    SentTime = Convert.ToDateTime(row["Time"]),
                    FromEmail = row["FromEmail"].ToString(),
                    ToEmail = row["ToEmail"].ToString(),
                    Subject = row["Subject"].ToString(),
                    Body = row["Body"].ToString(),
                    AttachmentNames = row["AttachmentNames"]?.ToString()
                };

                return Ok(email);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving email: {ex.Message}");
            }
        }

    }
}
