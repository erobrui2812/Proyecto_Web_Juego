using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace hundir_la_flota.Hubs
{
    public class MatchmakingHub : Hub
    {
       
        private static readonly ConcurrentDictionary<string, UserStatus> ConnectedUsers = new();

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
               
                ConnectedUsers[userId] = new UserStatus
                {
                    ConnectionId = Context.ConnectionId,
                    IsHost = false,
                    InMatchmaking = false
                };
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
          
                ConnectedUsers.TryRemove(userId, out _);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task InviteFriend(string friendId)
        {
           
            if (ConnectedUsers.TryGetValue(friendId, out var friendStatus))
            {
                await Clients.Client(friendStatus.ConnectionId).SendAsync("ReceiveInvitation", Context.UserIdentifier);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "El amigo no está conectado.");
            }
        }

        public async Task AcceptInvitation(string hostId)
        {
            if (ConnectedUsers.TryGetValue(hostId, out var hostStatus))
            {
              
                await Clients.Client(hostStatus.ConnectionId).SendAsync("InvitationAccepted", Context.UserIdentifier);
            }
        }

        public async Task JoinRandomMatch()
        {
           
            var userId = Context.UserIdentifier;

            var randomOpponent = ConnectedUsers.Values
                .FirstOrDefault(u => u.InMatchmaking && u.ConnectionId != Context.ConnectionId);

            if (randomOpponent != null)
            {
             
                await Clients.Client(randomOpponent.ConnectionId).SendAsync("Matched", userId);
                await Clients.Caller.SendAsync("Matched", randomOpponent.ConnectionId);
            }
            else
            {
              
                if (ConnectedUsers.TryGetValue(userId, out var userStatus))
                {
                    userStatus.InMatchmaking = true;
                }

                await Clients.Caller.SendAsync("WaitingForMatch");
            }
        }

        public async Task PlayWithBot()
        {
            
            await Clients.Caller.SendAsync("MatchedWithBot");
        }
    }

    public class UserStatus
    {
        public string ConnectionId { get; set; }
        public bool IsHost { get; set; }
        public bool InMatchmaking { get; set; }
    }
}
