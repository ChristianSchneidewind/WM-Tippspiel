using System.Collections.Generic;
using System.Linq;
using TippSpiel.Data;
using TippSpiel.Models;

namespace TippSpiel.Services
{
    public class GroupStandingsService
    {
        private readonly IGameRepository _repository;

        public GroupStandingsService(IGameRepository repository)
        {
            _repository = repository;
        }

        public List<GroupStanding> CalculateGroupStandings(int groupId)
        {
            var group = _repository.GetGroup(groupId);
            if (group == null)
            {
                return new List<GroupStanding>();
            }

            var teamIds = new HashSet<int>();

            foreach (var game in group.Games ?? new List<Game>())
            {
                if (game.HomeTeamId.HasValue)
                {
                    teamIds.Add(game.HomeTeamId.Value);
                }

                if (game.AwayTeamId.HasValue)
                {
                    teamIds.Add(game.AwayTeamId.Value);
                }
            }

            var standings = new List<GroupStanding>();

            foreach (var teamId in teamIds)
            {
                var team = _repository.Teams.FirstOrDefault(t => t.Id == teamId);
                if (team == null)
                {
                    continue;
                }

                var standing = new GroupStanding
                {
                    Team = team
                };

                var gamesWithTeam = (group.Games ?? new List<Game>())
                    .Where(g =>
                        (g.HomeTeamId == teamId || g.AwayTeamId == teamId) &&
                        g.HomeTeamScore.HasValue && g.AwayTeamScore.HasValue)
                    .ToList();

                foreach (var game in gamesWithTeam)
                {
                    standing.Played++;

                    var homeScore = game.HomeTeamScore!.Value;
                    var awayScore = game.AwayTeamScore!.Value;

                    if (game.HomeTeamId == teamId)
                    {
                        standing.GoalsFor += homeScore;
                        standing.GoalsAgainst += awayScore;

                        if (homeScore > awayScore)
                        {
                            standing.Won++;
                            standing.Points += 3;
                        }
                        else if (homeScore == awayScore)
                        {
                            standing.Drawn++;
                            standing.Points += 1;
                        }
                        else
                        {
                            standing.Lost++;
                        }
                    }
                    else
                    {
                        standing.GoalsFor += awayScore;
                        standing.GoalsAgainst += homeScore;

                        if (awayScore > homeScore)
                        {
                            standing.Won++;
                            standing.Points += 3;
                        }
                        else if (awayScore == homeScore)
                        {
                            standing.Drawn++;
                            standing.Points += 1;
                        }
                        else
                        {
                            standing.Lost++;
                        }
                    }
                }

                standings.Add(standing);
            }

            return standings
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalDifference)
                .ThenByDescending(s => s.GoalsFor)
                .ToList();
        }
    }

    public class GroupStanding
    {
        public Team? Team { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }

        public int GoalDifference => GoalsFor - GoalsAgainst;
    }
}
