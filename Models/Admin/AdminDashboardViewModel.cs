using TippSpiel.Models;

namespace TippSpiel.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public IEnumerable<Group> Groups { get; set; } = Array.Empty<Group>();
        public IEnumerable<Game> Games { get; set; } = Array.Empty<Game>();
    }
}
