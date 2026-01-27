using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_Courts")]
public class Court
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Giá thuê sân mỗi giờ
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerHour { get; set; } = 100000; // 100.000 VND

    // Navigation
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
