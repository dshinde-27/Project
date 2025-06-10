using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using CRM_Api.Models;
using CRM_Api.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using MailKit.Search;
using MailKit.Net.Imap;
using MailKit;

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
                var messageId = message.MessageId ?? Guid.NewGuid().ToString();

                // Check for duplicates in InboxEmails
                int existingCount = 0;
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM InboxEmails WHERE MessageId = @MessageId", conn))
                {
                    cmd.Parameters.AddWithValue("@MessageId", messageId);
                    conn.Open();
                    existingCount = (int)cmd.ExecuteScalar();
                }

                if (existingCount > 0)
                    continue;

                // Insert into InboxEmails
                SqlHelper.ExecuteNonQueryWithParam(
                    connectionString,
                    @"INSERT INTO InboxEmails (MessageId, ReceivedTime, FromEmail, ToEmail, Subject, Body)
                    VALUES (@MessageId, @ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body)",
                    new SqlParameter("@MessageId", messageId),
                    new SqlParameter("@ReceivedTime", message.Date.DateTime),
                    new SqlParameter("@FromEmail", message.From.ToString()),
                    new SqlParameter("@ToEmail", message.To.ToString()),
                    new SqlParameter("@Subject", message.Subject),
                    new SqlParameter("@Body", message.TextBody ?? "")
                );

                // Insert into Emails table
                SqlHelper.ExecuteNonQueryWithParam(
                    connectionString,
                    @"INSERT INTO Emails (SentTime, ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames, IsDeleted, IsArchived, IsStarred, Label, Folder)
                    VALUES (@SentTime, @ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @AttachmentNames, 0, 0, 0, NULL, 'Inbox')",
                    new SqlParameter("@SentTime", DBNull.Value), 
                    new SqlParameter("@ReceivedTime", message.Date.DateTime),
                    new SqlParameter("@FromEmail", message.From.ToString()),
                    new SqlParameter("@ToEmail", message.To.ToString()),
                    new SqlParameter("@Subject", message.Subject),
                    new SqlParameter("@Body", message.TextBody ?? ""),
                    new SqlParameter("@AttachmentNames", string.Join(", ", message.Attachments.Select(a => a.ContentDisposition?.FileName ?? "Unnamed")))
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


        private IList<SentEmail> FetchEmailsFromFolder(string folderName)
        {
            var emailList = new List<SentEmail>();

            string senderEmail = _configuration["Smtp:SenderEmail"];
            string senderPassword = _configuration["Smtp:SenderPassword"];

            using var client = new ImapClient();
            client.Connect("imap.gmail.com", 993, true);
            client.Authenticate(senderEmail, senderPassword);

            // ✅ This line now works with full folder names like [Gmail]/Sent Mail
            var targetFolder = client.GetFolder(folderName);

            if (targetFolder == null)
                throw new Exception($"Folder '{folderName}' not found on the server.");

            targetFolder.Open(FolderAccess.ReadOnly);

            var uids = targetFolder.Search(SearchQuery.All);
            foreach (var uid in uids.Reverse().Take(20))
            {
                var message = targetFolder.GetMessage(uid);
                emailList.Add(new SentEmail
                {
                    SentTime = message.Date.DateTime,
                    FromEmail = message.From.ToString(),
                    ToEmail = message.To.ToString(),
                    Subject = message.Subject,
                    Body = message.TextBody ?? message.HtmlBody ?? "(No content)",
                    AttachmentNames = string.Join(", ", message.Attachments.Select(a => a.ContentDisposition?.FileName ?? a.ContentType.Name))
                });
            }

            client.Disconnect(true);
            return emailList;
        }

        [HttpGet("folder/{*folderName}")]
        public IActionResult GetEmailsFromFolder(string folderName)
        {
            try
            {
                // Decode the folder name from URL format to plain text
                string decodedFolderName = Uri.UnescapeDataString(folderName);

                // Example: decodedFolderName = "[Gmail]/Sent Mail"

                using var client = new ImapClient();
                var email = _configuration["Smtp:SenderEmail"];
                var password = _configuration["Smtp:SenderPassword"];

                client.Connect("imap.gmail.com", 993, true);
                client.Authenticate(email, password);

                var folder = client.GetFolder(decodedFolderName);
                folder.Open(FolderAccess.ReadOnly);

                var uids = folder.Search(SearchQuery.All);
                var emails = new List<object>();

                foreach (var uid in uids.Reverse().Take(10)) // get last 10 for brevity
                {
                    var message = folder.GetMessage(uid);
                    emails.Add(new
                    {
                        From = message.From.ToString(),
                        To = message.To.ToString(),
                        Subject = message.Subject,
                        Body = message.TextBody,
                        Date = message.Date.DateTime
                    });
                }

                client.Disconnect(true);
                return Ok(emails);
            }
            catch (FolderNotFoundException)
            {
                return NotFound($"Folder '{folderName}' not found on the server.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving emails from '{folderName}': {ex.Message}");
            }
        }

        [HttpGet("folders")]
        public IActionResult GetAvailableFolders()
        {
            try
            {
                string senderEmail = _configuration["Smtp:SenderEmail"];
                string senderPassword = _configuration["Smtp:SenderPassword"];

                using var client = new ImapClient();
                client.Connect("imap.gmail.com", 993, true);
                client.Authenticate(senderEmail, senderPassword);

                var folders = client.GetFolders(client.PersonalNamespaces[0])
                    .Select(f => f.FullName)
                    .ToList();

                client.Disconnect(true);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve folders: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteEmail(int id)
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET IsDeleted = 1 WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query, new SqlParameter("@Id", id));
                return Ok("Email marked as deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting email: {ex.Message}");
            }
        }

        [HttpPost("archive/{id}")]
        public IActionResult ArchiveEmail(int id)
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET IsArchived = 1 WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query, new SqlParameter("@Id", id));
                return Ok("Email archived.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error archiving email: {ex.Message}");
            }
        }

        [HttpPost("star/{id}")]
        public IActionResult StarEmail(int id)
        {
            return ToggleBooleanFlag(id, "IsStarred", true, "Email starred.");
        }

        [HttpPost("unstar/{id}")]
        public IActionResult UnstarEmail(int id)
        {
            return ToggleBooleanFlag(id, "IsStarred", false, "Email unstarred.");
        }

        private IActionResult ToggleBooleanFlag(int id, string column, bool value, string message)
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = $"UPDATE Emails SET {column} = @value WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query,
                    new SqlParameter("@value", value),
                    new SqlParameter("@Id", id));

                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating {column}: {ex.Message}");
            }
        }

        [HttpPost("move/{id}")]
        public IActionResult MoveToFolder(int id, [FromQuery] string folder)
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET Folder = @Folder WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query,
                    new SqlParameter("@Folder", folder),
                    new SqlParameter("@Id", id));

                return Ok($"Email moved to folder '{folder}'.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error moving email: {ex.Message}");
            }
        }

        [HttpPost("label/{id}")]
        public IActionResult LabelEmail(int id, [FromQuery] string label)
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET Label = @Label WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query,
                    new SqlParameter("@Label", label),
                    new SqlParameter("@Id", id));

                return Ok($"Label '{label}' assigned to email.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error labeling email: {ex.Message}");
            }
        }

        [HttpGet("archived")]
        public IActionResult GetArchivedEmails()
        {
            string conn = _configuration.GetConnectionString("Connection");
            string query = "SELECT * FROM Emails WHERE IsArchived = 1 AND IsDeleted = 0";

            var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query);
            return Ok(ds.Tables[0]);
        }

        [HttpGet("starred")]
        public IActionResult GetStarredEmails()
        {
            string conn = _configuration.GetConnectionString("Connection");
            string query = "SELECT * FROM Emails WHERE IsStarred = 1 AND IsDeleted = 0";

            var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query);
            return Ok(ds.Tables[0]);
        }

        [HttpGet("label/{label}")]
        public IActionResult GetLabeledEmails(string label)
        {
            string conn = _configuration.GetConnectionString("Connection");
            string query = "SELECT * FROM Emails WHERE Label = @Label AND IsDeleted = 0";

            var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query,
                new SqlParameter("@Label", label));
            return Ok(ds.Tables[0]);
        }


    }
}

