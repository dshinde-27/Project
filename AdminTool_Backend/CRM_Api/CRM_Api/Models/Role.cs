namespace CRM_Api.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; } // Now a boolean
    }

}
