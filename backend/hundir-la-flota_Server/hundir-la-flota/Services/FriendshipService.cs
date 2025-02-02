using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace hundir_la_flota.Services
{
    public interface IFriendshipService
    {
        Task<ServiceResponse<string>> SendFriendRequestAsync(int userId, FriendRequestDto request);
        Task<ServiceResponse<string>> RespondToFriendRequestAsync(int userId, FriendRequestResponseDto response);
        Task<ServiceResponse<List<FriendDto>>> GetFriendsAsync(int userId);
        Task<ServiceResponse<string>> RemoveFriendAsync(int userId, int friendId);
        Task<ServiceResponse<List<FriendRequestDto>>> GetPendingRequestsAsync(int userId);
        Task<ServiceResponse<List<FriendRequestDto>>> GetUnacceptedRequestsAsync(int userId);
        Task<ServiceResponse<List<UserDto>>> SearchUsersAsync(string nickname, int? userId);
        Task<ServiceResponse<string>> GetNicknameAsync(int userId);
        Task<ServiceResponse<List<FriendDto>>> GetConnectedFriendsAsync(int userId);
    }

    public class FriendshipService : IFriendshipService
    {
        private readonly MyDbContext _dbContext;
        private readonly IWebSocketService _webSocketService;


        public FriendshipService(MyDbContext dbContext, IWebSocketService webSocketService)
        {
            _dbContext = dbContext;
            _webSocketService = webSocketService;
        }

        public async Task<ServiceResponse<string>> SendFriendRequestAsync(int userId, FriendRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Nickname) && string.IsNullOrWhiteSpace(request.Email))
            {
                return new ServiceResponse<string> { Success = false, Message = "Debes proporcionar un nickname o un correo electrónico." };
            }

            var friend = await FindUserByNicknameOrEmailAsync(request.Nickname, request.Email);
            if (friend == null)
                return new ServiceResponse<string> { Success = false, Message = "No se encontró el usuario." };

            if (userId == friend.Id)
            {
                return new ServiceResponse<string> { Success = false, Message = "No puedes enviarte una solicitud de amistad a ti mismo." };
            }

            var existingFriendship = await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.UserId == userId && f.FriendId == friend.Id) ||
                    (f.UserId == friend.Id && f.FriendId == userId));

            if (existingFriendship != null)
            {
                return new ServiceResponse<string> { Success = false, Message = "Ya existe una solicitud de amistad o ya sois amigos." };
            }

            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friend.Id,
                IsConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Friendships.Add(friendship);
            await _dbContext.SaveChangesAsync();

            await _webSocketService.NotifyUserAsync(friend.Id, "FriendRequest", userId.ToString());
            return new ServiceResponse<string> { Success = true, Message = "Solicitud de amistad enviada." };
        }


        public async Task<ServiceResponse<string>> RespondToFriendRequestAsync(int userId, FriendRequestResponseDto response)
        {
            var friendship = await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    f.UserId == response.SenderId &&
                    f.FriendId == userId &&
                    !f.IsConfirmed);

            if (friendship == null)
                return new ServiceResponse<string> { Success = false, Message = "Solicitud de amistad no encontrada." };

            if (response.Accept)
            {
                friendship.IsConfirmed = true;
                friendship.ConfirmedAt = DateTime.UtcNow;
            }
            else
            {
                _dbContext.Friendships.Remove(friendship);
            }

            await _dbContext.SaveChangesAsync();

            var responseMessage = response.Accept ? "Accepted" : "Rejected";
            await _webSocketService.NotifyUserAsync(response.SenderId, "FriendRequestResponse", responseMessage);
            return new ServiceResponse<string>
            {
                Success = true,
                Message = response.Accept ? "Solicitud de amistad aceptada." : "Solicitud de amistad rechazada."
            };
        }

        public async Task<ServiceResponse<List<FriendDto>>> GetFriendsAsync(int userId)
        {
            var friendships = await _dbContext.Friendships
                .Where(f => (f.UserId == userId || f.FriendId == userId) && f.IsConfirmed)
                .Select(f => new FriendDto
                {
                    FriendId = f.UserId == userId ? f.FriendId : f.UserId,
                    FriendNickname = f.UserId == userId ? f.Friend.Nickname : f.User.Nickname,
                    FriendMail = f.UserId == userId ? f.Friend.Email : f.User.Email,
                    AvatarUrl = f.UserId == userId ? f.Friend.AvatarUrl : f.User.AvatarUrl,
                    Status = _webSocketService.GetUserState(f.FriendId).ToString()
                })
                .ToListAsync();

            return new ServiceResponse<List<FriendDto>> { Success = true, Data = friendships };
        }

        public async Task<ServiceResponse<string>> RemoveFriendAsync(int userId, int friendId)
        {
            var friendship = await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId)
                    && f.IsConfirmed);

            if (friendship == null)
                return new ServiceResponse<string> { Success = false, Message = "No se encontró la amistad." };

            _dbContext.Friendships.Remove(friendship);
            await _dbContext.SaveChangesAsync();

            return new ServiceResponse<string> { Success = true, Message = "Amigo eliminado." };
        }

        public async Task<ServiceResponse<List<FriendRequestDto>>> GetPendingRequestsAsync(int userId)
        {
            var pendingRequests = await _dbContext.Friendships
                .Where(f => f.FriendId == userId && !f.IsConfirmed)
                .Include(f => f.User)
                .Select(f => new FriendRequestDto
                {
                    Id = f.Id,
                    SenderId = f.UserId,
                    SenderNickname = f.User.Nickname,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return new ServiceResponse<List<FriendRequestDto>> { Success = true, Data = pendingRequests };
        }

        public async Task<ServiceResponse<List<FriendRequestDto>>> GetUnacceptedRequestsAsync(int userId)
        {
            var unacceptedRequests = await _dbContext.Friendships
                .Where(f => f.UserId == userId && !f.IsConfirmed)
                .Include(f => f.Friend)
                .Select(f => new FriendRequestDto
                {
                    Id = f.Id,
                    RecipientId = f.FriendId,
                    RecipientNickname = f.Friend.Nickname,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return new ServiceResponse<List<FriendRequestDto>> { Success = true, Data = unacceptedRequests };
        }

        public async Task<ServiceResponse<List<UserDto>>> SearchUsersAsync(string nickname, int? userId)
        {
            if (string.IsNullOrEmpty(nickname))
                return new ServiceResponse<List<UserDto>> { Success = false, Message = "El nickname no puede estar vacío." };
            
            var normalizedSearch = NormalizeString(nickname).Trim();

            var users = _dbContext.Users
                .AsEnumerable()
                 .Where(u => u.Id != userId && NormalizeString(u.Nickname).Contains(normalizedSearch))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Nickname = u.Nickname,
                    AvatarUrl = u.AvatarUrl
                })
                .ToList();

            if (users.Count == 0)
                return new ServiceResponse<List<UserDto>> { Success = false, Message = "No se encontraron usuarios con ese nickname." };

            return new ServiceResponse<List<UserDto>> { Success = true, Data = users };
        }

        public async Task<ServiceResponse<string>> GetNicknameAsync(int userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Usuario no encontrado." };

            return new ServiceResponse<string> { Success = true, Data = user.Nickname };
        }

        public async Task<User> FindUserByNicknameOrEmailAsync(string nickname, string email)
        {

            var normalizedNickname = nickname?.ToLower();
            var normalizedEmail = email?.ToLower();


            var user = await _dbContext.Users
                .Where(u => u.Nickname.ToLower() == normalizedNickname || u.Email.ToLower() == normalizedEmail)
                .FirstOrDefaultAsync();

            return user;
        }

        public async Task<ServiceResponse<List<FriendDto>>> GetConnectedFriendsAsync(int userId)
        {
            var friendships = await _dbContext.Friendships
                .Where(f => (f.UserId == userId || f.FriendId == userId) && f.IsConfirmed)
                .Include(f => f.User)
                .Include(f => f.Friend)
                .ToListAsync();

            var connectedFriends = friendships
                .Select(f => new
                {
                    FriendId = f.UserId == userId ? f.FriendId : f.UserId,
                    Nickname = f.UserId == userId ? f.Friend.Nickname : f.User.Nickname,
                    Email = f.UserId == userId ? f.Friend.Email : f.User.Email,
                    AvatarUrl = f.UserId == userId ? f.Friend.AvatarUrl : f.User.AvatarUrl,
                    Status = _webSocketService.IsUserConnected(f.UserId == userId ? f.FriendId : f.UserId)
                        ? "Connected"
                        : "Disconnected"
                })
                .Where(f => f.Status == "Connected")
                .ToList();

            var result = connectedFriends.Select(f => new FriendDto
            {
                FriendId = f.FriendId,
                FriendNickname = f.Nickname,
                FriendMail = f.Email,
                AvatarUrl = f.AvatarUrl,
                Status = f.Status
            }).ToList();

            return new ServiceResponse<List<FriendDto>> { Success = true, Data = result };
        }





        private string NormalizeString(string input)
        {
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var character in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(character);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower();
        }
    }
}
