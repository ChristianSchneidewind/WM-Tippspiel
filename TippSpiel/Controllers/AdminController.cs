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

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IGameRepository _repository;
    private readonly FootballApiService _apiService;

    public AdminController(IGameRepository repository, FootballApiService apiService)
    {
        _repository = repository;
        _apiService = apiService;
    }

    [HttpPost]
    public async Task<IActionResult> ImportSchedule()
    {
        await _apiService.InitialSeed();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SyncResults()
    {
        await _apiService.SyncMatchResults();
        return RedirectToAction(nameof(Index));
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

        TippSpiel.Models.Team homeTeam = _repository.GetOrCreateTeam(model.HomeTeam);
        TippSpiel.Models.Team awayTeam = _repository.GetOrCreateTeam(model.AwayTeam);

        var game = new TippSpiel.Models.Game
        {
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            KickOff = model.KickOff,
            GroupId = model.GroupId
        };

        _repository.AddGame(game);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult EditGame(int id)
    {
        TippSpiel.Models.Game? game = _repository.GetGame(id);
        if (game == null)
        {
            return NotFound();
        }

        var model = new AdminGameEditViewModel
        {
            Id = game.Id,
            HomeTeam = game.HomeTeam?.Name ?? string.Empty,
            AwayTeam = game.AwayTeam?.Name ?? string.Empty,
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

        TippSpiel.Models.Team homeTeam = _repository.GetOrCreateTeam(model.HomeTeam);
        TippSpiel.Models.Team awayTeam = _repository.GetOrCreateTeam(model.AwayTeam);

        var game = new TippSpiel.Models.Game
        {
            Id = model.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
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
        TippSpiel.Models.Game? game = _repository.GetGame(id);
        if (game == null)
        {
            return NotFound();
        }

        var model = new AdminResultViewModel
        {
            Id = game.Id,
            HomeTeam = game.HomeTeam?.Name ?? string.Empty,
            AwayTeam = game.AwayTeam?.Name ?? string.Empty,
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
