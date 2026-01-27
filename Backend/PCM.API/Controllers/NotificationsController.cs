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
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var query = _context.Notifications
            .Where(n => n.ReceiverId == memberId);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type.ToString(),
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedDate = n.CreatedDate
            })
            .ToListAsync();

        return Ok(ApiResponse<List<NotificationDto>>.Ok(notifications));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var count = await _context.Notifications
            .CountAsync(n => n.ReceiverId == memberId && !n.IsRead);

        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(int id)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.ReceiverId == memberId);

        if (notification == null)
            return NotFound(ApiResponse<bool>.Fail("Không tìm thấy thông báo"));

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Ok(true, "Đã đánh dấu đã đọc"));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead()
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var notifications = await _context.Notifications
            .Where(n => n.ReceiverId == memberId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<int>.Ok(notifications.Count, $"Đã đánh dấu {notifications.Count} thông báo đã đọc"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(int id)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.ReceiverId == memberId);

        if (notification == null)
            return NotFound(ApiResponse<bool>.Fail("Không tìm thấy thông báo"));

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Ok(true, "Đã xóa thông báo"));
    }
}
