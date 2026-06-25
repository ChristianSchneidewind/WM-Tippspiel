using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Models.ViewModels;

namespace TippSpiel.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var teams = await _context.Teams
                .Include(t => t.Players)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var playerIds = teams
                .SelectMany(t => t.Players)
                .Select(p => p.Id)
                .ToList();

            var events = playerIds.Count == 0
                ? new List<TippSpiel.Models.MatchEvent>()
                : await _context.MatchEvents
                    .Where(e =>
                        playerIds.Contains(e.PlayerId) ||
                        (e.AssistPlayerId.HasValue && playerIds.Contains(e.AssistPlayerId.Value)))
                    .ToListAsync();

            var viewModel = teams.Select(team => new TeamDetailsViewModel
            {
                TeamId = team.Id,
                TeamName = team.Name,
                Players = team.Players
                    .OrderBy(p => p.Position)
                    .ThenBy(p => p.Name)
                    .Select(p => new PlayerStatsViewModel
                    {
                        PlayerId = p.Id,
                        Name = p.Name,
                        Position = p.Position,
                        Appearances = p.Appearances,
                        Goals = events.Count(e => e.PlayerId == p.Id && e.EventType == "Goal"),
                        Assists = events.Count(e => e.PlayerId == p.Id && e.EventType == "Assist"),
                        YellowCards = events.Count(e => e.PlayerId == p.Id && e.EventType == "YellowCard"),
                        RedCards = events.Count(e => e.PlayerId == p.Id && e.EventType == "RedCard")
                    })
                    .ToList()
            }).ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id, string? source = null)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            var playerIds = team.Players.Select(p => p.Id).ToList();

            var events = await _context.MatchEvents
                .Where(e =>
                    playerIds.Contains(e.PlayerId) ||
                    (e.AssistPlayerId.HasValue && playerIds.Contains(e.AssistPlayerId.Value)))
                .ToListAsync();

            var viewModel = new TeamDetailsViewModel
            {
                TeamId = team.Id,
                TeamName = team.Name,
                BackUrl = string.Equals(source, "groups", StringComparison.OrdinalIgnoreCase)
                    ? Url.Action("Groups", "Home")
                    : Url.Action("Index", "Teams"),
                Players = team.Players
                    .OrderBy(p => p.Name)
                    .Select(p => new PlayerStatsViewModel
                    {
                        PlayerId = p.Id,
                        Name = p.Name,
                        Position = p.Position,
                        Appearances = p.Appearances,
                        Goals = events.Count(e => e.PlayerId == p.Id && e.EventType == "Goal"),
                        Assists = events.Count(e => e.PlayerId == p.Id && e.EventType == "Assist"),
                        YellowCards = events.Count(e => e.PlayerId == p.Id && e.EventType == "YellowCard"),
                        RedCards = events.Count(e => e.PlayerId == p.Id && e.EventType == "RedCard")
                    })
                    .ToList()
            };

            return View(viewModel);
        }
    }
}