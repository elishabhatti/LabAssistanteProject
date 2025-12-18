namespace LabAssistanteProject.Models
{
    public class Users
    {
        
        // Primary Key
        public int Id { get; set; }

        // Username
        public string? Username { get; set; }

        // Password Hash
        public string? Password { get; set; }

        // Full Name
        public string? FullName { get; set; }

        // Email
        public string? Email { get; set; }

        // Role: enduser, facility_head, assignee, admin
        public string? Role { get; set; }
    }
}
