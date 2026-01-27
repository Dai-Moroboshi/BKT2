using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PCM.API.Data;
using PCM.API.DTOs;
using PCM.API.Entities;
using PCM.API.Hubs;

namespace PCM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TournamentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<PcmHub> _hubContext;

    public TournamentsController(ApplicationDbContext context, IHubContext<PcmHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TournamentDto>>>> GetTournaments([FromQuery] string? status)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var query = _context.Tournaments.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TournamentStatus>(status, out var statusEnum))
        {
            query = query.Where(t => t.Status == statusEnum);
        }

        var tournaments = await query
            .Include(t => t.Participants)
            .OrderByDescending(t => t.StartDate)
            .Select(t => new TournamentDto
            {
                Id = t.Id,
                Name = t.Name,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Format = t.Format.ToString(),
                EntryFee = t.EntryFee,
                PrizePool = t.PrizePool,
                Status = t.Status.ToString(),
                Description = t.Description,
                ImageUrl = t.ImageUrl,
                ParticipantCount = t.Participants.Count,
                IsJoined = t.Participants.Any(p => p.MemberId == memberId)
            })
            .ToListAsync();

        return Ok(ApiResponse<List<TournamentDto>>.Ok(tournaments));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TournamentDetailDto>>> GetTournament(int id)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var tournament = await _context.Tournaments
            .Include(t => t.Participants)
                .ThenInclude(p => p.Member)
            .Include(t => t.Matches)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null)
            return NotFound(ApiResponse<TournamentDetailDto>.Fail("Không tìm thấy giải đấu"));

        var result = new TournamentDetailDto
        {
            Id = tournament.Id,
            Name = tournament.Name,
            StartDate = tournament.StartDate,
            EndDate = tournament.EndDate,
            Format = tournament.Format.ToString(),
            EntryFee = tournament.EntryFee,
            PrizePool = tournament.PrizePool,
            Status = tournament.Status.ToString(),
            Description = tournament.Description,
            ImageUrl = tournament.ImageUrl,
            Settings = tournament.Settings,
            ParticipantCount = tournament.Participants.Count,
            IsJoined = tournament.Participants.Any(p => p.MemberId == memberId),
            Participants = tournament.Participants.Select(p => new ParticipantDto
            {
                Id = p.Id,
                MemberId = p.MemberId,
                MemberName = p.Member?.FullName ?? "",
                TeamName = p.TeamName,
                PaymentStatus = p.PaymentStatus,
                Seed = p.Seed,
                GroupName = p.GroupName,
                MemberRank = p.Member?.RankLevel ?? 0
            }).ToList(),
            Matches = tournament.Matches.Select(m => new MatchDto
            {
                Id = m.Id,
                TournamentId = m.TournamentId,
                RoundName = m.RoundName,
                Date = m.Date,
                StartTime = m.StartTime,
                Score1 = m.Score1,
                Score2 = m.Score2,
                WinningSide = m.WinningSide.ToString(),
                Status = m.Status.ToString()
            }).ToList()
        };

        return Ok(ApiResponse<TournamentDetailDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TournamentDto>>> CreateTournament([FromBody] CreateTournamentDto dto)
    {
        var tournament = new Tournament
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Format = Enum.Parse<TournamentFormat>(dto.Format),
            EntryFee = dto.EntryFee,
            PrizePool = dto.PrizePool,
            Status = TournamentStatus.Registering,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Settings = dto.Settings,
            CreatedDate = DateTime.UtcNow
        };

        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync();

        var result = new TournamentDto
        {
            Id = tournament.Id,
            Name = tournament.Name,
            StartDate = tournament.StartDate,
            EndDate = tournament.EndDate,
            Format = tournament.Format.ToString(),
            EntryFee = tournament.EntryFee,
            PrizePool = tournament.PrizePool,
            Status = tournament.Status.ToString(),
            Description = tournament.Description
        };

        return Ok(ApiResponse<TournamentDto>.Ok(result, "Tạo giải đấu thành công"));
    }

    [HttpPost("{id}/join")]
    public async Task<ActionResult<ApiResponse<ParticipantDto>>> JoinTournament(int id, [FromBody] JoinTournamentDto dto)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        var member = await _context.Members.FindAsync(memberId);

        if (member == null)
            return NotFound(ApiResponse<ParticipantDto>.Fail("Không tìm thấy thành viên"));

        var tournament = await _context.Tournaments
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null)
            return NotFound(ApiResponse<ParticipantDto>.Fail("Không tìm thấy giải đấu"));

        if (tournament.Status != TournamentStatus.Registering && tournament.Status != TournamentStatus.Open)
            return BadRequest(ApiResponse<ParticipantDto>.Fail("Giải đấu không còn mở đăng ký"));

        if (tournament.Participants.Any(p => p.MemberId == memberId))
            return BadRequest(ApiResponse<ParticipantDto>.Fail("Bạn đã đăng ký giải đấu này"));

        if (member.WalletBalance < tournament.EntryFee)
            return BadRequest(ApiResponse<ParticipantDto>.Fail($"Số dư ví không đủ. Cần {tournament.EntryFee:N0}đ"));

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create participant
            var participant = new TournamentParticipant
            {
                TournamentId = id,
                MemberId = memberId,
                TeamName = dto.TeamName,
                PaymentStatus = true,
                JoinedDate = DateTime.UtcNow
            };
            _context.TournamentParticipants.Add(participant);

            // Deduct entry fee
            if (tournament.EntryFee > 0)
            {
                var walletTransaction = new WalletTransaction
                {
                    MemberId = memberId,
                    Amount = -tournament.EntryFee,
                    Type = TransactionType.Payment,
                    Status = TransactionStatus.Completed,
                    Description = $"Phí tham gia giải {tournament.Name}",
                    RelatedId = tournament.Id.ToString(),
                    CreatedDate = DateTime.UtcNow
                };
                _context.WalletTransactions.Add(walletTransaction);

                member.WalletBalance -= tournament.EntryFee;
                member.TotalSpent += tournament.EntryFee;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = new ParticipantDto
            {
                Id = participant.Id,
                MemberId = participant.MemberId,
                MemberName = member.FullName,
                TeamName = participant.TeamName,
                PaymentStatus = participant.PaymentStatus,
                MemberRank = member.RankLevel
            };

            return Ok(ApiResponse<ParticipantDto>.Ok(result, "Đăng ký tham gia giải đấu thành công"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<ParticipantDto>.Fail($"Lỗi: {ex.Message}"));
        }
    }

    [HttpPost("{id}/generate-schedule")]
    [Authorize(Roles = "Admin,Referee")]
    public async Task<ActionResult<ApiResponse<List<MatchDto>>>> GenerateSchedule(int id)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Participants)
                .ThenInclude(p => p.Member)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null)
            return NotFound(ApiResponse<List<MatchDto>>.Fail("Không tìm thấy giải đấu"));

        if (tournament.Participants.Count < 2)
            return BadRequest(ApiResponse<List<MatchDto>>.Fail("Cần ít nhất 2 người tham gia"));

        // Clear existing matches
        var existingMatches = await _context.Matches.Where(m => m.TournamentId == id).ToListAsync();
        _context.Matches.RemoveRange(existingMatches);

        var matches = new List<Match>();
        var participants = tournament.Participants
            .OrderByDescending(p => p.Member?.RankLevel ?? 0)
            .ToList();

        if (tournament.Format == TournamentFormat.Knockout)
        {
            matches = GenerateKnockoutBracket(tournament, participants);
        }
        else if (tournament.Format == TournamentFormat.RoundRobin)
        {
            matches = GenerateRoundRobinSchedule(tournament, participants);
        }
        else // Hybrid
        {
            // First round robin in groups, then knockout
            matches = GenerateHybridSchedule(tournament, participants);
        }

        _context.Matches.AddRange(matches);
        tournament.Status = TournamentStatus.DrawCompleted;
        await _context.SaveChangesAsync();

        var result = matches.Select(m => new MatchDto
        {
            Id = m.Id,
            TournamentId = m.TournamentId,
            RoundName = m.RoundName,
            Date = m.Date,
            StartTime = m.StartTime,
            Team1_Player1Id = m.Team1_Player1Id,
            Team2_Player1Id = m.Team2_Player1Id,
            Status = m.Status.ToString()
        }).ToList();

        return Ok(ApiResponse<List<MatchDto>>.Ok(result, "Đã tạo lịch thi đấu"));
    }

    private List<Match> GenerateKnockoutBracket(Tournament tournament, List<TournamentParticipant> participants)
    {
        var matches = new List<Match>();
        var random = new Random();

        // Shuffle participants (keeping seeds at top if any)
        var seededCount = participants.Count(p => p.Seed.HasValue);
        var seeded = participants.Where(p => p.Seed.HasValue).OrderBy(p => p.Seed).ToList();
        var unseeded = participants.Where(p => !p.Seed.HasValue).OrderBy(_ => random.Next()).ToList();
        var orderedParticipants = seeded.Concat(unseeded).ToList();

        // Pad to power of 2
        var bracketSize = 1;
        while (bracketSize < orderedParticipants.Count) bracketSize *= 2;

        var roundNames = new[] { "Chung kết", "Bán kết", "Tứ kết", "Vòng 16", "Vòng 32" };
        var currentDate = tournament.StartDate;

        // First round matches
        var firstRoundMatches = bracketSize / 2;
        var roundIndex = 0;
        while (bracketSize / (int)Math.Pow(2, roundIndex) > 1) roundIndex++;
        
        var roundName = roundIndex < roundNames.Length ? roundNames[roundIndex] : $"Vòng {bracketSize}";

        for (int i = 0; i < orderedParticipants.Count / 2; i++)
        {
            var player1 = orderedParticipants[i * 2];
            var player2 = i * 2 + 1 < orderedParticipants.Count ? orderedParticipants[i * 2 + 1] : null;

            var match = new Match
            {
                TournamentId = tournament.Id,
                RoundName = roundName,
                Date = currentDate,
                StartTime = new TimeSpan(8 + (i % 4) * 2, 0, 0),
                Team1_Player1Id = player1.MemberId,
                Team2_Player1Id = player2?.MemberId,
                IsRanked = true,
                Status = player2 == null ? MatchStatus.Finished : MatchStatus.Scheduled
            };

            if (player2 == null)
            {
                match.WinningSide = WinningSide.Team1;
                match.Score1 = 1;
                match.Score2 = 0;
            }

            matches.Add(match);
        }

        return matches;
    }

    private List<Match> GenerateRoundRobinSchedule(Tournament tournament, List<TournamentParticipant> participants)
    {
        var matches = new List<Match>();
        var currentDate = tournament.StartDate;
        var matchIndex = 0;

        for (int i = 0; i < participants.Count; i++)
        {
            for (int j = i + 1; j < participants.Count; j++)
            {
                var match = new Match
                {
                    TournamentId = tournament.Id,
                    RoundName = "Vòng tròn",
                    Date = currentDate.AddDays(matchIndex / 4),
                    StartTime = new TimeSpan(8 + (matchIndex % 4) * 2, 0, 0),
                    Team1_Player1Id = participants[i].MemberId,
                    Team2_Player1Id = participants[j].MemberId,
                    IsRanked = true,
                    Status = MatchStatus.Scheduled
                };

                matches.Add(match);
                matchIndex++;
            }
        }

        return matches;
    }

    private List<Match> GenerateHybridSchedule(Tournament tournament, List<TournamentParticipant> participants)
    {
        var matches = new List<Match>();
        var groupCount = Math.Max(2, participants.Count / 4);
        var groups = new List<List<TournamentParticipant>>();

        // Divide into groups
        for (int i = 0; i < groupCount; i++)
        {
            groups.Add(new List<TournamentParticipant>());
        }

        for (int i = 0; i < participants.Count; i++)
        {
            var participant = participants[i];
            participant.GroupName = $"Bảng {(char)('A' + i % groupCount)}";
            groups[i % groupCount].Add(participant);
        }

        // Generate group stage matches
        var currentDate = tournament.StartDate;
        var matchIndex = 0;

        foreach (var group in groups)
        {
            for (int i = 0; i < group.Count; i++)
            {
                for (int j = i + 1; j < group.Count; j++)
                {
                    var match = new Match
                    {
                        TournamentId = tournament.Id,
                        RoundName = group[i].GroupName,
                        Date = currentDate.AddDays(matchIndex / 4),
                        StartTime = new TimeSpan(8 + (matchIndex % 4) * 2, 0, 0),
                        Team1_Player1Id = group[i].MemberId,
                        Team2_Player1Id = group[j].MemberId,
                        IsRanked = true,
                        Status = MatchStatus.Scheduled
                    };

                    matches.Add(match);
                    matchIndex++;
                }
            }
        }

        return matches;
    }

    [HttpGet("{id}/bracket")]
    public async Task<ActionResult<ApiResponse<List<BracketDto>>>> GetBracket(int id)
    {
        var matches = await _context.Matches
            .Include(m => m.Team1_Player1)
            .Include(m => m.Team2_Player1)
            .Where(m => m.TournamentId == id)
            .OrderBy(m => m.RoundName)
            .ThenBy(m => m.Date)
            .ThenBy(m => m.StartTime)
            .ToListAsync();

        var brackets = matches
            .GroupBy(m => m.RoundName)
            .Select(g => new BracketDto
            {
                RoundName = g.Key ?? "",
                Matches = g.Select((m, index) => new BracketMatchDto
                {
                    MatchId = m.Id,
                    MatchNumber = index + 1,
                    Team1Name = m.Team1_Player1?.FullName ?? "TBD",
                    Team2Name = m.Team2_Player1?.FullName ?? "TBD",
                    Score1 = m.Status == MatchStatus.Finished ? m.Score1 : null,
                    Score2 = m.Status == MatchStatus.Finished ? m.Score2 : null,
                    Winner = m.WinningSide == WinningSide.Team1 ? m.Team1_Player1?.FullName :
                            m.WinningSide == WinningSide.Team2 ? m.Team2_Player1?.FullName : null
                }).ToList()
            })
            .ToList();

        return Ok(ApiResponse<List<BracketDto>>.Ok(brackets));
    }
}
