using System.ComponentModel.DataAnnotations;
namespace LabAssistanteProject.Models
{
public class Requests
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RequestorId { get; set; } 
    public int? AssigneeId { get; set; } 

    [Required]
    public int FacilityId { get; set; } 
    public virtual Users? Requestor { get; set; }

    public virtual Users? Assignee { get; set; } 

    public virtual Facilities? Facility { get; set; }

    [Required]
    public string? Severity { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    [Required]
    public string? Status { get; set; } = "unassigned";
    public string? Remarks { get; set; }
    public virtual ICollection<History>? StatusHistories { get; set; }
    }
}