namespace TippSpiel.Models.ViewModels
{
    public class PlayerStatsViewModel
    {
        public int PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public int Appearances { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
    }
}