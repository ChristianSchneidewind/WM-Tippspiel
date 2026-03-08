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
    }
}
