using CRM_Api.Helpers;
using CRM_Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRM_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PageController : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        [HttpGet("GetPages")]
        public IActionResult GetPages()
        {
            var pages = new List<Page>();

            try
            {
                string query = "SELECT Id, PageName, SubPageName, Status, Description FROM Pages";
                DataSet ds = SqlHelper.ExecuteDataSet(query);

                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        pages.Add(new Page
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            PageName = row["PageName"].ToString(),
                            SubPageName = row["SubPageName"].ToString(),
                            Status = row["Status"].ToString(),
                            Description = row["Description"].ToString()
                        });
                    }
                }

                Logger.Info("Fetched page list successfully. Total pages: '" + pages.Count + "'", pages.Count);
                return Ok(pages);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error fetching pages.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("AddPages")]
        public IActionResult AddPage([FromBody] Page page)
        {
            if (page == null)
            {
                Logger.Warn("Attempted to add null page data.");
                return BadRequest("Page data is required");
            }

            try
            {
                SqlHelper.ExecuteNonQuery(
                    @"INSERT INTO Pages (PageName, SubPageName, Status, Description) 
                      VALUES (@PageName, @SubPageName, @Status, @Description)",
                    new[]
                    {
                        new SqlParameter("@PageName", page.PageName),
                        new SqlParameter("@SubPageName", page.SubPageName),
                        new SqlParameter("@Status", page.Status),
                        new SqlParameter("@Description", page.Description)
                    });

                Logger.Info("Page '" + page.PageName + "' added successfully.", page.PageName);
                return Ok(new { message = "Page added successfully" });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while adding page '" + page.PageName + "'.", page.PageName);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
