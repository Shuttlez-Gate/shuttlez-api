using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shuttlez.API.Hubs;

/// <summary>
/// Hub للكابتن: يستقبل تحديثات الرحلات فورًا بدون polling.
/// Group = driver-{userId}
/// </summary>
[Authorize]
public class DriverHub : Hub
{
    public static string GroupName(Guid userId) => $"driver-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId.Value));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = ResolveUserId();
        if (userId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId.Value));
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>يُستدعى من العميل عند التحوّل Online لإعادة الانضمام صراحة.</summary>
    public async Task JoinDriverRoom()
    {
        var userId = ResolveUserId()
            ?? throw new HubException("غير مصرح");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
    }

    private Guid? ResolveUserId()
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
