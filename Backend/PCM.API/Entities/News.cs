using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_News")]
public class News
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsPinned { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }
}
