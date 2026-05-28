namespace TippSpiel.Models.ViewModels
{
    public class TeamDetailsViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public List<PlayerStatsViewModel> Players { get; set; } = new();
    }
}