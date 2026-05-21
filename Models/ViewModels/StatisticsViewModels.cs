namespace TippSpiel.Models.ViewModels
{
    public class StatisticsIndexViewModel
    {
        public List<PlayerStatisticViewModel> TopScorers { get; set; } = new();
        public List<PlayerStatisticViewModel> TopAssists { get; set; } = new();
        public List<PlayerStatisticViewModel> YellowCards { get; set; } = new();
        public List<PlayerStatisticViewModel> RedCards { get; set; } = new();
        public List<TeamStatisticViewModel> FairestTeams { get; set; } = new();
        public List<TeamStatisticViewModel> UnfairestTeams { get; set; } = new();

        public List<TeamStatisticViewModel> TeamStatistics { get; set; } = new();
    }

    public class PlayerStatisticViewModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TeamStatisticViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;

        public int Goals { get; set; }
        public int Assists { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
        public int FairplayPoints { get; set; }
    }
}
