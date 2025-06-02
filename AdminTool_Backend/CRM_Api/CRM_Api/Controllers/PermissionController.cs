using CRM_Api.Helpers;
using CRM_Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Security;

namespace CRM_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : Controller
    {
        private readonly IConfiguration _configuration;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public PermissionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("AddOrUpdatePermissions")]
        public IActionResult AddOrUpdatePermissions([FromBody] List<Permission> permissions)
        {
            try
            {
                if (permissions == null || permissions.Count == 0)
                {
                    Logger.Warn("AddOrUpdatePermissions failed: No permissions received.");
                    return BadRequest(new { message = "Invalid request. No permissions received." });
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Connection")))
                {
                    conn.Open();

                    foreach (var permission in permissions)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            MERGE INTO Permissions AS target
                            USING (SELECT @RoleId AS RoleId, @PageId AS PageId) AS source
                            ON target.RoleId = source.RoleId AND target.PageId = source.PageId
                            WHEN MATCHED THEN
                                UPDATE SET HasPermission = @HasPermission
                            WHEN NOT MATCHED THEN
                                INSERT (RoleId, PageId, HasPermission)
                                VALUES (@RoleId, @PageId, @HasPermission);", conn))
                        {
                            cmd.Parameters.AddWithValue("@RoleId", permission.RoleId);
                            cmd.Parameters.AddWithValue("@PageId", permission.PageId);
                            cmd.Parameters.AddWithValue("@HasPermission", permission.HasPermission);

                            cmd.ExecuteNonQuery();

                            Logger.Info("Permission updated or inserted for RoleId: " + permission.RoleId + ", PageId: " + permission.PageId);
                        }
                    }
                }

                return Ok(new { message = "Permissions updated successfully!" });
            }
            catch (Exception ex)
            {
                Logger.Error("Error in AddOrUpdatePermissions: " + ex.Message);
                return StatusCode(500, new { message = "Error updating permissions.", error = ex.Message });
            }
        }

        [HttpGet("GetPermissions")]
        public IActionResult GetPermissions()
        {
            try
            {
                var permissions = new List<Permission>();
                string query = "SELECT p.Id AS PageId, p.PageName, p.SubPageName FROM Pages p";

                DataSet ds = SqlHelper.ExecuteDataSet(query);

                if (ds?.Tables.Count > 0)
                {
                    DataTable dt = ds.Tables[0];
                    foreach (DataRow row in dt.Rows)
                    {
                        permissions.Add(new Permission
                        {
                            PageId = Convert.ToInt32(row["PageId"]),
                            PageName = row["PageName"].ToString(),
                            SubPageName = row["SubPageName"] != DBNull.Value ? row["SubPageName"].ToString() : null
                        });
                    }
                }

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetPermissions: " + ex.Message);
                return StatusCode(500, new { message = "Error fetching permissions.", error = ex.Message });
            }
        }

        [HttpGet("GetMenu/{roleId}")]
        public IActionResult GetMenu(int roleId)
        {
            try
            {
                var menu = new Dictionary<string, List<string>>();
                string query = @"
                    SELECT pg.PageName, pg.SubPageName
                    FROM Permissions pr
                    INNER JOIN Pages pg ON pr.PageId = pg.Id
                    WHERE pr.RoleId = @RoleId AND pr.HasPermission = 1";

                var ds = SqlHelper.ExecuteDataSetWithParams(query, new SqlParameter[] {
                    new SqlParameter("@RoleId", roleId)
                });

                if (ds?.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        string page = row["PageName"].ToString();
                        string subPage = row["SubPageName"] != DBNull.Value ? row["SubPageName"].ToString() : null;

                        if (!menu.ContainsKey(page))
                            menu[page] = new List<string>();

                        if (!string.IsNullOrEmpty(subPage))
                            menu[page].Add(subPage);
                    }
                }

                return Ok(menu);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetMenu: " + ex.Message);
                return StatusCode(500, new { message = "Error fetching menu.", error = ex.Message });
            }
        }

        [HttpGet("GetPermissionsByRole/{roleId}")]
        public IActionResult GetPermissionsByRole(int roleId)
        {
            try
            {
                var permissions = new List<Permission>();
                string query = @"
                    SELECT pr.RoleId, pr.PageId, pr.HasPermission, pg.PageName, pg.SubPageName
                    FROM Permissions pr
                    INNER JOIN Pages pg ON pr.PageId = pg.Id
                    WHERE pr.RoleId = @RoleId";

                var ds = SqlHelper.ExecuteDataSetWithParams(query, new SqlParameter[] {
                    new SqlParameter("@RoleId", roleId)
                });

                if (ds?.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        permissions.Add(new Permission
                        {
                            RoleId = Convert.ToInt32(row["RoleId"]),
                            PageId = Convert.ToInt32(row["PageId"]),
                            HasPermission = Convert.ToBoolean(row["HasPermission"]),
                            PageName = row["PageName"].ToString(),
                            SubPageName = row["SubPageName"] != DBNull.Value ? row["SubPageName"].ToString() : null
                        });
                    }
                }

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetPermissionsByRole: " + ex.Message);
                return StatusCode(500, new { message = "Error fetching permissions.", error = ex.Message });
            }
        }
    }
}
