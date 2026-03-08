using System.Collections.Generic;
using TippSpiel.Models;

namespace TippSpiel.Models.ViewModels
{
    public class ScheduleViewModel
    {
        public IReadOnlyList<Game> Games { get; set; } = new List<Game>();
    }
}
