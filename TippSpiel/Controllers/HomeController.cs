using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
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
            return View();
        }

        public IActionResult Groups()
        {
            var groups = _repository.Groups
                .OrderBy(group => group.Name)
                .Select(group => new GroupOverviewViewModel
                {
                    GroupId = group.Id,
                    GroupName = group.Name,
                    Teams = group.Games
                        .SelectMany(game => new[] { game.HomeTeam, game.AwayTeam })
                        .Where(team => !string.IsNullOrWhiteSpace(team))
                        .Distinct()
                        .OrderBy(team => team)
                        .ToList(),
                    Games = group.Games
                        .OrderBy(game => game.KickOff)
                        .ToList()
                })
                .ToList();

            return View(groups);
        }

        public IActionResult Schedule()
        {
            var model = new ScheduleViewModel
            {
                Games = _repository.Games
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

            return View(new RankingsViewModel { Entries = entries });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
