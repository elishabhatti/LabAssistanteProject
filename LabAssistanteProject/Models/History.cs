using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabAssistanteProject.Models
{
    // History Model
    public class History
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public virtual Requests? Request { get; set; }

        [Required]
        public string OldStatus { get; set; } = string.Empty;

        [Required]
        public string NewStatus { get; set; } = string.Empty;

        [Required]
        public int UpdatedBy { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual Users? UpdatedByUser { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
