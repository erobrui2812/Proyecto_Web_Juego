using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace hundir_la_flota.Hubs
{
    public class NotificationHub : Hub
    {
        
        public async Task ReceiveFriendRequest(int userId)
        {
            await Clients.User(userId.ToString()).SendAsync("ReceiveFriendRequest", userId);
        }

        
        public async Task FriendRequestResponse(int userId, bool accepted)
        {
            await Clients.User(userId.ToString()).SendAsync("FriendRequestResponse", accepted);
        }

        
        public async Task FriendRemoved(int friendId)
        {
            await Clients.User(friendId.ToString()).SendAsync("FriendRemoved", friendId);
        }
    }
}
