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
using Org.BouncyCastle.Asn1.X509;

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
                    FROM Emails where IsDeleted = 0
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

        [HttpGet("starred")]
        public IActionResult GetStarredEmails()
        {
            string conn = _configuration.GetConnectionString("Connection");
            string query = "SELECT Id, SentTime, FromEmail, ToEmail, Subject, Body, AttachmentNames, IsStarred FROM Emails WHERE IsStarred = 1 ORDER BY ReceivedTime DESC";

            var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query);
            var table = ds.Tables[0];

            var emails = table.AsEnumerable().Select(row => new SentEmail
            {
                Id = row.Field<int>("Id"),
                SentTime = row.Field<DateTime>("SentTime"),
                FromEmail = row.Field<string>("FromEmail"),
                ToEmail = row.Field<string>("ToEmail"),
                Subject = row.Field<string>("Subject"),
                Body = row.Field<string>("Body"),
                AttachmentNames = row.Field<string?>("AttachmentNames"),
                IsStarred = row.Field<bool>("IsStarred")
            }).ToList();

            return Ok(emails);
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

        [HttpDelete("Delete/{id}")]
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

        [HttpDelete("Delete")]
        public IActionResult AllDeleteEmail()
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "Select Emails SET IsDeleted = 1 ";

                SqlHelper.ExecuteNonQueryWithParam(conn, query);
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

        [HttpGet("archived")]
        public IActionResult GetArchivedEmails()
        {
            string conn = _configuration.GetConnectionString("Connection");
            string query = "SELECT * FROM Emails WHERE IsArchived = 1 AND IsDeleted = 0";

            var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query);
            return Ok(ds.Tables[0]);
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

        [HttpGet("label/{label}")]
        public IActionResult GetLabeledEmails(string label)
        {
            string conn = _configuration.GetConnectionString("Connection");
            string query = "SELECT * FROM Emails WHERE Label = @Label AND IsDeleted = 0";

            var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query,
                new SqlParameter("@Label", label));
            return Ok(ds.Tables[0]);
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

    }
}

