using System;
using System.Collections.Generic;

namespace TippSpiel.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public int GroupCount { get; set; }
        public int GameCount { get; set; }
        public int TippCount { get; set; }

        // WICHTIG: Die Liste muss diesen Namen haben
        public List<UpcomingGameViewModel> UpcomingGames { get; set; } = new();
    }

    public class UpcomingGameViewModel
    {
        public int Id { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public DateTimeOffset KickOff { get; set; }
    }
}