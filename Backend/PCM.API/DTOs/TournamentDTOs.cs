using System.ComponentModel.DataAnnotations;

namespace PCM.API.DTOs;

// ============ TOURNAMENT DTOs ============

public class TournamentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public decimal PrizePool { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ParticipantCount { get; set; }
    public bool IsJoined { get; set; }
}

public class TournamentDetailDto : TournamentDto
{
    public string? Settings { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
    public List<MatchDto> Matches { get; set; } = new();
    public List<StandingDto> Standings { get; set; } = new();
}

public class CreateTournamentDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public string Format { get; set; } = "Knockout";

    [Range(0, 10000000)]
    public decimal EntryFee { get; set; }

    [Range(0, 100000000)]
    public decimal PrizePool { get; set; }

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Settings { get; set; }
}

public class ParticipantDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public bool PaymentStatus { get; set; }
    public int? Seed { get; set; }
    public string? GroupName { get; set; }
    public double MemberRank { get; set; }
}

public class StandingDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public int Rank { get; set; }
}

public class JoinTournamentDto
{
    public string? TeamName { get; set; }
}
