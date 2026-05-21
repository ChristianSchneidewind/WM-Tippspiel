using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Helpers;
using TippSpiel.Models;
using TippSpiel.Models.ViewModels;

namespace TippSpiel.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            return await BuildProfileView(currentUser.Id);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            return await BuildProfileView(id);
        }

        private async Task<IActionResult> BuildProfileView(string profileUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            var profileUser = await _userManager.FindByIdAsync(profileUserId);

            if (profileUser == null)
            {
                return NotFound();
            }

            var ranking = await _db.Users
                .Select(u => new
                {
                    UserId = u.Id,
                    Points = _db.Tipps
                        .Where(t => t.UserId == u.Id)
                        .Sum(t => (int?)t.points) ?? 0
                })
                .OrderByDescending(x => x.Points)
                .ToListAsync();

            var userRankingEntry = ranking.FirstOrDefault(x => x.UserId == profileUser.Id);

            var rank = userRankingEntry == null
                ? 0
                : ranking.FindIndex(x => x.UserId == profileUser.Id) + 1;

            var totalPoints = userRankingEntry?.Points ?? 0;

            var dbTips = await _db.Tipps
                .Where(t => t.UserId == profileUser.Id)
                .Include(t => t.Game)
                    .ThenInclude(g => g!.HomeTeam)
                .Include(t => t.Game)
                    .ThenInclude(g => g!.AwayTeam)
                .ToListAsync();

            dbTips = dbTips
                .OrderBy(t => t.Game!.KickOff)
                .ToList();

            var isOwnProfile = currentUser.Id == profileUser.Id;

            var tips = dbTips
                .Select(t => new UserTipProfileViewModel
                {
                    TipId = t.Id,
                    GameId = t.GameId,
                    KickOff = t.Game!.KickOff,
                    HomeTeam = GameHelper.FixTeamName(t.Game.HomeTeamName),
                    AwayTeam = GameHelper.FixTeamName(t.Game.AwayTeamName),
                    HomeTeamTipp = t.HomeTeamTipp,
                    AwayTeamTipp = t.AwayTeamTipp,
                    HomeTeamScore = t.Game.HomeTeamScore,
                    AwayTeamScore = t.Game.AwayTeamScore,
                    Points = t.points,

                    // Bearbeiten nur im eigenen Profil und nur vor Anpfiff
                    CanEdit = isOwnProfile && DateTimeOffset.UtcNow < t.Game.KickOff
                })
                .ToList();

            var vm = new UserProfileViewModel
            {
                UserId = profileUser.Id,
                UserName = profileUser.UserName ?? "Unbekannt",
                TotalPoints = totalPoints,
                Rank = rank,
                IsOwnProfile = isOwnProfile,
                Tips = tips
            };

            return View("Index", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTip(int tipId, int homeTeamTipp, int awayTeamTipp)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            var tip = await _db.Tipps
                .Include(t => t.Game)
                .FirstOrDefaultAsync(t => t.Id == tipId && t.UserId == user.Id);

            if (tip == null)
            {
                return NotFound();
            }

            if (tip.Game == null)
            {
                return BadRequest();
            }

            if (DateTimeOffset.UtcNow >= tip.Game.KickOff)
            {
                TempData["Error"] = "Dieser Tipp kann nicht mehr geändert werden, weil das Spiel bereits begonnen hat.";
                return RedirectToAction(nameof(Index));
            }

            if (homeTeamTipp < 0 || homeTeamTipp > 100 || awayTeamTipp < 0 || awayTeamTipp > 100)
            {
                TempData["Error"] = "Bitte gib gültige Tore zwischen 0 und 100 ein.";
                return RedirectToAction(nameof(Index));
            }

            tip.HomeTeamTipp = homeTeamTipp;
            tip.AwayTeamTipp = awayTeamTipp;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Tipp wurde erfolgreich aktualisiert.";
            return RedirectToAction(nameof(Index));
        }
    }
}