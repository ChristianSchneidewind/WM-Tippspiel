namespace TippSpiel.Models.ViewModels
{
    public class TeamDetailsViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? BackUrl { get; set; }
        public List<PlayerStatsViewModel> Players { get; set; } = new();
    }
}