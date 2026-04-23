using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace TippSpiel.Models
{
    public class Game
        {
            public int Id{get; set;}
        public int HomeTeamId { get; set; }
        [ForeignKey("HomeTeamId")]
        public Team? HomeTeam { get; set; }

        public int AwayTeamId { get; set; }
        [ForeignKey("AwayTeamId")]
        public Team? AwayTeam { get; set; }

        public DateTimeOffset KickOff { get; set; }
        public int? HomeTeamScore { get; set; }
        public int? AwayTeamScore { get; set; }
            public int? MatchNumber { get; set; }

            public int GroupId {get; set;}
            public Group? Group {get; set;}

        [NotMapped]
        public string HomeTeamName => HomeTeam?.Name ?? "TBD";
        [NotMapped]
        public string AwayTeamName => AwayTeam?.Name ?? "TBD";

        public int? ExternalId { get; set; }
    }
}
    
