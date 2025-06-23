using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using static CRM_Api.Models.Chat;
using CRM_Api.Models;

namespace CRM_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ChatController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Users")]
        public IActionResult GetUsers([FromQuery] string currentUser)
        {
            List<UserDto> users = new List<UserDto>();

            string connectionString = _configuration.GetConnectionString("Connection");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, Username, Email FROM Users WHERE Username != @Username";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Username", currentUser);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new UserDto
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return Ok(users);
        }

        [HttpPost("SendMessage")]
        public IActionResult SendMessage([FromBody] ChatMessageDto msg)
        {
            string connectionString = _configuration.GetConnectionString("Connection");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string insertQuery = @"INSERT INTO ChatMessages 
                               (SenderUsername, ReceiverUsername, MessageText) 
                               VALUES (@Sender, @Receiver, @Text)";

                using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Sender", msg.SenderUsername);
                    cmd.Parameters.AddWithValue("@Receiver", msg.ReceiverUsername);
                    cmd.Parameters.AddWithValue("@Text", msg.MessageText);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Optional: Send Email
                string receiverEmail = GetEmailByUsername(msg.ReceiverUsername, con);
                if (!string.IsNullOrEmpty(receiverEmail))
                {
                    SendEmailNotification(receiverEmail, msg.SenderUsername, msg.MessageText);
                }
            }

            return Ok(new { status = "Message Sent" });
        }

        private string GetEmailByUsername(string username, SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand("SELECT Email FROM Users WHERE Username = @Username", con))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                object result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }

        private void SendEmailNotification(string toEmail, string fromUser, string messageText)
        {
            var smtpSection = _configuration.GetSection("Smtp");

            string smtpServer = smtpSection["SmtpServer"];
            int port = int.Parse(smtpSection["Port"]);
            string senderEmail = smtpSection["SenderEmail"];
            string senderPassword = smtpSection["SenderPassword"];

            using (var client = new System.Net.Mail.SmtpClient(smtpServer, port))
            {
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword);

                var mail = new System.Net.Mail.MailMessage();
                mail.To.Add(toEmail);
                mail.Subject = $"New message from {fromUser}";
                mail.From = new System.Net.Mail.MailAddress(senderEmail);
                mail.IsBodyHtml = true;

                // Get role of receiver
                string role = GetRoleByEmail(toEmail);

                string approveLink;
                if (role == "Admin")
                    approveLink = $"http://localhost:3000/chat?sender={fromUser}&receiver={toEmail}";
                else
                    approveLink = $"http://10.0.7.47:3001/chat?sender={fromUser}&receiver={toEmail}";

                mail.Body = $@"
            <html>
              <body>
                <p>You received a new message from <strong>{fromUser}</strong>:</p>
                <blockquote>{messageText}</blockquote>
                <p>
                  <a href='{approveLink}' style='padding: 10px 15px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Accept</a>
                </p>
              </body>
            </html>";

                try
                {
                    client.Send(mail);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Email send error: " + ex.Message);
                }
            }
        }

        private string GetRoleByEmail(string email)
        {
            string role = "User"; // Default role if not found
            string connectionString = _configuration.GetConnectionString("Connection");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                SELECT r.Name FROM Users u
                JOIN Roles r ON u.RoleId = r.Id
                WHERE u.Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        role = result.ToString();
                    }
                }
            }

            return role;
        }


        [HttpGet("ApproveMessage")]
        public IActionResult ApproveMessage([FromQuery] string sender, [FromQuery] string receiver)
        {
            string connectionString = _configuration.GetConnectionString("Connection");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string updateQuery = @"UPDATE ChatMessages
                               SET IsApproved = 1
                               WHERE SenderUsername = @Sender AND ReceiverUsername = @Receiver AND IsApproved = 0";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Sender", sender);
                    cmd.Parameters.AddWithValue("@Receiver", receiver);
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        return Content("Chat message approved successfully!");
                    }
                    else
                    {
                        return Content("No pending messages found or already approved.");
                    }
                }
            }
        }



      
    }
}
