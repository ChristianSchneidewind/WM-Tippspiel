using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Helpers;
using TippSpiel.Models;

namespace TippSpiel.Controllers
{
    [Authorize]
    public class TipsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public TipsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> SaveInlineTip([FromBody] TipRequest request)
        {
            if (request == null) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var game = await _context.Games.FindAsync(request.GameId);
            if (game == null) return NotFound();

            // Prüfen, ob das Spiel bereits begonnen hat
            if (DateTimeOffset.UtcNow >= game.KickOff)
            {
                return BadRequest("Das Spiel hat bereits begonnen.");
            }

            var tip = await _context.Tipps
                .FirstOrDefaultAsync(t => t.GameId == request.GameId && t.UserId == user.Id);

            if (tip == null)
            {
                tip = new Tipp
                {
                    GameId = request.GameId,
                    UserId = user.Id,
                    HomeTeamTipp = request.HomeScore,
                    AwayTeamTipp = request.AwayScore
                };
                _context.Tipps.Add(tip);
            }
            else
            {
                tip.HomeTeamTipp = request.HomeScore;
                tip.AwayTeamTipp = request.AwayScore;
            }

            tip.points = TippPointsHelper.CalculatePoints(
                tip.HomeTeamTipp,
                tip.AwayTeamTipp,
                game.HomeTeamScore,
                game.AwayTeamScore
            );

            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class TipRequest
    {
        public int GameId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}
