using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_Notifications")]
public class Notification
{
    [Key]
    public int Id { get; set; }

    // FK to Member (người nhận)
    public int ReceiverId { get; set; }

    [ForeignKey("ReceiverId")]
    public virtual Member? Receiver { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Info;

    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
