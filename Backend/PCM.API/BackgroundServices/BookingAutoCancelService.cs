using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PCM.API.Data;
using PCM.API.Entities;
using PCM.API.Hubs;

namespace PCM.API.BackgroundServices;

/// <summary>
/// Background service that auto-cancels bookings that have been in "Holding" status for more than 5 minutes
/// </summary>
public class BookingAutoCancelService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingAutoCancelService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _holdTimeout = TimeSpan.FromMinutes(5);

    public BookingAutoCancelService(
        IServiceProvider serviceProvider,
        ILogger<BookingAutoCancelService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingAutoCancelService started");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait for DB migration

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredHolds(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired holds");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessExpiredHolds(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PcmHub>>();

        var expiredHolds = await context.Bookings
            .Include(b => b.Court)
            .Where(b => b.Status == BookingStatus.Holding &&
                       b.HoldStartTime.HasValue &&
                       b.HoldStartTime.Value.AddMinutes(5) < DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        foreach (var booking in expiredHolds)
        {
            booking.Status = BookingStatus.Cancelled;
            _logger.LogInformation("Auto-cancelled expired hold for booking {BookingId}", booking.Id);

            // Notify via SignalR
            await hubContext.Clients.All.SendAsync("UpdateCalendar", new
            {
                courtId = booking.CourtId,
                startTime = booking.StartTime,
                endTime = booking.EndTime,
                status = "available"
            }, stoppingToken);
        }

        if (expiredHolds.Count > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Cancelled {Count} expired holds", expiredHolds.Count);
        }
    }
}
