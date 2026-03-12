using System.Diagnostics;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;
using TippSpiel.Data;
using TippSpiel.Models.ViewModels;

namespace TippSpiel.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGameRepository _repository;
        private readonly ApplicationDbContext _db;

        public HomeController(IGameRepository repository, ApplicationDbContext db)
        {
            _repository = repository;
            _db = db;
        }

        public IActionResult Index()
        {
            var nextGame = _db.Games
                .Include(game => game.Group)
                .AsEnumerable()
                .OrderBy(game => game.KickOff)
                .Select(game => new NextGameViewModel
                {
                    HomeTeam = game.HomeTeam,
                    AwayTeam = game.AwayTeam,
                    GroupName = game.Group != null ? game.Group.Name : string.Empty,
                    KickOff = game.KickOff
                })
                .FirstOrDefault();

            var model = new HomeIndexViewModel
            {
                GroupCount = _db.Groups.Count(),
                GameCount = _db.Games.Count(),
                TippCount = _db.Tipps.Count(),
                NextGame = nextGame
            };

            return View(model);
        }

        public IActionResult Groups()
        {
            var groups = _repository.Groups
                .Where(group => !string.Equals(group.Name, "Finalrunde", StringComparison.OrdinalIgnoreCase))
                .OrderBy(group => group.Name)
                .AsEnumerable()
                .Select(group =>
                {
                    var games = group.Games
                        .OrderBy(game => game.KickOff)
                        .ToList();

                    var teams = games
                        .SelectMany(game => new[] { game.HomeTeam, game.AwayTeam })
                        .Where(team => !string.IsNullOrWhiteSpace(team))
                        .Distinct()
                        .OrderBy(team => team)
                        .ToList();

                    var standings = teams.ToDictionary(
                        team => team,
                        team => new GroupTableRowViewModel
                        {
                            TeamName = team
                        });

                    var finishedGames = games
                        .Where(game => game.HomeTeamScore.HasValue && game.AwayTeamScore.HasValue)
                        .ToList();

                    foreach (var game in finishedGames)
                    {
                        var homeTeam = game.HomeTeam;
                        var awayTeam = game.AwayTeam;

                        if (!standings.ContainsKey(homeTeam) || !standings.ContainsKey(awayTeam))
                        {
                            continue;
                        }

                        var home = standings[homeTeam];
                        var away = standings[awayTeam];

                        var homeGoals = game.HomeTeamScore!.Value;
                        var awayGoals = game.AwayTeamScore!.Value;

                        home.GamesPlayed++;
                        away.GamesPlayed++;

                        home.GoalsFor += homeGoals;
                        home.GoalsAgainst += awayGoals;

                        away.GoalsFor += awayGoals;
                        away.GoalsAgainst += homeGoals;

                        if (homeGoals > awayGoals)
                        {
                            home.Wins++;
                            away.Losses++;
                            home.Points += 3;
                        }
                        else if (homeGoals < awayGoals)
                        {
                            away.Wins++;
                            home.Losses++;
                            away.Points += 3;
                        }
                        else
                        {
                            home.Draws++;
                            away.Draws++;
                            home.Points += 1;
                            away.Points += 1;
                        }
                    }

                    var tableRows = standings.Values
                        .OrderByDescending(row => row.Points)
                        .ThenByDescending(row => row.GoalDifference)
                        .ThenByDescending(row => row.GoalsFor)
                        .ThenBy(row => row.TeamName)
                        .ToList();

                    for (int i = 0; i < tableRows.Count; i++)
                    {
                        tableRows[i].Position = i + 1;
                    }

                    return new GroupOverviewViewModel
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        Teams = teams,
                        Games = games,
                        TableRows = tableRows
                    };
                })
                .ToList();

            return View(groups);
        }

        public IActionResult Finals()
        {
            var finalStageGames = _repository.Games
                .Where(game => game.Group != null && string.Equals(game.Group.Name, "Finalrunde", StringComparison.OrdinalIgnoreCase))
                .AsEnumerable()
                .OrderBy(game => game.MatchNumber ?? int.MaxValue)
                .ThenBy(game => game.KickOff)
                .ToList();

            var rounds = new List<KnockoutRoundViewModel>
            {
                BuildRound("Sechzehntelfinale", finalStageGames, 73, 88),
                BuildRound("Achtelfinale", finalStageGames, 89, 96),
                BuildRound("Viertelfinale", finalStageGames, 97, 100),
                BuildRound("Halbfinale", finalStageGames, 101, 102),
                BuildRound("Spiel um Platz 3", finalStageGames, 103, 103),
                BuildRound("Finale", finalStageGames, 104, 104)
            };

            var model = new FinalsViewModel
            {
                Rounds = rounds.Where(round => round.Games.Count > 0).ToList()
            };

            return View(model);
        }

        private static KnockoutRoundViewModel BuildRound(string name, List<Game> games, int start, int end)
        {
            var roundGames = games
                .Where(game => game.MatchNumber.HasValue && game.MatchNumber.Value >= start && game.MatchNumber.Value <= end)
                .OrderBy(game => game.MatchNumber)
                .ThenBy(game => game.KickOff)
                .ToList();

            return new KnockoutRoundViewModel
            {
                Name = name,
                Games = roundGames
            };
        }

        public IActionResult Schedule()
        {
            var model = new ScheduleViewModel
            {
                Games = _repository.Games
                    .AsEnumerable()
                    .OrderBy(game => game.KickOff)
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Rankings()
        {
            var entries = _db.Users
                .Select(user => new RankingEntryViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? user.Email ?? "Unbekannt",
                    Points = _db.Tipps
                        .Where(tipp => tipp.UserId == user.Id)
                        .Sum(tipp => (int?)tipp.points) ?? 0
                })
                .OrderByDescending(entry => entry.Points)
                .ThenBy(entry => entry.UserName)
                .ToList();

            return View(new RankingsViewModel
            {
                Entries = entries
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}