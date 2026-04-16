using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;
using TippSpiel.Data;
using TippSpiel.Models.ViewModels;

namespace TippSpiel.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    // --- STARTSEITE ---
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var now = DateTimeOffset.Now;

        var allGames = await _db.Games
            .Include(g => g.Group)
            .ToListAsync();

        var upcomingGames = allGames
            .Where(g => g.KickOff > now)
            .OrderBy(g => g.KickOff)
            .Take(3)
            .Select(game => new UpcomingGameViewModel
            {
                Id = game.Id,
                HomeTeam = FixTeamName(game.HomeTeam),
                AwayTeam = FixTeamName(game.AwayTeam),
                GroupName = game.Group?.Name ?? "Unbekannt",
                KickOff = game.KickOff
            })
            .ToList();

        return View(new HomeIndexViewModel
        {
            UpcomingGames = upcomingGames,
            GroupCount = await _db.Groups.CountAsync(),
            GameCount = allGames.Count,
            TippCount = await _db.Tipps.CountAsync()
        });
    }

    // --- GRUPPENÜBERSICHT & TABELLEN ---
    public async Task<IActionResult> Groups()
    {
        var groupsData = await _db.Groups
            .Include(g => g.Games)
            .ToListAsync();

        // Namen global für diese Anfrage waschen
        foreach (var group in groupsData)
        {
            foreach (var game in group.Games)
            {
                game.HomeTeam = FixTeamName(game.HomeTeam);
                game.AwayTeam = FixTeamName(game.AwayTeam);
            }
        }

        var groupsVm = groupsData
            .Where(g => !g.Name.Equals("Finalrunde", StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Name)
            .Select(g => new GroupOverviewViewModel
            {
                GroupId = g.Id,
                GroupName = g.Name,
                Teams = g.Games
                    .SelectMany(x => new[] { x.HomeTeam, x.AwayTeam })
                    .Distinct()
                    .Where(t => !string.IsNullOrEmpty(t))
                    .OrderBy(t => t)
                    .ToList(),
                Games = g.Games.OrderBy(x => x.KickOff).ToList()
            })
            .ToList();

        foreach (var groupVm in groupsVm)
        {
            CalculateTable(groupVm);
        }

        return View(groupsVm);
    }

    // --- FINALRUNDE ---
    public async Task<IActionResult> Finals()
    {
        var allGames = await _db.Games
            .Include(g => g.Group)
            .ToListAsync();

        foreach (var game in allGames)
        {
            game.HomeTeam = FixTeamName(game.HomeTeam);
            game.AwayTeam = FixTeamName(game.AwayTeam);
        }

        var finalGames = allGames
            .Where(g => g.Group != null && g.Group.Name.Equals("Finalrunde", StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.MatchNumber ?? 999)
            .ToList();

        var model = new FinalsViewModel
        {
            Rounds = new List<KnockoutRoundViewModel>
            {
                new()
                {
                    Name = "Sechzehntelfinale",
                    Games = finalGames.Where(g => g.MatchNumber is >= 73 and <= 88).ToList()
                },
                new()
                {
                    Name = "Achtelfinale",
                    Games = finalGames.Where(g => g.MatchNumber is >= 89 and <= 96).ToList()
                },
                new()
                {
                    Name = "Viertelfinale",
                    Games = finalGames.Where(g => g.MatchNumber is >= 97 and <= 100).ToList()
                },
                new()
                {
                    Name = "Halbfinale",
                    Games = finalGames.Where(g => g.MatchNumber is >= 101 and <= 102).ToList()
                },
                new()
                {
                    Name = "Spiel um Platz 3",
                    Games = finalGames.Where(g => g.MatchNumber == 103).ToList()
                },
                new()
                {
                    Name = "Finale",
                    Games = finalGames.Where(g => g.MatchNumber == 104).ToList()
                }
            }
            .Where(r => r.Games.Any())
            .ToList()
        };

        return View(model);
    }

    // --- SPIELPLAN ---
    public async Task<IActionResult> Schedule()
    {
        var gamesFromDb = await _db.Games
            .Include(g => g.Group)
            .ToListAsync();

        foreach (var game in gamesFromDb)
        {
            game.HomeTeam = FixTeamName(game.HomeTeam);
            game.AwayTeam = FixTeamName(game.AwayTeam);
        }

        var sortedGames = gamesFromDb
            .OrderBy(g => g.KickOff)
            .ToList();

        return View(new ScheduleViewModel
        {
            Games = sortedGames
        });
    }

    // --- RANGLISTE ---
    public async Task<IActionResult> Rankings()
    {
        var entries = await _db.Users
            .Select(u => new RankingEntryViewModel
            {
                UserId = u.Id,
                UserName = u.UserName ?? "Anonym",
                Points = _db.Tipps
                    .Where(t => t.UserId == u.Id)
                    .Sum(t => (int?)t.points) ?? 0
            })
            .OrderByDescending(e => e.Points)
            .ToListAsync();

        return View(new RankingsViewModel
        {
            Entries = entries
        });
    }

    // --- HILFSMETHODEN ---
    private static void CalculateTable(GroupOverviewViewModel groupVm)
    {
        var rows = groupVm.Teams
            .Select(name => new TableRowViewModel { TeamName = name })
            .ToList();

        foreach (var game in groupVm.Games.Where(g => g.HomeTeamScore.HasValue && g.AwayTeamScore.HasValue))
        {
            var home = rows.FirstOrDefault(r => r.TeamName == game.HomeTeam);
            var away = rows.FirstOrDefault(r => r.TeamName == game.AwayTeam);

            if (home != null && away != null)
            {
                home.GamesPlayed++;
                away.GamesPlayed++;

                home.GoalsFor += game.HomeTeamScore!.Value;
                home.GoalsAgainst += game.AwayTeamScore!.Value;

                away.GoalsFor += game.AwayTeamScore!.Value;
                away.GoalsAgainst += game.HomeTeamScore!.Value;

                if (game.HomeTeamScore > game.AwayTeamScore)
                {
                    home.Wins++;
                    home.Points += 3;
                    away.Losses++;
                }
                else if (game.HomeTeamScore < game.AwayTeamScore)
                {
                    away.Wins++;
                    away.Points += 3;
                    home.Losses++;
                }
                else
                {
                    home.Draws++;
                    away.Draws++;
                    home.Points += 1;
                    away.Points += 1;
                }
            }
        }

        groupVm.TableRows = rows
            .OrderByDescending(r => r.Points)
            .ThenByDescending(r => r.GoalDifference)
            .ThenByDescending(r => r.GoalsFor)
            .ToList();

        for (int i = 0; i < groupVm.TableRows.Count; i++)
            groupVm.TableRows[i].Position = i + 1;
    }

    private string FixTeamName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return name.Trim() switch
        {
            "Saudiarabien" => "Saudi-Arabien",
            "IR Iran" => "Iran",
            "Curacao" => "Curaçao",
            "Republik Korea" => "Südkorea",
            _ => name
        };
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}