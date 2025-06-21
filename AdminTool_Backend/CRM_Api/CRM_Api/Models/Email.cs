using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CRM_Api.Models
{
    public class Email
    {
        [Required]
        [EmailAddress]
        public string To { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; }

        // Optional attachment list
        public List<IFormFile> Files { get; set; } = new();
    }

    public class SentEmail
    {
        public int Id { get; set; }
        public string FromEmail { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime? SentTime { get; set; }
        public DateTime? ReceivedTime { get; set; }
        public string? AttachmentNames { get; set; }
        public bool IsStarred { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRead { get; set; } // ← Add this
    }



}
