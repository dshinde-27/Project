using System.Text.Json.Serialization;

namespace CRM_Api.Models
{
    public class Permission
    {
        public int RoleId { get; set; }
        public int PageId { get; set; }
        public bool HasPermission { get; set; }

        [JsonIgnore]
        public string? PageName { get; set; }

        [JsonIgnore]
        public string? SubPageName { get; set; }
    }
}
