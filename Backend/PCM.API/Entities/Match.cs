using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_Matches")]
public class Match
{
    [Key]
    public int Id { get; set; }

    // FK to Tournament (nullable cho trận giao hữu)
    public int? TournamentId { get; set; }

    [ForeignKey("TournamentId")]
    public virtual Tournament? Tournament { get; set; }

    // Tên vòng đấu (VD: "Group A", "Quarter Final")
    [MaxLength(50)]
    public string? RoundName { get; set; }

    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }

    // Team 1
    public int? Team1_Player1Id { get; set; }
    public int? Team1_Player2Id { get; set; }

    // Team 2
    public int? Team2_Player1Id { get; set; }
    public int? Team2_Player2Id { get; set; }

    // Navigation for players
    [ForeignKey("Team1_Player1Id")]
    public virtual Member? Team1_Player1 { get; set; }

    [ForeignKey("Team1_Player2Id")]
    public virtual Member? Team1_Player2 { get; set; }

    [ForeignKey("Team2_Player1Id")]
    public virtual Member? Team2_Player1 { get; set; }

    [ForeignKey("Team2_Player2Id")]
    public virtual Member? Team2_Player2 { get; set; }

    // Kết quả
    public int Score1 { get; set; } = 0;
    public int Score2 { get; set; } = 0;

    // Chi tiết các set (JSON/String: VD: "11-9, 5-11, 11-8")
    [MaxLength(200)]
    public string? Details { get; set; }

    // Bên thắng
    public WinningSide WinningSide { get; set; } = WinningSide.None;

    // Có tính điểm DUPR không
    public bool IsRanked { get; set; } = true;

    // Trạng thái
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    // FK to Court (nếu có)
    public int? CourtId { get; set; }

    [ForeignKey("CourtId")]
    public virtual Court? Court { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
