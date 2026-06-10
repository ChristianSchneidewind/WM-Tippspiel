using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TippSpiel.Models;
using TippSpiel.Data;
using TippSpiel.Models.ViewModels;
using TippSpiel.Helpers;
using TippSpiel.Services;

namespace TippSpiel.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly GroupStandingsService _groupStandingsService;
    private readonly KnockoutBracketService _knockoutBracketService;

    public HomeController(ApplicationDbContext db, UserManager<User> userManager, GroupStandingsService groupStandingsService, KnockoutBracketService knockoutBracketService)
    {
        _db = db;
        _userManager = userManager;
        _groupStandingsService = groupStandingsService;
        _knockoutBracketService = knockoutBracketService;
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
            var standings = _groupStandingsService.CalculateGroupStandings(groupVm.GroupId);

            groupVm.TableRows = standings
                .Select((standing, index) => new TableRowViewModel
                {
                    Position = index + 1,
                    TeamName = GameHelper.FixTeamName(standing.Team?.Name ?? string.Empty),
                    GamesPlayed = standing.Played,
                    Wins = standing.Won,
                    Draws = standing.Drawn,
                    Losses = standing.Lost,
                    GoalsFor = standing.GoalsFor,
                    GoalsAgainst = standing.GoalsAgainst,
                    Points = standing.Points
                })
                .ToList();
        }

        return View(groupsVm);
    }

    // --- FINALRUNDE ---
    public async Task<IActionResult> Finals()
    {
        var rounds = await _knockoutBracketService.BuildAsync(_db);
        return View(new FinalsViewModel { Rounds = rounds });
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
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}