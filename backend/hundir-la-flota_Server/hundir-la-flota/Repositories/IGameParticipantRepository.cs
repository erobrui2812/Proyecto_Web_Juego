namespace hundir_la_flota.Repositories
{
    public interface IGameParticipantRepository
    {
        Task AddAsync(GameParticipant participant);
        Task<List<GameParticipant>> GetParticipantsByGameIdAsync(Guid gameId);
        Task<List<GameParticipant>> GetParticipantsByUserIdAsync(int userId);
        Task RemoveAsync(GameParticipant participant);
        Task UpdateAsync(GameParticipant participant);
    }
}
