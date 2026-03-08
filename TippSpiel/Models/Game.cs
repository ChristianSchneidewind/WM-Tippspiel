using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TippSpiel.Models
{
    public class Game
        {
            public int Id{get; set;}
            [Required]
            public string HomeTeam {get; set;} = string.Empty;
            [Required]
            public string AwayTeam {get; set;} = string.Empty;
            public DateTimeOffset KickOff {get; set;}
            public int? HomeTeamScore {get; set;}
            public int? AwayTeamScore{get; set;}
            public int? MatchNumber { get; set; }

            public int GroupId {get; set;}
            public Group? Group {get; set;}
            

        }
}
    
