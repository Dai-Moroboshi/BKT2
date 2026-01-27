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
public class MatchesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<PcmHub> _hubContext;

    public MatchesController(ApplicationDbContext context, IHubContext<PcmHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MatchDto>>>> GetMatches(
        [FromQuery] int? tournamentId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Team1_Player1)
            .Include(m => m.Team1_Player2)
            .Include(m => m.Team2_Player1)
            .Include(m => m.Team2_Player2)
            .Include(m => m.Court)
            .AsQueryable();

        if (tournamentId.HasValue)
            query = query.Where(m => m.TournamentId == tournamentId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MatchStatus>(status, out var statusEnum))
            query = query.Where(m => m.Status == statusEnum);

        if (from.HasValue)
            query = query.Where(m => m.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.Date <= to.Value);

        var matches = await query
            .OrderBy(m => m.Date)
            .ThenBy(m => m.StartTime)
            .Select(m => new MatchDto
            {
                Id = m.Id,
                TournamentId = m.TournamentId,
                TournamentName = m.Tournament != null ? m.Tournament.Name : null,
                RoundName = m.RoundName,
                Date = m.Date,
                StartTime = m.StartTime,
                Team1_Player1Id = m.Team1_Player1Id,
                Team1_Player1Name = m.Team1_Player1 != null ? m.Team1_Player1.FullName : null,
                Team1_Player2Id = m.Team1_Player2Id,
                Team1_Player2Name = m.Team1_Player2 != null ? m.Team1_Player2.FullName : null,
                Team2_Player1Id = m.Team2_Player1Id,
                Team2_Player1Name = m.Team2_Player1 != null ? m.Team2_Player1.FullName : null,
                Team2_Player2Id = m.Team2_Player2Id,
                Team2_Player2Name = m.Team2_Player2 != null ? m.Team2_Player2.FullName : null,
                Score1 = m.Score1,
                Score2 = m.Score2,
                Details = m.Details,
                WinningSide = m.WinningSide.ToString(),
                IsRanked = m.IsRanked,
                Status = m.Status.ToString(),
                CourtId = m.CourtId,
                CourtName = m.Court != null ? m.Court.Name : null
            })
            .ToListAsync();

        return Ok(ApiResponse<List<MatchDto>>.Ok(matches));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MatchDto>>> GetMatch(int id)
    {
        var match = await _context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Team1_Player1)
            .Include(m => m.Team1_Player2)
            .Include(m => m.Team2_Player1)
            .Include(m => m.Team2_Player2)
            .Include(m => m.Court)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound(ApiResponse<MatchDto>.Fail("Không tìm thấy trận đấu"));

        var result = new MatchDto
        {
            Id = match.Id,
            TournamentId = match.TournamentId,
            TournamentName = match.Tournament?.Name,
            RoundName = match.RoundName,
            Date = match.Date,
            StartTime = match.StartTime,
            Team1_Player1Id = match.Team1_Player1Id,
            Team1_Player1Name = match.Team1_Player1?.FullName,
            Team1_Player2Id = match.Team1_Player2Id,
            Team1_Player2Name = match.Team1_Player2?.FullName,
            Team2_Player1Id = match.Team2_Player1Id,
            Team2_Player1Name = match.Team2_Player1?.FullName,
            Team2_Player2Id = match.Team2_Player2Id,
            Team2_Player2Name = match.Team2_Player2?.FullName,
            Score1 = match.Score1,
            Score2 = match.Score2,
            Details = match.Details,
            WinningSide = match.WinningSide.ToString(),
            IsRanked = match.IsRanked,
            Status = match.Status.ToString(),
            CourtId = match.CourtId,
            CourtName = match.Court?.Name
        };

        return Ok(ApiResponse<MatchDto>.Ok(result));
    }

    [HttpPost("{id}/result")]
    [Authorize(Roles = "Admin,Referee")]
    public async Task<ActionResult<ApiResponse<MatchDto>>> UpdateResult(int id, [FromBody] UpdateMatchResultDto dto)
    {
        var match = await _context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Team1_Player1)
            .Include(m => m.Team2_Player1)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound(ApiResponse<MatchDto>.Fail("Không tìm thấy trận đấu"));

        match.Score1 = dto.Score1;
        match.Score2 = dto.Score2;
        match.Details = dto.Details;
        match.WinningSide = Enum.Parse<WinningSide>(dto.WinningSide);
        match.Status = MatchStatus.Finished;

        // Update DUPR ranks if ranked match
        if (match.IsRanked)
        {
            await UpdatePlayerRanks(match);
        }

        await _context.SaveChangesAsync();

        // Notify via SignalR
        await _hubContext.Clients.Group($"match_{id}").SendAsync("UpdateMatchScore", new
        {
            matchId = id,
            score1 = match.Score1,
            score2 = match.Score2,
            winningSide = match.WinningSide.ToString(),
            status = match.Status.ToString()
        });

        // Check if tournament should end
        if (match.TournamentId.HasValue)
        {
            await CheckTournamentCompletion(match.TournamentId.Value);
        }

        var result = new MatchDto
        {
            Id = match.Id,
            TournamentId = match.TournamentId,
            Score1 = match.Score1,
            Score2 = match.Score2,
            Details = match.Details,
            WinningSide = match.WinningSide.ToString(),
            Status = match.Status.ToString()
        };

        return Ok(ApiResponse<MatchDto>.Ok(result, "Cập nhật kết quả thành công"));
    }

    private async Task UpdatePlayerRanks(Match match)
    {
        // Simple DUPR-like calculation
        var winnerIds = new List<int?>();
        var loserIds = new List<int?>();

        if (match.WinningSide == WinningSide.Team1)
        {
            winnerIds.AddRange(new[] { match.Team1_Player1Id, match.Team1_Player2Id });
            loserIds.AddRange(new[] { match.Team2_Player1Id, match.Team2_Player2Id });
        }
        else
        {
            winnerIds.AddRange(new[] { match.Team2_Player1Id, match.Team2_Player2Id });
            loserIds.AddRange(new[] { match.Team1_Player1Id, match.Team1_Player2Id });
        }

        var allIds = winnerIds.Concat(loserIds).Where(id => id.HasValue).Select(id => id!.Value).ToList();
        var players = await _context.Members.Where(m => allIds.Contains(m.Id)).ToListAsync();

        foreach (var player in players)
        {
            if (winnerIds.Contains(player.Id))
            {
                player.RankLevel = Math.Min(5.5, player.RankLevel + 0.05);
            }
            else
            {
                player.RankLevel = Math.Max(2.0, player.RankLevel - 0.03);
            }
        }
    }

    private async Task CheckTournamentCompletion(int tournamentId)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Matches)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null) return;

        var allMatchesFinished = tournament.Matches.All(m => m.Status == MatchStatus.Finished);

        if (allMatchesFinished && tournament.Status != TournamentStatus.Finished)
        {
            tournament.Status = TournamentStatus.Finished;

            // Award prize pool to winner(s)
            if (tournament.PrizePool > 0)
            {
                await AwardPrizes(tournament);
            }

            await _context.SaveChangesAsync();
        }
    }

    private async Task AwardPrizes(Tournament tournament)
    {
        // Find final match winner
        var finalMatch = tournament.Matches
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.StartTime)
            .FirstOrDefault(m => m.Status == MatchStatus.Finished);

        if (finalMatch == null) return;

        var winnerId = finalMatch.WinningSide == WinningSide.Team1
            ? finalMatch.Team1_Player1Id
            : finalMatch.Team2_Player1Id;

        if (!winnerId.HasValue) return;

        var winner = await _context.Members.FindAsync(winnerId.Value);
        if (winner == null) return;

        // Award 70% to winner, 30% to runner-up
        var firstPrize = tournament.PrizePool * 0.7m;
        var secondPrize = tournament.PrizePool * 0.3m;

        var winnerTransaction = new WalletTransaction
        {
            MemberId = winnerId.Value,
            Amount = firstPrize,
            Type = TransactionType.Reward,
            Status = TransactionStatus.Completed,
            Description = $"Giải nhất - {tournament.Name}",
            RelatedId = tournament.Id.ToString(),
            CreatedDate = DateTime.UtcNow
        };
        _context.WalletTransactions.Add(winnerTransaction);
        winner.WalletBalance += firstPrize;

        // Notify winner
        var notification = new Notification
        {
            ReceiverId = winnerId.Value,
            Message = $"Chúc mừng! Bạn đã giành giải nhất {tournament.Name} và nhận {firstPrize:N0}đ",
            Type = NotificationType.Success,
            CreatedDate = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);

        // Award runner-up
        var runnerUpId = finalMatch.WinningSide == WinningSide.Team1
            ? finalMatch.Team2_Player1Id
            : finalMatch.Team1_Player1Id;

        if (runnerUpId.HasValue)
        {
            var runnerUp = await _context.Members.FindAsync(runnerUpId.Value);
            if (runnerUp != null)
            {
                var runnerUpTransaction = new WalletTransaction
                {
                    MemberId = runnerUpId.Value,
                    Amount = secondPrize,
                    Type = TransactionType.Reward,
                    Status = TransactionStatus.Completed,
                    Description = $"Giải nhì - {tournament.Name}",
                    RelatedId = tournament.Id.ToString(),
                    CreatedDate = DateTime.UtcNow
                };
                _context.WalletTransactions.Add(runnerUpTransaction);
                runnerUp.WalletBalance += secondPrize;
            }
        }
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<List<MatchDto>>>> GetMyMatches()
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var matches = await _context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Team1_Player1)
            .Include(m => m.Team2_Player1)
            .Where(m => m.Team1_Player1Id == memberId || m.Team1_Player2Id == memberId ||
                       m.Team2_Player1Id == memberId || m.Team2_Player2Id == memberId)
            .OrderByDescending(m => m.Date)
            .Select(m => new MatchDto
            {
                Id = m.Id,
                TournamentId = m.TournamentId,
                TournamentName = m.Tournament != null ? m.Tournament.Name : null,
                RoundName = m.RoundName,
                Date = m.Date,
                StartTime = m.StartTime,
                Team1_Player1Name = m.Team1_Player1 != null ? m.Team1_Player1.FullName : null,
                Team2_Player1Name = m.Team2_Player1 != null ? m.Team2_Player1.FullName : null,
                Score1 = m.Score1,
                Score2 = m.Score2,
                WinningSide = m.WinningSide.ToString(),
                Status = m.Status.ToString()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<MatchDto>>.Ok(matches));
    }
}
