using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SE26Project_18.Backend.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userIdClaim.Value}");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinChat(long chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task LeaveChat(long chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }
}