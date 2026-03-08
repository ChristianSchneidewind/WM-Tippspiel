using TippSpiel.Models;

namespace TippSpiel.Data
{
    public class InMemoryGameRepository : IGameRepository
    {
        private readonly List<Group> _groups = new();
        private readonly List<Game> _games = new();
        private int _nextGroupId = 1;
        private int _nextGameId = 1;

        public IEnumerable<Group> Groups => _groups;
        public IEnumerable<Game> Games => _games;

        public Group? GetGroup(int id) => _groups.FirstOrDefault(group => group.Id == id);

        public Game? GetGame(int id) => _games.FirstOrDefault(game => game.Id == id);

        public Group AddGroup(string name)
        {
            var group = new Group
            {
                Id = _nextGroupId++,
                Name = name
            };

            _groups.Add(group);
            return group;
        }

        public Game AddGame(Game game)
        {
            var group = GetGroup(game.GroupId);
            if (group == null)
            {
                throw new InvalidOperationException("Group not found.");
            }

            var newGame = new Game
            {
                Id = _nextGameId++,
                HomeTeam = game.HomeTeam,
                AwayTeam = game.AwayTeam,
                KickOff = game.KickOff,
                GroupId = game.GroupId,
                Group = group,
                HomeTeamScore = game.HomeTeamScore,
                AwayTeamScore = game.AwayTeamScore
            };

            _games.Add(newGame);
            group.Games.Add(newGame);
            return newGame;
        }

        public void UpdateGame(Game game)
        {
            var existing = GetGame(game.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Game not found.");
            }

            if (existing.GroupId != game.GroupId)
            {
                var oldGroup = GetGroup(existing.GroupId);
                if (oldGroup != null)
                {
                    oldGroup.Games.Remove(existing);
                }

                var newGroup = GetGroup(game.GroupId);
                if (newGroup == null)
                {
                    throw new InvalidOperationException("Group not found.");
                }

                existing.GroupId = newGroup.Id;
                existing.Group = newGroup;
                newGroup.Games.Add(existing);
            }

            existing.HomeTeam = game.HomeTeam;
            existing.AwayTeam = game.AwayTeam;
            existing.KickOff = game.KickOff;
            existing.HomeTeamScore = game.HomeTeamScore;
            existing.AwayTeamScore = game.AwayTeamScore;
        }

        public void UpdateResult(int gameId, int? homeScore, int? awayScore)
        {
            var existing = GetGame(gameId);
            if (existing == null)
            {
                throw new InvalidOperationException("Game not found.");
            }

            existing.HomeTeamScore = homeScore;
            existing.AwayTeamScore = awayScore;
        }
    }
}
