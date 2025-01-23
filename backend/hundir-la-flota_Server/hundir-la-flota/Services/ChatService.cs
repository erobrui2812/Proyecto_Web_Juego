using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hundir_la_flota.Services
{
    public interface IChatService
    {
        Task<ServiceResponse<string>> SendMessageAsync(Guid gameId, int userId, string message);
        Task<ServiceResponse<List<ChatMessageDTO>>> GetMessagesAsync(Guid gameId);
    }

    public class ChatService : IChatService
    {
        private readonly ConcurrentDictionary<Guid, List<ChatMessageDTO>> _messagesByGame;

        public ChatService()
        {

            _messagesByGame = new ConcurrentDictionary<Guid, List<ChatMessageDTO>>();
        }

        public async Task<ServiceResponse<string>> SendMessageAsync(Guid gameId, int userId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "El mensaje no puede estar vacío."
                };
            }

            var chatMessage = new ChatMessageDTO
            {
                SenderId = userId,
                Message = message,
                SentAt = DateTime.UtcNow
            };

            // Añadir el mensaje al almacén en memoria
            _messagesByGame.AddOrUpdate(
            gameId,
                new List<ChatMessageDTO> { chatMessage },
                (key, existingMessages) =>
                {
                    existingMessages.Add(chatMessage);
                    return existingMessages;
                });

            return await Task.FromResult(new ServiceResponse<string>
            {
                Success = true,
                Message = "Mensaje enviado con éxito."
            });
        }

        public async Task<ServiceResponse<List<ChatMessageDTO>>> GetMessagesAsync(Guid gameId)
        {

            if (_messagesByGame.TryGetValue(gameId, out var messages))
            {
                return await Task.FromResult(new ServiceResponse<List<ChatMessageDTO>>
                {
                    Success = true,
                    Data = messages.OrderBy(m => m.SentAt).ToList()
                });
            }

            return await Task.FromResult(new ServiceResponse<List<ChatMessageDTO>>
            {
                Success = false,
                Message = "No se encontraron mensajes para este juego."
            });
        }
    }
}