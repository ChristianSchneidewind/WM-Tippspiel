using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TippSpiel.Models;
using TippSpiel.Data;
using TippSpiel.Models.ViewModels;
using TippSpiel.Helpers;

namespace TippSpiel.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public HomeController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // --- STARTSEITE ---
    public async Task<IActionResult> Index()
    {
        var now = DateTimeOffset.Now;
        var allGames = await _db.Games
            .Include(g => g.Group)
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .ToListAsync();

        var upcomingGames = allGames
            .Where(g => g.KickOff > now)
            .OrderBy(g => g.KickOff)
            .Take(3)
            .Select(game => new UpcomingGameViewModel
            {
                Id = game.Id,
                HomeTeam = GameHelper.FixTeamName(game.HomeTeamName),
                AwayTeam = GameHelper.FixTeamName(game.AwayTeamName),
                GroupName = game.Group?.Name ?? "Unbekannt",
                KickOff = game.KickOff,
                Venue = game.Venue
            })
            .ToList();

        var allGroupNames = await _db.Groups.Select(g => g.Name).ToListAsync();

        var realGroupCount = allGroupNames
            .Select(name => name.Trim())
            .Count(name =>
                name.Length == 1 ||
                (name.StartsWith("Gruppe", StringComparison.OrdinalIgnoreCase) && !name.Contains("Finalrunde", StringComparison.OrdinalIgnoreCase)) ||
                (name.StartsWith("Group", StringComparison.OrdinalIgnoreCase) && !name.Contains("Finalrunde", StringComparison.OrdinalIgnoreCase))
            );

        return View(new HomeIndexViewModel
        {
            UpcomingGames = upcomingGames,
            GroupCount = realGroupCount, // Jetzt sollte hier 12 stehen
            GameCount = await _db.Games.CountAsync(),
            TippCount = await _db.Tipps.CountAsync()
        });
    }

    // --- GRUPPENÜBERSICHT & TABELLEN ---
    public async Task<IActionResult> Groups()
    {
        var groupsData = await _db.Groups
            .Include(g => g.Games).ThenInclude(game => game.HomeTeam)
            .Include(g => g.Games).ThenInclude(game => game.AwayTeam)
            .ToListAsync();

        var groupsVm = groupsData
            // 1. FILTER: Nur echte Vorrunden-Gruppen zulassen
            .Where(g =>
                // Fall A: Name ist nur Buchstabe
                (g.Name.Trim().Length == 1 && char.IsLetter(g.Name.Trim()[0])) ||
                // Fall B: Name enthält "Gruppe" oder "Group", aber keine KO-Runden
                ((g.Name.Contains("Gruppe", StringComparison.OrdinalIgnoreCase) || 
                  g.Name.Contains("Group", StringComparison.OrdinalIgnoreCase)) &&
                 !g.Name.Contains("Finalrunde", StringComparison.OrdinalIgnoreCase))
            )
            // 2. SORTIERUNG: Erst nach Länge (A vor Gruppe A), dann alphabetisch
            .OrderBy(g => g.Name.Length)
            .ThenBy(g => g.Name)
            .Select(g => new GroupOverviewViewModel
            {
                GroupId = g.Id,
                GroupName = g.Name,
                Teams = g.Games
                    .SelectMany(x => new[] { x.HomeTeam, x.AwayTeam })
                    .Where(t => t != null)
                    .DistinctBy(t => t!.Id)
                    .OrderBy(t => t!.Name)
                    .Select(t => t!)
                    .ToList(),
                Games = g.Games.OrderBy(x => x.KickOff).ToList()
            })
            .ToList();

        // 3. Tabellen berechnen (nur für die gefilterten Gruppen)
        foreach (var groupVm in groupsVm)
        {
            CalculateTable(groupVm);
        }

        return View(groupsVm);
    }

    // --- FINALRUNDE ---
    public async Task<IActionResult> Finals()
    {
        // Diese Liste MUSS exakt mit den Rückgabewerten deines FootballApiServices übereinstimmen
        var knockoutOrder = new List<string>
    {
        "Sechzehntelfinale",
        "Achtelfinale",
        "Viertelfinale",
        "Halbfinale",
        "Spiel um Platz 3",
        "Finale"
    };

        var roundsData = await _db.Groups
            .Include(g => g.Games).ThenInclude(game => game.HomeTeam)
            .Include(g => g.Games).ThenInclude(game => game.AwayTeam)
            .ToListAsync(); // Wir laden erst alles, um C#-Vergleiche (Trim/Ignore Case) zu nutzen

        var sortedRounds = roundsData
            .Where(g => knockoutOrder.Any(k => k.Equals(g.Name?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .OrderBy(g => knockoutOrder.IndexOf(knockoutOrder.First(k => k.Equals(g.Name?.Trim(), StringComparison.OrdinalIgnoreCase))))
            .Select(g => new KnockoutRoundViewModel
            {
                Name = g.Name.Trim(),
                Games = g.Games.OrderBy(x => x.KickOff).ToList()
            })
            .ToList();

        return View(new FinalsViewModel { Rounds = sortedRounds });
    }

    // --- SPIELPLAN ---
    public async Task<IActionResult> Schedule()
    {
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id;

        var gamesFromDb = await _db.Games
            .Include(g => g.Group)
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .AsNoTracking()
            .ToListAsync();

        if (userId != null)
        {
            var userTips = await _db.Tipps
                .Where(t => t.UserId == userId)
                .ToDictionaryAsync(t => t.GameId);

            foreach (var game in gamesFromDb)
            {
                if (userTips.TryGetValue(game.Id, out var tip))
                {
                    game.UserTipHome = tip.HomeTeamTipp;
                    game.UserTipAway = tip.AwayTeamTipp;
                }
            }
        }

        var sortedGames = gamesFromDb.OrderBy(g => g.KickOff).ToList();
        return View(new ScheduleViewModel { Games = sortedGames });
    }

    // --- RANGLISTE ---
    public async Task<IActionResult> Rankings()
    {
        var entries = await _db.Users
            .Select(u => new RankingEntryViewModel
            {
                UserId = u.Id,
                UserName = u.UserName ?? "Anonym",
                Points = _db.Tipps.Where(t => t.UserId == u.Id).Sum(t => (int?)t.points) ?? 0
            })
            .OrderByDescending(e => e.Points)
            .ToListAsync();

        return View(new RankingsViewModel { Entries = entries });
    }

    // --- HILFSMETHODEN ---

    private static void CalculateTable(GroupOverviewViewModel groupVm)
    {
        var rows = groupVm.Teams
            .Select(team => new TableRowViewModel
            {
                TeamName = GameHelper.FixTeamName(team.Name)
            })
            .ToList();

        foreach (var game in groupVm.Games.Where(g => g.HomeTeamScore.HasValue && g.AwayTeamScore.HasValue))
        {
            // Normalize game team names to the same normalized form as groupVm.Teams
            var homeName = GameHelper.FixTeamName(game.HomeTeamName);
            var awayName = GameHelper.FixTeamName(game.AwayTeamName);

            var home = rows.FirstOrDefault(r => r.TeamName == homeName);
            var away = rows.FirstOrDefault(r => r.TeamName == awayName);

            if (home != null && away != null)
            {
                home.GamesPlayed++; away.GamesPlayed++;
                home.GoalsFor += game.HomeTeamScore!.Value; home.GoalsAgainst += game.AwayTeamScore!.Value;
                away.GoalsFor += game.AwayTeamScore!.Value; away.GoalsAgainst += game.HomeTeamScore!.Value;

                if (game.HomeTeamScore > game.AwayTeamScore) { home.Wins++; home.Points += 3; away.Losses++; }
                else if (game.HomeTeamScore < game.AwayTeamScore) { away.Wins++; away.Points += 3; home.Losses++; }
                else { home.Draws++; away.Draws++; home.Points += 1; away.Points += 1; }
            }
        }

        groupVm.TableRows = rows
            .OrderByDescending(r => r.Points)
            .ThenByDescending(r => r.GoalDifference)
            .ThenByDescending(r => r.GoalsFor)
            .ToList();

        for (int i = 0; i < groupVm.TableRows.Count; i++) groupVm.TableRows[i].Position = i + 1;
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}