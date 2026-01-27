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
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        var member = await _context.Members.FindAsync(memberId);

        if (member == null)
            return NotFound(ApiResponse<DashboardDto>.Fail("Không tìm thấy thành viên"));

        var now = DateTime.UtcNow;

        // Upcoming bookings
        var upcomingBookings = await _context.Bookings
            .Include(b => b.Court)
            .Where(b => b.MemberId == memberId && b.StartTime > now && b.Status == BookingStatus.Confirmed)
            .OrderBy(b => b.StartTime)
            .Take(5)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                CourtId = b.CourtId,
                CourtName = b.Court!.Name,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status.ToString()
            })
            .ToListAsync();

        // Upcoming matches
        var upcomingMatches = await _context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Team1_Player1)
            .Include(m => m.Team2_Player1)
            .Where(m => m.Date >= now.Date && m.Status == MatchStatus.Scheduled)
            .Where(m => m.Team1_Player1Id == memberId || m.Team1_Player2Id == memberId ||
                       m.Team2_Player1Id == memberId || m.Team2_Player2Id == memberId)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.StartTime)
            .Take(5)
            .Select(m => new MatchDto
            {
                Id = m.Id,
                TournamentName = m.Tournament != null ? m.Tournament.Name : null,
                RoundName = m.RoundName,
                Date = m.Date,
                StartTime = m.StartTime,
                Team1_Player1Name = m.Team1_Player1 != null ? m.Team1_Player1.FullName : null,
                Team2_Player1Name = m.Team2_Player1 != null ? m.Team2_Player1.FullName : null,
                Status = m.Status.ToString()
            })
            .ToListAsync();

        // Pinned news
        var pinnedNews = await _context.News
            .Where(n => n.IsPinned)
            .OrderByDescending(n => n.CreatedDate)
            .Take(3)
            .Select(n => new NewsDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                IsPinned = n.IsPinned,
                CreatedDate = n.CreatedDate,
                ImageUrl = n.ImageUrl
            })
            .ToListAsync();

        // Unread notifications count
        var unreadCount = await _context.Notifications
            .CountAsync(n => n.ReceiverId == memberId && !n.IsRead);

        var dashboard = new DashboardDto
        {
            WalletBalance = member.WalletBalance,
            RankLevel = member.RankLevel,
            UpcomingBookings = upcomingBookings.Count,
            UpcomingMatches = upcomingMatches.Count,
            UnreadNotifications = unreadCount,
            NextBookings = upcomingBookings,
            NextMatches = upcomingMatches,
            PinnedNews = pinnedNews
        };

        return Ok(ApiResponse<DashboardDto>.Ok(dashboard));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin,Treasurer")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetAdminDashboard()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Total members
        var totalMembers = await _context.Members.CountAsync();
        var activeMembers = await _context.Members.CountAsync(m => m.IsActive);

        // Bookings this month
        var bookingsThisMonth = await _context.Bookings
            .CountAsync(b => b.CreatedDate >= startOfMonth);

        // Active tournaments
        var activeTournaments = await _context.Tournaments
            .CountAsync(t => t.Status == TournamentStatus.Ongoing || t.Status == TournamentStatus.Registering);

        // Financial report
        var transactions = await _context.WalletTransactions
            .Where(t => t.Status == TransactionStatus.Completed)
            .ToListAsync();

        var totalRevenue = transactions
            .Where(t => t.Type == TransactionType.Payment)
            .Sum(t => Math.Abs(t.Amount));

        // Monthly data
        var monthlyData = transactions
            .Where(t => t.CreatedDate >= now.AddMonths(-6))
            .GroupBy(t => new { t.CreatedDate.Year, t.CreatedDate.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyReportDto
            {
                Month = $"{g.Key.Month}/{g.Key.Year}",
                Income = g.Where(t => t.Type == TransactionType.Payment).Sum(t => Math.Abs(t.Amount)),
                Expense = g.Where(t => t.Type == TransactionType.Refund || t.Type == TransactionType.Reward)
                    .Sum(t => Math.Abs(t.Amount))
            })
            .ToList();

        var dashboard = new AdminDashboardDto
        {
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            TotalRevenue = totalRevenue,
            BookingsThisMonth = bookingsThisMonth,
            ActiveTournaments = activeTournaments,
            FinancialReport = new FinancialReportDto
            {
                TotalIncome = totalRevenue,
                MonthlyData = monthlyData
            }
        };

        return Ok(ApiResponse<AdminDashboardDto>.Ok(dashboard));
    }
}
