
namespace hundir_la_flota.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using System.Threading.Tasks;

    public class StatusHub : Hub
    {
        
        public async Task UpdateUserStatus(int userId, string status)
        {
            
            
            await Clients.All.SendAsync("ReceiveUserStatus", userId, status);
        }

        // Cuando un cliente se conecta
        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.Identity?.Name;
            if (userId != null)
            {
                Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            return base.OnConnectedAsync();
        }


        //public override async Task OnMatchAsync()
        //{
        //    var userId = Context.UserIdentifier;
        //    await UpdateUserStatus(int.Parse(userId), "Jugando");
        //    await base.OnMatchAsync();
        //}

        // Cuando un cliente se desconecta
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;  
            await UpdateUserStatus(int.Parse(userId), "Desconectado");
            await base.OnDisconnectedAsync(exception);
        }
    }

}
