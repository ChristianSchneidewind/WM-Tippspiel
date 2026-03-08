using System.ComponentModel.DataAnnotations;

namespace TippSpiel.Models.Admin
{
    public class AdminGameEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string HomeTeam { get; set; } = string.Empty;

        [Required]
        public string AwayTeam { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset KickOff { get; set; } = DateTimeOffset.Now;

        [Required]
        public int GroupId { get; set; }

        public int? HomeTeamScore { get; set; }
        public int? AwayTeamScore { get; set; }
    }
}
