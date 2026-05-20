using System.Collections.Generic;
using TippSpiel.Models;

namespace TippSpiel.Models.ViewModels
{
    public class GroupOverviewViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public IReadOnlyList<string> Teams { get; set; } = new List<string>();
        public IReadOnlyList<Game> Games { get; set; } = new List<Game>();

        // NEU: Hier fügen wir die Tabellenzeilen hinzu
        public List<TableRowViewModel> TableRows { get; set; } = new List<TableRowViewModel>();
    }

    // Diese Klasse definiert, was in einer Tabellenzeile steht
    public class TableRowViewModel
    {
        public int Position { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Points { get; set; }
    }
}