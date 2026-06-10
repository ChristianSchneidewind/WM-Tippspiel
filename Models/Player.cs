using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TippSpiel.Models
{
    public class Player
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public int Appearances{ get; set;  }

        public int TeamId { get; set; }
        
        [ForeignKey("TeamId")]
        public Team? Team { get; set; }
        
        public string? ExternalId { get; set; }
    }
}
