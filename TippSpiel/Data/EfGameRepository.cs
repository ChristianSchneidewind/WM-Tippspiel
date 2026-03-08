using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data
{
    public class EfGameRepository : IGameRepository
    {
        private readonly ApplicationDbContext _db;

        public EfGameRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public IEnumerable<Group> Groups => _db.Groups.Include(group => group.Games).AsNoTracking().ToList();

        public IEnumerable<Game> Games => _db.Games.Include(game => game.Group).AsNoTracking().ToList();

        public Group? GetGroup(int id) => _db.Groups.Include(group => group.Games).FirstOrDefault(group => group.Id == id);

        public Game? GetGame(int id) => _db.Games.Include(game => game.Group).FirstOrDefault(game => game.Id == id);

        public Group AddGroup(string name)
        {
            var group = new Group { Name = name };
            _db.Groups.Add(group);
            _db.SaveChanges();
            return group;
        }

        public Game AddGame(Game game)
        {
            var group = _db.Groups.FirstOrDefault(existing => existing.Id == game.GroupId);
            if (group == null)
            {
                throw new InvalidOperationException("Group not found.");
            }

            game.Group = group;
            _db.Games.Add(game);
            _db.SaveChanges();
            return game;
        }

        public void UpdateGame(Game game)
        {
            var existing = _db.Games.FirstOrDefault(existingGame => existingGame.Id == game.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Game not found.");
            }

            existing.HomeTeam = game.HomeTeam;
            existing.AwayTeam = game.AwayTeam;
            existing.KickOff = game.KickOff;
            existing.GroupId = game.GroupId;
            existing.HomeTeamScore = game.HomeTeamScore;
            existing.AwayTeamScore = game.AwayTeamScore;

            _db.SaveChanges();
        }

        public void UpdateResult(int gameId, int? homeScore, int? awayScore)
        {
            var existing = _db.Games.FirstOrDefault(game => game.Id == gameId);
            if (existing == null)
            {
                throw new InvalidOperationException("Game not found.");
            }

            existing.HomeTeamScore = homeScore;
            existing.AwayTeamScore = awayScore;
            _db.SaveChanges();
        }
    }
}
