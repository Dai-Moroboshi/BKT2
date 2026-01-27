using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCM.API.Data;
using PCM.API.DTOs;
using PCM.API.Entities;

namespace PCM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MembersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<MemberDto>>>> GetMembers(
        [FromQuery] string? search,
        [FromQuery] string? tier,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Members.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.FullName.Contains(search));
        }

        if (!string.IsNullOrEmpty(tier) && Enum.TryParse<MemberTier>(tier, out var tierEnum))
        {
            query = query.Where(m => m.Tier == tierEnum);
        }

        var totalCount = await query.CountAsync();

        var members = await query
            .OrderByDescending(m => m.RankLevel)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MemberDto
            {
                Id = m.Id,
                FullName = m.FullName,
                JoinDate = m.JoinDate,
                RankLevel = m.RankLevel,
                IsActive = m.IsActive,
                Tier = m.Tier.ToString(),
                AvatarUrl = m.AvatarUrl
            })
            .ToListAsync();

        var result = new PaginatedResult<MemberDto>
        {
            Items = members,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<MemberDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MemberDetailDto>>> GetMember(int id)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member == null)
            return NotFound(ApiResponse<MemberDetailDto>.Fail("Không tìm thấy thành viên"));

        // Get match statistics
        var matches = await _context.Matches
            .Where(m => m.Team1_Player1Id == id || m.Team1_Player2Id == id ||
                       m.Team2_Player1Id == id || m.Team2_Player2Id == id)
            .Where(m => m.Status == MatchStatus.Finished)
            .ToListAsync();

        var wins = matches.Count(m =>
            (m.WinningSide == WinningSide.Team1 && (m.Team1_Player1Id == id || m.Team1_Player2Id == id)) ||
            (m.WinningSide == WinningSide.Team2 && (m.Team2_Player1Id == id || m.Team2_Player2Id == id)));

        var losses = matches.Count - wins;

        // Get recent matches
        var recentMatches = matches
            .OrderByDescending(m => m.Date)
            .Take(5)
            .Select(m => new MatchSummaryDto
            {
                MatchId = m.Id,
                Date = m.Date,
                Score = $"{m.Score1} - {m.Score2}",
                IsWin = (m.WinningSide == WinningSide.Team1 && (m.Team1_Player1Id == id || m.Team1_Player2Id == id)) ||
                       (m.WinningSide == WinningSide.Team2 && (m.Team2_Player1Id == id || m.Team2_Player2Id == id)),
                TournamentName = m.Tournament?.Name
            })
            .ToList();

        var result = new MemberDetailDto
        {
            Id = member.Id,
            FullName = member.FullName,
            Email = member.User?.Email ?? "",
            JoinDate = member.JoinDate,
            RankLevel = member.RankLevel,
            IsActive = member.IsActive,
            Tier = member.Tier.ToString(),
            AvatarUrl = member.AvatarUrl,
            TotalSpent = member.TotalSpent,
            TotalMatches = matches.Count,
            Wins = wins,
            Losses = losses,
            RecentMatches = recentMatches
        };

        return Ok(ApiResponse<MemberDetailDto>.Ok(result));
    }

    [HttpGet("{id}/profile")]
    public async Task<ActionResult<ApiResponse<MemberDetailDto>>> GetMemberProfile(int id)
    {
        return await GetMember(id);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<MemberDto>>> UpdateProfile([FromBody] UpdateMemberDto dto)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        var member = await _context.Members.FindAsync(memberId);

        if (member == null)
            return NotFound(ApiResponse<MemberDto>.Fail("Không tìm thấy thông tin thành viên"));

        if (!string.IsNullOrEmpty(dto.FullName))
            member.FullName = dto.FullName;

        if (!string.IsNullOrEmpty(dto.AvatarUrl))
            member.AvatarUrl = dto.AvatarUrl;

        await _context.SaveChangesAsync();

        var result = new MemberDto
        {
            Id = member.Id,
            FullName = member.FullName,
            JoinDate = member.JoinDate,
            RankLevel = member.RankLevel,
            IsActive = member.IsActive,
            Tier = member.Tier.ToString(),
            AvatarUrl = member.AvatarUrl
        };

        return Ok(ApiResponse<MemberDto>.Ok(result, "Cập nhật thông tin thành công"));
    }
}
