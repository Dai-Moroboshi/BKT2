using System.ComponentModel.DataAnnotations;

namespace PCM.API.DTOs;

// ============ MATCH DTOs ============

public class MatchDto
{
    public int Id { get; set; }
    public int? TournamentId { get; set; }
    public string? TournamentName { get; set; }
    public string? RoundName { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    
    // Team 1
    public int? Team1_Player1Id { get; set; }
    public string? Team1_Player1Name { get; set; }
    public int? Team1_Player2Id { get; set; }
    public string? Team1_Player2Name { get; set; }
    
    // Team 2
    public int? Team2_Player1Id { get; set; }
    public string? Team2_Player1Name { get; set; }
    public int? Team2_Player2Id { get; set; }
    public string? Team2_Player2Name { get; set; }
    
    // Result
    public int Score1 { get; set; }
    public int Score2 { get; set; }
    public string? Details { get; set; }
    public string WinningSide { get; set; } = "None";
    public bool IsRanked { get; set; }
    public string Status { get; set; } = string.Empty;
    
    public int? CourtId { get; set; }
    public string? CourtName { get; set; }
}

public class UpdateMatchResultDto
{
    [Required]
    public int Score1 { get; set; }
    
    [Required]
    public int Score2 { get; set; }
    
    public string? Details { get; set; } // VD: "11-9, 5-11, 11-8"
    
    [Required]
    public string WinningSide { get; set; } = string.Empty; // "Team1" or "Team2"
}

public class CreateMatchDto
{
    public int? TournamentId { get; set; }
    public string? RoundName { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    public int? Team1_Player1Id { get; set; }
    public int? Team1_Player2Id { get; set; }
    public int? Team2_Player1Id { get; set; }
    public int? Team2_Player2Id { get; set; }
    
    public bool IsRanked { get; set; } = true;
    public int? CourtId { get; set; }
}

// For Bracket visualization
public class BracketDto
{
    public string RoundName { get; set; } = string.Empty;
    public List<BracketMatchDto> Matches { get; set; } = new();
}

public class BracketMatchDto
{
    public int MatchId { get; set; }
    public int MatchNumber { get; set; }
    public string Team1Name { get; set; } = string.Empty;
    public string Team2Name { get; set; } = string.Empty;
    public int? Score1 { get; set; }
    public int? Score2 { get; set; }
    public string? Winner { get; set; }
    public int? NextMatchId { get; set; }
}
