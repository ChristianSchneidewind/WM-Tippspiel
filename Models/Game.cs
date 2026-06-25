using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TippSpiel.Models
{
    public class Game
    {
        public int Id { get; set; }

        // Das ? macht die ID optional (NULL erlaubt)
        public int? HomeTeamId { get; set; }
        [ForeignKey("HomeTeamId")]
        public Team? HomeTeam { get; set; }

        // Das ? macht die ID optional (NULL erlaubt)
        public int? AwayTeamId { get; set; }
        [ForeignKey("AwayTeamId")]
        public Team? AwayTeam { get; set; }

        public DateTimeOffset KickOff { get; set; }

        public string? IfesId { get; set; }
        public int? HomeTeamScore { get; set; }
        public int? AwayTeamScore { get; set; }

        // NEU: Für die Entscheidung im Elfmeterschießen in der K.o.-Runde
        public int? HomeTeamPenaltyScore { get; set; }
        public int? AwayTeamPenaltyScore { get; set; }

        public int? MatchNumber { get; set; }
        public string? Venue { get; set; }

        public int GroupId { get; set; }
        public Group? Group { get; set; }

        [NotMapped]
        public string HomeTeamName => HomeTeam?.Name ?? ""; // Hier "" statt "TBD", damit der Helper greifen kann

        [NotMapped]
        public string AwayTeamName => AwayTeam?.Name ?? "";

        public int? ExternalId { get; set; }

        [NotMapped]
        public int? UserTipHome { get; set; }

        [NotMapped]
        public int? UserTipAway { get; set; }
    }
}