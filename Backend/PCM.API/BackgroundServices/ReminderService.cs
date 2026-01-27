using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PCM.API.Data;
using PCM.API.Entities;
using PCM.API.Hubs;

namespace PCM.API.BackgroundServices;

/// <summary>
/// Background service that sends reminders for upcoming bookings and matches
/// </summary>
public class ReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public ReminderService(
        IServiceProvider serviceProvider,
        ILogger<ReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendBookingReminders(stoppingToken);
                await SendMatchReminders(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminders");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task SendBookingReminders(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PcmHub>>();

        var tomorrow = DateTime.UtcNow.AddDays(1);
        var tomorrowEnd = tomorrow.AddHours(1);

        // Find bookings happening in ~24 hours that haven't been reminded
        var upcomingBookings = await context.Bookings
            .Include(b => b.Member)
            .Include(b => b.Court)
            .Where(b => b.Status == BookingStatus.Confirmed &&
                       b.StartTime >= tomorrow &&
                       b.StartTime <= tomorrowEnd)
            .ToListAsync(stoppingToken);

        foreach (var booking in upcomingBookings)
        {
            // Check if reminder already sent
            var reminderExists = await context.Notifications
                .AnyAsync(n => n.ReceiverId == booking.MemberId &&
                              n.Message.Contains($"Booking #{booking.Id}") &&
                              n.CreatedDate >= DateTime.UtcNow.AddHours(-12), stoppingToken);

            if (!reminderExists && booking.Member != null)
            {
                var notification = new Notification
                {
                    ReceiverId = booking.MemberId,
                    Message = $"Nhắc nhở: Bạn có lịch đặt sân {booking.Court?.Name} vào {booking.StartTime:HH:mm dd/MM/yyyy} (Booking #{booking.Id})",
                    Type = NotificationType.Info,
                    CreatedDate = DateTime.UtcNow
                };
                context.Notifications.Add(notification);

                // Send real-time notification
                await hubContext.Clients.User(booking.Member.UserId)
                    .SendAsync("ReceiveNotification", new
                    {
                        message = notification.Message,
                        type = "Info"
                    }, stoppingToken);

                _logger.LogInformation("Sent booking reminder to member {MemberId} for booking {BookingId}",
                    booking.MemberId, booking.Id);
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }

    private async Task SendMatchReminders(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PcmHub>>();

        var tomorrow = DateTime.UtcNow.AddDays(1).Date;

        // Find matches happening tomorrow
        var upcomingMatches = await context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Team1_Player1)
            .Include(m => m.Team1_Player2)
            .Include(m => m.Team2_Player1)
            .Include(m => m.Team2_Player2)
            .Where(m => m.Status == MatchStatus.Scheduled &&
                       m.Date.Date == tomorrow)
            .ToListAsync(stoppingToken);

        foreach (var match in upcomingMatches)
        {
            var playerIds = new List<int?>
            {
                match.Team1_Player1Id,
                match.Team1_Player2Id,
                match.Team2_Player1Id,
                match.Team2_Player2Id
            }.Where(id => id.HasValue).Select(id => id!.Value).ToList();

            var members = await context.Members
                .Where(m => playerIds.Contains(m.Id))
                .ToListAsync(stoppingToken);

            foreach (var member in members)
            {
                // Check if reminder already sent
                var reminderExists = await context.Notifications
                    .AnyAsync(n => n.ReceiverId == member.Id &&
                                  n.Message.Contains($"Match #{match.Id}") &&
                                  n.CreatedDate >= DateTime.UtcNow.AddHours(-12), stoppingToken);

                if (!reminderExists)
                {
                    var tournamentInfo = match.Tournament != null ? $" ({match.Tournament.Name})" : "";
                    var notification = new Notification
                    {
                        ReceiverId = member.Id,
                        Message = $"Nhắc nhở: Bạn có trận đấu{tournamentInfo} vào {match.Date:dd/MM/yyyy} lúc {match.StartTime:hh\\:mm} (Match #{match.Id})",
                        Type = NotificationType.Info,
                        CreatedDate = DateTime.UtcNow
                    };
                    context.Notifications.Add(notification);

                    await hubContext.Clients.User(member.UserId)
                        .SendAsync("ReceiveNotification", new
                        {
                            message = notification.Message,
                            type = "Info"
                        }, stoppingToken);

                    _logger.LogInformation("Sent match reminder to member {MemberId} for match {MatchId}",
                        member.Id, match.Id);
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}
