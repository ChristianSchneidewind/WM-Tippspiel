using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;

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

            return View(teams);
        }

        public async Task<IActionResult> Details(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }
    }
}