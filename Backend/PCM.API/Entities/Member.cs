using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PCM.API.Entities;

[Table("272_Members")]
public class Member
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime JoinDate { get; set; } = DateTime.UtcNow;

    // Rank DUPR (Dynamic Universal Pickleball Rating)
    public double RankLevel { get; set; } = 3.0;

    public bool IsActive { get; set; } = true;

    // FK to Identity User
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual IdentityUser? User { get; set; }

    // Wallet
    [Column(TypeName = "decimal(18,2)")]
    public decimal WalletBalance { get; set; } = 0;

    // Tier (Hạng thành viên)
    public MemberTier Tier { get; set; } = MemberTier.Standard;

    // Tổng tiền đã chi tiêu
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSpent { get; set; } = 0;

    // Avatar
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    // Navigation properties
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<TournamentParticipant> TournamentParticipations { get; set; } = new List<TournamentParticipant>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
