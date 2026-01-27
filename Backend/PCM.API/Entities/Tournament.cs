using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_Tournaments")]
public class Tournament
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Thể thức
    public TournamentFormat Format { get; set; } = TournamentFormat.Knockout;

    // Phí tham gia
    [Column(TypeName = "decimal(18,2)")]
    public decimal EntryFee { get; set; } = 0;

    // Tổng giải thưởng
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrizePool { get; set; } = 0;

    // Trạng thái
    public TournamentStatus Status { get; set; } = TournamentStatus.Open;

    // Cấu hình nâng cao (JSON)
    public string? Settings { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
