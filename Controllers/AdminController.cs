using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TippSpiel.Data;
using TippSpiel.Models;
using TippSpiel.Models.Admin;
using TippSpiel.Services;

namespace TippSpiel.Controllers;

// [Authorize(Roles = "Admin")] // Später wieder aktivieren!
public class AdminController : Controller
{
    private readonly IGameRepository _repository;

    public AdminController(IGameRepository repository)
    {
        _repository = repository;
    }

    // --- Der Rest bleibt weitgehend gleich, aber achte auf die Repository-Aufrufe ---

    public IActionResult Index()
    {
        var viewModel = new AdminDashboardViewModel
        {
            Groups = _repository.Groups,
            Games = _repository.Games.OrderBy(g => g.KickOff).ToList()
        };

        return View(viewModel);
    }

    // ... (CreateGroup, CreateGame etc. wie gehabt) ...

    public IActionResult EditGame(int id)
    {
        TippSpiel.Models.Game? game = _repository.GetGame(id);
        if (game == null) return NotFound();

        var model = new AdminGameEditViewModel
        {
            Id = game.Id,
            HomeTeam = game.HomeTeam?.Name ?? game.HomeTeamName ?? string.Empty,
            AwayTeam = game.AwayTeam?.Name ?? game.AwayTeamName ?? string.Empty,
            KickOff = game.KickOff,
            GroupId = game.GroupId,
            HomeTeamScore = game.HomeTeamScore,
            AwayTeamScore = game.AwayTeamScore,
            Venue = game.Venue
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

        // Falls du manuell Teams editierst
        TippSpiel.Models.Team homeTeam = _repository.GetOrCreateTeam(model.HomeTeam);
        TippSpiel.Models.Team awayTeam = _repository.GetOrCreateTeam(model.AwayTeam);

        var game = _repository.GetGame(model.Id);
        if (game != null)
        {
            game.HomeTeamId = homeTeam.Id;
            game.AwayTeamId = awayTeam.Id;
            game.KickOff = model.KickOff;
            game.GroupId = model.GroupId;
            game.HomeTeamScore = model.HomeTeamScore;
            game.AwayTeamScore = model.AwayTeamScore;
            game.Venue = model.Venue;

            _repository.UpdateGame(game);
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Result(int id)
    {
        var game = _repository.GetGame(id);
        if (game == null)
            return NotFound();

        var model = new AdminResultViewModel
        {
            Id = game.Id,
            HomeTeam = game.HomeTeam?.Name ?? game.HomeTeamName ?? "Home",
            AwayTeam = game.AwayTeam?.Name ?? game.AwayTeamName ?? "Away",
            HomeTeamScore = game.HomeTeamScore,
            AwayTeamScore = game.AwayTeamScore
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Result(AdminResultViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var game = _repository.GetGame(model.Id);
        if (game == null)
            return NotFound();

        // Aktualisiere das Spiel mit den Ergebnissen und berechne Tipps neu
        _repository.UpdateResult(model.Id, model.HomeTeamScore, model.AwayTeamScore);

        return RedirectToAction(nameof(Index));
    }

    // ... (Restliche Methoden) ...

    private void PopulateGroups(int? selectedGroupId = null)
    {
        ViewBag.Groups = new SelectList(_repository.Groups.OrderBy(g => g.Name), "Id", "Name", selectedGroupId);
    }
}