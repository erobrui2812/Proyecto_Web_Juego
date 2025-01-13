
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
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;                                      
            await UpdateUserStatus(int.Parse(userId), "Conectado");
            await base.OnConnectedAsync();
        }

        // Cuando un cliente se desconecta
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;  
            await UpdateUserStatus(int.Parse(userId), "Desconectado");
            await base.OnDisconnectedAsync(exception);
        }
    }

}
