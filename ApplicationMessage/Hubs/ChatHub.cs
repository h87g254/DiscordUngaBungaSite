using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ApplicationMessage.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendPrivateMessage(int toUserId, string senderUsername, string message)
        {
            await Clients.Group($"user_{toUserId}").SendAsync("ReceivePrivateMessage", senderUsername, message);
        }

        public async Task SendRoomMessage(int roomId, string senderUsername, string message)
        {
            await Clients.Group($"room_{roomId}").SendAsync("ReceiveRoomMessage", senderUsername, message);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userId, out int id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{id}");
            }

            var roomIdQuery = httpContext.Request.Query["roomId"];
            if (int.TryParse(roomIdQuery, out int roomId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
            }

            await base.OnConnectedAsync();
        }
    }
}
