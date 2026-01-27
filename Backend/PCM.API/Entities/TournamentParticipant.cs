using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_TournamentParticipants")]
public class TournamentParticipant
{
    [Key]
    public int Id { get; set; }

    // FK to Tournament
    public int TournamentId { get; set; }

    [ForeignKey("TournamentId")]
    public virtual Tournament? Tournament { get; set; }

    // FK to Member
    public int MemberId { get; set; }

    [ForeignKey("MemberId")]
    public virtual Member? Member { get; set; }

    // Tên đội (nếu đánh đôi)
    [MaxLength(100)]
    public string? TeamName { get; set; }

    // Đã trừ Entry Fee chưa
    public bool PaymentStatus { get; set; } = false;

    // Hạt giống (Seed)
    public int? Seed { get; set; }

    // Nhóm/Bảng đấu
    [MaxLength(50)]
    public string? GroupName { get; set; }

    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
}
