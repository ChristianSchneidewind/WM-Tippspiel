using System.Collections.Generic;

namespace TippSpiel.Models.ViewModels
{
    public class RankingEntryViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Points { get; set; }
    }

    public class RankingsViewModel
    {
        public IReadOnlyList<RankingEntryViewModel> Entries { get; set; } = new List<RankingEntryViewModel>();
    }
}
