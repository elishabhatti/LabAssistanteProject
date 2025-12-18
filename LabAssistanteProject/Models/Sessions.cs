namespace LabAssistanteProject.Models
{
    public class Sessions
    {
        public int id { get; set; }
        public int userId { get; set; }
        public string? token { get; set; }
        public bool? IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);

    }
}
