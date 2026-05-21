using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Models.ViewModels;

namespace TippSpiel.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StatisticsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new StatisticsIndexViewModel();

            // Top-Torschützen
            vm.TopScorers = await _db.MatchEvents
                .Where(e => e.EventType == "Goal")
                .Include(e => e.Player)
                    .ThenInclude(p => p!.Team)
                .GroupBy(e => new
                {
                    e.PlayerId,
                    PlayerName = e.Player!.Name,
                    TeamName = e.Player.Team!.Name
                })
                .Select(g => new PlayerStatisticViewModel
                {
                    PlayerId = g.Key.PlayerId,
                    PlayerName = g.Key.PlayerName,
                    TeamName = g.Key.TeamName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            return View(vm);
            

            // Top-Assists
            vm.TopAssists = await _db.MatchEvents
                .Where(e => e.AssistPlayerId != null)
                .Include(e => e.AssistPlayer)
                    .ThenInclude(p => p!.Team)
                .GroupBy(e => new
                {
                    PlayerId = e.AssistPlayerId!.Value,
                    PlayerName = e.AssistPlayer!.Name,
                    TeamName = e.AssistPlayer.Team!.Name
                })
                .Select(g => new PlayerStatisticViewModel
                {
                    PlayerId = g.Key.PlayerId,
                    PlayerName = g.Key.PlayerName,
                    TeamName = g.Key.TeamName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // Gelbe Karten
            vm.YellowCards = await _db.MatchEvents
                .Where(e => e.EventType == "YellowCard")
                .Include(e => e.Player)
                    .ThenInclude(p => p!.Team)
                .GroupBy(e => new
                {
                    e.PlayerId,
                    PlayerName = e.Player!.Name,
                    TeamName = e.Player.Team!.Name
                })
                .Select(g => new PlayerStatisticViewModel
                {
                    PlayerId = g.Key.PlayerId,
                    PlayerName = g.Key.PlayerName,
                    TeamName = g.Key.TeamName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // Rote Karten
            vm.RedCards = await _db.MatchEvents
                .Where(e => e.EventType == "RedCard")
                .Include(e => e.Player)
                    .ThenInclude(p => p!.Team)
                .GroupBy(e => new
                {
                    e.PlayerId,
                    PlayerName = e.Player!.Name,
                    TeamName = e.Player.Team!.Name
                })
                .Select(g => new PlayerStatisticViewModel
                {
                    PlayerId = g.Key.PlayerId,
                    PlayerName = g.Key.PlayerName,
                    TeamName = g.Key.TeamName,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();
        }


    }
}