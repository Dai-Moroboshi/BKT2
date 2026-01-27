using System.ComponentModel.DataAnnotations;

namespace PCM.API.DTOs;

// ============ MEMBER DTOs ============

public class MemberDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public double RankLevel { get; set; }
    public bool IsActive { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class MemberDetailDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public double RankLevel { get; set; }
    public bool IsActive { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public decimal TotalSpent { get; set; }
    
    // Statistics
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public List<RankHistoryDto> RankHistory { get; set; } = new();
    public List<MatchSummaryDto> RecentMatches { get; set; } = new();
}

public class RankHistoryDto
{
    public DateTime Date { get; set; }
    public double RankLevel { get; set; }
}

public class MatchSummaryDto
{
    public int MatchId { get; set; }
    public DateTime Date { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public bool IsWin { get; set; }
    public string? TournamentName { get; set; }
}

public class UpdateMemberDto
{
    [MaxLength(100)]
    public string? FullName { get; set; }
    
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}
