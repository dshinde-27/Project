using CRM_Api.Helpers;
using CRM_Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace CRM_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet("GetRoles")]
        public IActionResult GetRoles()
        {
            var roles = new List<Role>();
            try
            {
                string query = "SELECT Id, Name, Status FROM Roles";
                var ds = SqlHelper.ExecuteDataSet(query);

                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        roles.Add(new Role
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Name = row["Name"].ToString(),
                            Status = row["Status"].ToString()
                        });
                    }
                }

                Logger.Info("Fetched role list successfully. Total roles: '" + roles.Count + "'", roles.Count);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error fetching roles.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("AddRoles")]
        public IActionResult AddRole([FromBody] Role role)
        {
            try
            {
                SqlHelper.ExecuteNonQuery(
                    "INSERT INTO Roles (Name, Status) VALUES (@Name, @Status)",
                    new[]
                    {
                        new SqlParameter("@Name", role.Name),
                        new SqlParameter("@Status", role.Status)
                    });

                Logger.Info("Role ''" + role.Name + "'' added successfully.", role.Name);
                return Ok("Role added successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error adding role ''" + role.Name + "''", role.Name);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("EditRole/{id}")]
        public IActionResult EditRole(int id, [FromBody] Role role)
        {
            if (role == null || role.Id != id)
            {
                Logger.Warn("Invalid role data for update. ID mismatch.");
                return BadRequest("Invalid role data.");
            }

            try
            {
                var exists = Convert.ToInt32(SqlHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM Roles WHERE Id = @Id",
                    new[] { new SqlParameter("@Id", id) }));

                if (exists == 0)
                {
                    Logger.Warn("Role with Id '" + id + "' not found for update.", id);
                    return NotFound(new { message = "Role not found." });
                }

                SqlHelper.ExecuteNonQuery(
                    @"UPDATE Roles 
                      SET Name = @Name, 
                          Status = @Status 
                      WHERE Id = @Id",
                    new[]
                    {
                        new SqlParameter("@Name", role.Name),
                        new SqlParameter("@Status", role.Status),
                        new SqlParameter("@Id", id)
                    });

                Logger.Info("Role with Id '" + id + "' updated successfully.", id);
                return Ok(new { message = "Role updated successfully." });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating role with Id '" + id + "'.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteRole(int id)
        {
            try
            {
                int rowsAffected = SqlHelper.ExecuteNonQuery(
                    "DELETE FROM Roles WHERE Id = @Id",
                    new[] { new SqlParameter("@Id", id) });

                if (rowsAffected == 0)
                {
                    Logger.Warn("Role with Id '" + id + "' not found for deletion.", id);
                    return NotFound();
                }

                Logger.Info("Role with Id '" + id + "' deleted successfully.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error deleting role with Id '" + id + "'.", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}