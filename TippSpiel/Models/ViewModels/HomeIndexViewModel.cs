using System;

namespace TippSpiel.Models.ViewModels
{
    public class NextGameViewModel
    {
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public DateTimeOffset KickOff { get; set; }
    }

    public class HomeIndexViewModel
    {
        public int GroupCount { get; set; }
        public int GameCount { get; set; }
        public int TippCount { get; set; }
        public NextGameViewModel? NextGame { get; set; }
    }
}
