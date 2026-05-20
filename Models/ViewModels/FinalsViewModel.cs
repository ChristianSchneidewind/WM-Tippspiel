using System.Collections.Generic;
using TippSpiel.Models;

namespace TippSpiel.Models.ViewModels
{
    public class KnockoutRoundViewModel
    {
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<Game> Games { get; set; } = new List<Game>();
    }

    public class FinalsViewModel
    {
        public IReadOnlyList<KnockoutRoundViewModel> Rounds { get; set; } = new List<KnockoutRoundViewModel>();
    }
}
