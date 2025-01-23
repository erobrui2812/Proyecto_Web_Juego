using Microsoft.EntityFrameworkCore;


namespace hundir_la_flota.Repositories
{
    public class GameParticipantRepository : IGameParticipantRepository
    {
        private readonly MyDbContext _context;

        public GameParticipantRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(GameParticipant participant)
        {
            _context.GameParticipants.Add(participant);
            await _context.SaveChangesAsync();
        }

        public async Task<List<GameParticipant>> GetParticipantsByGameIdAsync(Guid gameId)
        {
            return await _context.GameParticipants
                .Include(p => p.User)
                .Where(p => p.GameId == gameId)
                .ToListAsync();
        }

        public async Task<List<GameParticipant>> GetParticipantsByUserIdAsync(int userId)
        {
            return await _context.GameParticipants
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task RemoveAsync(GameParticipant participant)
        {
            _context.GameParticipants.Remove(participant);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GameParticipant participant)
        {
            _context.GameParticipants.Update(participant);
            await _context.SaveChangesAsync();
        }
    }


}
