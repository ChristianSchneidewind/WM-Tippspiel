using TippSpiel.Models;

namespace TippSpiel.Data
{
    public interface IGameRepository
    {
        IEnumerable<Group> Groups { get; }
        IEnumerable<Game> Games { get; }
        IEnumerable<Team> Teams { get; }
        Group? GetGroup(int id);
        Game? GetGame(int id);
        Team? GetTeamByName(string name);
        Team GetOrCreateTeam(string name);
        Group AddGroup(string name);
        Game AddGame(Game game);
        void UpdateGame(Game game);
        void UpdateResult(int gameId, int? homeScore, int? awayScore);
    }
}
