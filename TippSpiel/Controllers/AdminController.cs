using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using TippSpiel.Data;
using TippSpiel.Models;
using TippSpiel.Models.Admin;

namespace TippSpiel.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IGameRepository _repository;
        private readonly AdminOptions _options;

        public AdminController(IGameRepository repository, IOptions<AdminOptions> options)
        {
            _repository = repository;
            _options = options.Value;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new AdminLoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.Equals(model.Username, _options.Username, StringComparison.Ordinal) ||
                !string.Equals(model.Password, _options.Password, StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "Ungültige Zugangsdaten.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                Groups = _repository.Groups,
                Games = _repository.Games
            };

            return View(viewModel);
        }

        public IActionResult CreateGroup()
        {
            return View(new AdminGroupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGroup(AdminGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _repository.AddGroup(model.Name);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult CreateGame()
        {
            PopulateGroups();
            return View(new AdminGameEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGame(AdminGameEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateGroups(model.GroupId);
                return View(model);
            }

            var game = new Game
            {
                HomeTeam = model.HomeTeam,
                AwayTeam = model.AwayTeam,
                KickOff = model.KickOff,
                GroupId = model.GroupId
            };

            _repository.AddGame(game);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult EditGame(int id)
        {
            var game = _repository.GetGame(id);
            if (game == null)
            {
                return NotFound();
            }

            var model = new AdminGameEditViewModel
            {
                Id = game.Id,
                HomeTeam = game.HomeTeam,
                AwayTeam = game.AwayTeam,
                KickOff = game.KickOff,
                GroupId = game.GroupId,
                HomeTeamScore = game.HomeTeamScore,
                AwayTeamScore = game.AwayTeamScore
            };

            PopulateGroups(model.GroupId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditGame(AdminGameEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateGroups(model.GroupId);
                return View(model);
            }

            var game = new Game
            {
                Id = model.Id,
                HomeTeam = model.HomeTeam,
                AwayTeam = model.AwayTeam,
                KickOff = model.KickOff,
                GroupId = model.GroupId,
                HomeTeamScore = model.HomeTeamScore,
                AwayTeamScore = model.AwayTeamScore
            };

            _repository.UpdateGame(game);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Result(int id)
        {
            var game = _repository.GetGame(id);
            if (game == null)
            {
                return NotFound();
            }

            var model = new AdminResultViewModel
            {
                Id = game.Id,
                HomeTeam = game.HomeTeam,
                AwayTeam = game.AwayTeam,
                HomeTeamScore = game.HomeTeamScore,
                AwayTeamScore = game.AwayTeamScore
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Result(AdminResultViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _repository.UpdateResult(model.Id, model.HomeTeamScore, model.AwayTeamScore);
            return RedirectToAction(nameof(Index));
        }

        private void PopulateGroups(int? selectedGroupId = null)
        {
            ViewBag.Groups = new SelectList(_repository.Groups, "Id", "Name", selectedGroupId);
        }
    }
}
