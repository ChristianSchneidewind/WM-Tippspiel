using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace TippSpiel.Models
{
    public class Tipp
    {
        public int Id {get; set;}

        public int GameId {get; set;}
        public Game? Game {get; set;}
        [Required]
        public string UserId {get; set;} = string.Empty;
        [ForeignKey("UserId")]
        public User? User {get; set;}
        [Required]
        [Range(0,100)]
        public int HomeTeamTipp {get; set;}
        [Required]
        [Range(0, 100)]
        public int AwayTeamTipp {get; set;}

        public int points {get; set;}

    }

}