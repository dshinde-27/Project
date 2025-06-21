using CRM_Api.Helpers;
using CRM_Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CRM_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [HttpPost("Login")]
        public IActionResult Login([FromBody] Login model)
        {
            try
            {
                var reader = SqlHelper.ExecuteReader(@"
            SELECT u.Id, u.Username, u.Email, u.Password, u.RoleId, r.Name
            FROM Users u WITH (NOLOCK)
            JOIN Roles r WITH (NOLOCK) ON u.RoleId = r.Id
            WHERE u.Username = @Identifier OR u.Email = @Identifier",
                    new[] { new SqlParameter("@Identifier", model.UserName) });

                using (reader)
                {
                    if (reader.Read())
                    {
                        string storedPassword = reader["Password"].ToString();

                        if (model.Password == storedPassword)
                        {
                            Logger.Info("User '" + model.UserName + "' logged in successfully.");

                            var user = new
                            {
                                UserId = reader["Id"],
                                Username = reader["Username"].ToString(),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                Role = reader["Name"].ToString(),
                                Email = reader["Email"].ToString()
                            };

                            return Ok(new
                            {
                                Role = user.RoleId,
                                UserId = user.UserId,
                                Username = user.Username,
                                Email = user.Email
                            });
                        }
                        else
                        {
                            Logger.Warn("Invalid password attempt for user '" + model.UserName + "'.");
                        }
                    }
                }

                Logger.Warn("Login failed for identifier: '" + model.UserName + "'");
                return Unauthorized();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during login.");
                return StatusCode(500, "Internal server error");
            }
        }


    }
}
