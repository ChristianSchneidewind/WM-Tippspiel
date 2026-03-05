using  System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TippSpiel.Models
{
    public class Group
    {
        public int Id{get; set;}
        [Required]
        public string Name {get; set;} = string.Empty;
        public ICollection<Match> Matches {get; set;} = new List<Match>();
    }
}