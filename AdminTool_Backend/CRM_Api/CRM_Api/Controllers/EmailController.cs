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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("send")]
        public IActionResult SendEmail([FromBody] Email request)
        {
            Logger.Info("Received email send request.");

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.To) || !MailAddress.TryCreate(request.To, out _))
                {
                    Logger.Warn("Invalid recipient email address.");
                    return BadRequest("Invalid recipient email address.");
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    Logger.Warn("Missing email subject.");
                    return BadRequest("Email subject is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Body))
                {
                    Logger.Warn("Missing email body.");
                    return BadRequest("Email body is required.");
                }

                string smtpServer = _configuration["Smtp:SmtpServer"];
                int port = int.Parse(_configuration["Smtp:Port"]);
                string senderEmail = _configuration["Smtp:SenderEmail"];
                string senderPassword = _configuration["Smtp:SenderPassword"];
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                Logger.Info("SMTP server: '"+ smtpServer +"', Port: '"+ port +"', Sender: '"+ senderEmail +"'");

                if (!MailAddress.TryCreate(senderEmail, out _))
                {
                    Logger.Error("Configured sender email is invalid.");
                    return StatusCode(500, "Configured sender email is invalid.");
                }

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
                    Logger.Info("Attachments: '"+ attachmentNames +"'");
                }

                smtpClient.Send(mailMessage);
                Logger.Info("Email successfully sent to '"+ request.To +"'");

                string insertQuery = "INSERT INTO SentEmails (SentTime, FromEmail, ToEmail, Subject, Body, AttachmentNames)VALUES (@SentTime, @FromEmail, @ToEmail, @Subject, @Body, @AttachmentNames)";

                SqlHelper.ExecuteNonQueryWithParam(connectionString, insertQuery,
                    new SqlParameter("@SentTime", DateTime.Now),
                    new SqlParameter("@FromEmail", senderEmail),
                    new SqlParameter("@ToEmail", request.To),
                    new SqlParameter("@Subject", request.Subject),
                    new SqlParameter("@Body", request.Body),
                    new SqlParameter("@AttachmentNames", (object?)attachmentNames ?? DBNull.Value)
                );

                Logger.Info("Email record inserted into SentEmails table.");
                return Ok("Email sent and logged successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to send email.");
                return StatusCode(500, "Failed to send email: '"+ ex.Message +"'");
            }
        }

        [HttpGet("sent")]
        public IActionResult GetSentEmails()
        {
            Logger.Info("Fetching sent emails.");

            try
            {
                string connectionString = _configuration.GetConnectionString("Connection");

                string query ="SELECT Id, SentTime, FromEmail, ToEmail, Subject, Body, AttachmentNames FROM SentEmails ORDER BY SentTime DESC";

                Logger.Info("Executing SQL query: '"+ query +"'");

                DataSet ds = SqlHelper.ExecuteDatasetCommand(connectionString, CommandType.Text, query);

                var emails = new List<SentEmail>();

                if (ds.Tables.Count > 0)
                {
                    Logger.Info("Rows fetched: " + ds.Tables[0].Rows.Count);

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
                else
                {
                    Logger.Info("No sent email records found.");
                }

                return Ok(emails);
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving sent emails: " + ex.Message);
                return StatusCode(500, "Error retrieving sent emails: " + ex.Message);
            }
        }

        [HttpGet("inbox")]
        public IActionResult GetInboxEmails()
        {
            Logger.Info("Fetching inbox emails.");

            try
            {
                string connectionString = _configuration.GetConnectionString("Connection");

                string query = "SELECT Id, ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames FROM Emails WHERE IsDeleted = 0 AND IsArchived = 0ORDER BY ReceivedTime DESC";

                Logger.Info("Executing SQL query:'"+ query +"'");

                DataSet ds = SqlHelper.ExecuteDatasetCommand(connectionString, CommandType.Text, query);

                var emails = new List<SentEmail>();

                if (ds.Tables.Count > 0)
                {
                    Logger.Info("Rows fetched: " + ds.Tables[0].Rows.Count);

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
                else
                {
                    Logger.Info("No inbox email records found.");
                }

                return Ok(emails);
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving inbox emails: " + ex.Message);
                return StatusCode(500, "Error retrieving inbox emails: " + ex.Message);
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetEmailById(int id)
        {
            Logger.Info("Fetching email with ID: " + id);

            try
            {
                string connectionString = _configuration.GetConnectionString("Connection");

                string query = "SELECT Id, ReceivedTime AS Time, FromEmail, ToEmail, Subject, Body, AttachmentNames FROM InboxEmails WHERE Id = @Id UNION SELECT Id, SentTime AS Time, FromEmail, ToEmail, Subject, Body, AttachmentNames FROM SentEmails WHERE Id = @Id";

                Logger.Info("Executing SQL query to retrieve email by ID.");

                DataSet ds = SqlHelper.ExecuteDatasetCommand(connectionString, CommandType.Text, query, new SqlParameter("@Id", id));

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Logger.Warn("No email found with ID: " + id);
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

                Logger.Info("Email fetched successfully with ID: " + id);
                return Ok(email);
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving email with ID: " + id + " - " + ex.Message);
                return StatusCode(500, "Error retrieving email: " + ex.Message);
            }
        }

        [HttpPost("star/{id}")]
        public IActionResult StarEmail(int id)
        {
            Logger.Info("Starring email with ID: " + id);
            return ToggleBooleanFlag(id, "IsStarred", true, "Email starred.");
        }

        [HttpPost("unstar/{id}")]
        public IActionResult UnstarEmail(int id)
        {
            Logger.Info("Unstarring email with ID: " + id);
            return ToggleBooleanFlag(id, "IsStarred", false, "Email unstarred.");
        }


        [HttpGet("starred")]
        public IActionResult GetStarredEmails()
        {
            Logger.Info("Fetching starred emails.");
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "SELECT Id, SentTime, FromEmail, ToEmail, Subject, Body, AttachmentNames, IsStarred FROM Emails WHERE IsStarred = 1 ORDER BY ReceivedTime DESC";

                Logger.Info("Executing query to get starred emails.");

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

                Logger.Info("Fetched " + emails.Count + " starred emails.");
                return Ok(emails);
            }
            catch (Exception ex)
            {
                Logger.Error("Error fetching starred emails: " + ex.Message);
                return StatusCode(500, "Error fetching starred emails: " + ex.Message);
            }
        }

        private IActionResult ToggleBooleanFlag(int id, string column, bool value, string message)
        {
            Logger.Info("Toggling column '" + column + "' to value '" + value + "' for email ID: " + id);
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET " + column + " = @value WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query,
                    new SqlParameter("@value", value),
                    new SqlParameter("@Id", id));

                Logger.Info("Successfully updated column '" + column + "' for email ID: " + id);
                return Ok(message);
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating column '" + column + "' for email ID: " + id + " - " + ex.Message);
                return StatusCode(500, "Error updating " + column + ": " + ex.Message);
            }
        }

        [HttpDelete("Delete/{id}")]
        public IActionResult DeleteEmail(int id)
        {
            Logger.Info("Marking email ID: " + id + " as deleted.");
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET IsDeleted = 1 WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query, new SqlParameter("@Id", id));

                Logger.Info("Email ID: " + id + " marked as deleted.");
                return Ok("Email marked as deleted.");
            }
            catch (Exception ex)
            {
                Logger.Error("Error deleting email ID: " + id + " - " + ex.Message);
                return StatusCode(500, "Error deleting email: " + ex.Message);
            }
        }

        [HttpGet("Deleted")]
        public IActionResult GetDeletedEmails()
        {
            string conn = _configuration.GetConnectionString("Connection");

            Logger.Info("Fetching deleted emails.");
            try
            {
                string query = "SELECT * FROM Emails WHERE IsDeleted = 1";
                DataTable dt = SqlHelper.ExecuteDataTable(query,conn, null);

                var emails = new List<SentEmail>();

                foreach (DataRow row in dt.Rows)
                {
                    emails.Add(new SentEmail
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        FromEmail = row["FromEmail"].ToString(),
                        ToEmail = row["ToEmail"].ToString(),
                        Subject = row["Subject"].ToString(),
                        Body = row["Body"].ToString(),
                        SentTime = row["SentTime"] != DBNull.Value ? Convert.ToDateTime(row["SentTime"]) : (DateTime?)null,
                        ReceivedTime = row["ReceivedTime"] != DBNull.Value ? Convert.ToDateTime(row["ReceivedTime"]) : (DateTime?)null,
                        IsStarred = Convert.ToBoolean(row["IsStarred"]),
                        IsDeleted = Convert.ToBoolean(row["IsDeleted"])
                    });
                }

                Logger.Info("Fetched " + emails.Count + " deleted emails.");
                return Ok(emails);
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving deleted emails: " + ex.Message);
                return StatusCode(500, "Error retrieving deleted emails: " + ex.Message);
            }
        }

        [HttpPost("archive/{id}")]
        public IActionResult ArchiveEmail(int id)
        {
            Logger.Info("Archiving email with ID: " + id);
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET IsArchived = 1 WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query, new SqlParameter("@Id", id));

                Logger.Info("Email with ID: " + id + " archived successfully.");
                return Ok("Email archived.");
            }
            catch (Exception ex)
            {
                Logger.Error("Error archiving email with ID: " + id + " - " + ex.Message);
                return StatusCode(500, "Error archiving email: " + ex.Message);
            }
        }

        [HttpGet("archived")]
        public IActionResult GetArchivedEmails()
        {
            Logger.Info("Fetching archived emails.");
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "SELECT * FROM Emails WHERE IsArchived = 1 AND IsDeleted = 0";

                var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query);
                DataTable dt = ds.Tables[0];

                var emails = new List<SentEmail>();

                foreach (DataRow row in dt.Rows)
                {
                    emails.Add(new SentEmail
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        FromEmail = row["FromEmail"].ToString(),
                        ToEmail = row["ToEmail"].ToString(),
                        Subject = row["Subject"].ToString(),
                        Body = row["Body"].ToString(),
                        SentTime = row["SentTime"] != DBNull.Value ? Convert.ToDateTime(row["SentTime"]) : (DateTime?)null,
                        ReceivedTime = row["ReceivedTime"] != DBNull.Value ? Convert.ToDateTime(row["ReceivedTime"]) : (DateTime?)null,
                        AttachmentNames = row["AttachmentNames"]?.ToString(),
                        IsStarred = Convert.ToBoolean(row["IsStarred"]),
                        IsDeleted = Convert.ToBoolean(row["IsDeleted"])
                    });
                }

                Logger.Info("Fetched " + emails.Count + " archived emails.");
                return Ok(emails);
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving archived emails: " + ex.Message);
                return StatusCode(500, "Error retrieving archived emails: " + ex.Message);
            }
        }

        [HttpPost("label/{id}")]
        public IActionResult LabelEmail(int id, [FromQuery] string label)
        {
            Logger.Info("Applying label '" + label + "' to email ID: " + id);
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET Label = @Label WHERE Id = @Id";

                SqlHelper.ExecuteNonQueryWithParam(conn, query,
                    new SqlParameter("@Label", label),
                    new SqlParameter("@Id", id));

                Logger.Info("Label '" + label + "' applied successfully to email ID: " + id);
                return Ok("Label '" + label + "' assigned to email.");
            }
            catch (Exception ex)
            {
                Logger.Error("Error labeling email ID: " + id + " - " + ex.Message);
                return StatusCode(500, "Error labeling email: " + ex.Message);
            }
        }

        [HttpGet("label/{label}")]
        public IActionResult GetLabeledEmails(string label)
        {
            Logger.Info("Fetching emails with label: '" + label + "'");
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "SELECT * FROM Emails WHERE Label = @Label AND IsDeleted = 0";

                var ds = SqlHelper.ExecuteDatasetCommand(conn, CommandType.Text, query,
                    new SqlParameter("@Label", label));

                Logger.Info("Retrieved " + ds.Tables[0].Rows.Count + " emails with label: '" + label + "'");
                return Ok(ds.Tables[0]);
            }
            catch (Exception ex)
            {
                Logger.Error("Error fetching labeled emails with label '" + label + "': " + ex.Message);
                return StatusCode(500, "Error retrieving labeled emails: " + ex.Message);
            }
        }

        [HttpPost("move/{id?}")]
        public IActionResult MoveToFolder(int? id, [FromQuery] string folder, [FromQuery] string rule)
        {
            Logger.Info($"Moving email(s) to folder: '{folder}' with rule: '{rule}', ID: {id}");

            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "";
                int rowsAffected = 0;

                if (id.HasValue)
                {
                    query = "UPDATE Emails SET Folder = @Folder WHERE Id = @Id";
                    rowsAffected = SqlHelper.ExecuteNonQueryWithParam(conn, query,
                        new SqlParameter("@Folder", folder),
                        new SqlParameter("@Id", id.Value));
                }
                else if (!string.IsNullOrWhiteSpace(rule))
                {
                    var trimmedRule = rule.Trim();
                    query = @"
                        UPDATE Emails
                        SET Folder = @Folder
                        WHERE 
                            (FromEmail LIKE @Rule) OR 
                            (ToEmail LIKE @Rule)";
                    rowsAffected = SqlHelper.ExecuteNonQueryWithParam(conn, query,
                        new SqlParameter("@Folder", folder),
                        new SqlParameter("@Rule", "%" + trimmedRule));
                }
                else
                {
                    return BadRequest("Either email ID or rule must be provided.");
                }

                Logger.Info($"{rowsAffected} email(s) successfully moved to folder: '{folder}'");
                return Ok($"{rowsAffected} email(s) moved to folder '{folder}'.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error moving emails to folder '{folder}': " + ex.Message);
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        [HttpGet("email-domains")]
        public IActionResult GetEmailDomains()
        {
            Logger.Info("Fetching unique email domains.");
            try
            {
                string conn = _configuration.GetConnectionString("Connection");

                string query = @"
                        SELECT DISTINCT 
                            LOWER(SUBSTRING(FromEmail, CHARINDEX('@', FromEmail) + 1, LEN(FromEmail))) AS Domain
                        FROM Emails
                        WHERE FromEmail LIKE '%@%.%'

                        UNION

                        SELECT DISTINCT 
                            LOWER(SUBSTRING(ToEmail, CHARINDEX('@', ToEmail) + 1, LEN(ToEmail))) AS Domain
                        FROM Emails
                        WHERE ToEmail LIKE '%@%.%'";

                DataTable dt = SqlHelper.ExecuteDataTable(query, conn ,null);

                var domains = dt.AsEnumerable()
                                .Select(row => row["Domain"].ToString())
                                .Where(domain => !string.IsNullOrWhiteSpace(domain))
                                .Distinct()
                                .OrderBy(d => d)
                                .ToList();

                Logger.Info("Fetched " + domains.Count + " unique email domains.");
                return Ok(domains);
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving email domains: " + ex.Message);
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        [HttpGet("folders")]
        public IActionResult GetFolders()
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "SELECT DISTINCT Folder FROM Emails WHERE Folder IS NOT NULL AND Folder <> ''";

                var dt = SqlHelper.ExecuteQueryDT(conn, query);
                var folders = dt.Rows.Cast<DataRow>()
                                     .Select(row => row["Folder"].ToString())
                                     .ToList();

                return Ok(folders);
            }
            catch (Exception ex)
            {
                Logger.Error("Error fetching folders: " + ex.Message);
                return StatusCode(500, "Failed to get folders");
            }
        }

        [HttpGet("folder/{folderName}")]
        public IActionResult GetEmailsByFolder(string folderName)
        {
            try
            {
                string conn = _configuration.GetConnectionString("Connection");

                string query = @"
                    SELECT Id, FromEmail, ToEmail, Subject, Body, Folder, ReceivedTime
                    FROM Emails
                    WHERE LOWER(Folder) = LOWER(@FolderName)
                    ORDER BY ReceivedTime DESC";

                DataTable dt = SqlHelper.ExecuteDataTable(query,conn, new SqlParameter[] {new SqlParameter("@FolderName", folderName)});


                var emails = dt.AsEnumerable().Select(row => new SentEmail
                {
                    Id = Convert.ToInt32(row["Id"]),
                    FromEmail = row["FromEmail"].ToString(),
                    ToEmail = row["ToEmail"].ToString(),
                    Subject = row["Subject"].ToString(),
                    Body = row["Body"].ToString(),
                    Folder = row["Folder"].ToString(),
                    ReceivedTime = Convert.ToDateTime(row["ReceivedTime"])
                }).ToList();

                if (emails == null || emails.Count == 0)
                {
                    return NotFound($"No emails found in folder: {folderName}");
                }

                return Ok(emails);
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving emails by folder: " + ex.Message);
                return StatusCode(500, "Error retrieving emails.");
            }
        }


        [HttpPost("markasread/{id}")]
        public IActionResult MarkAsRead(int id)
        {
            Logger.Info("Marking email with ID: " + id + " as read.");
            try
            {
                string conn = _configuration.GetConnectionString("Connection");
                string query = "UPDATE Emails SET IsRead = 1 WHERE Id = @Id";
                SqlParameter[] parameters = { new SqlParameter("@Id", id) };

                SqlHelper.ExecuteNonQueryWithParam(conn, query, parameters);

                Logger.Info("Email with ID: " + id + " marked as read.");
                return Ok("Email marked as read.");
            }
            catch (Exception ex)
            {
                Logger.Error("Error marking email ID: " + id + " as read: " + ex.Message);
                return StatusCode(500, "Error marking email as read: " + ex.Message);
            }
        }


    }
}

