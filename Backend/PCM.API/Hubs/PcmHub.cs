using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PCM.API.Hubs;

[Authorize]
public class PcmHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a match group to receive real-time updates for that match
    /// </summary>
    public async Task JoinMatchGroup(int matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match_{matchId}");
    }

    /// <summary>
    /// Leave a match group
    /// </summary>
    public async Task LeaveMatchGroup(int matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match_{matchId}");
    }

    /// <summary>
    /// Join a tournament group to receive updates for all matches in that tournament
    /// </summary>
    public async Task JoinTournamentGroup(int tournamentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tournament_{tournamentId}");
    }

    /// <summary>
    /// Leave a tournament group
    /// </summary>
    public async Task LeaveTournamentGroup(int tournamentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament_{tournamentId}");
    }

    /// <summary>
    /// Subscribe to calendar updates for a specific court
    /// </summary>
    public async Task SubscribeToCourtCalendar(int courtId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"court_{courtId}");
    }

    /// <summary>
    /// Unsubscribe from court calendar updates
    /// </summary>
    public async Task UnsubscribeFromCourtCalendar(int courtId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"court_{courtId}");
    }

    // Client methods (called from server):
    // - ReceiveNotification(object notification)
    // - UpdateCalendar(object calendarUpdate)
    // - UpdateMatchScore(object matchUpdate)
    // - WalletUpdated(decimal newBalance)
}
