using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TippSpiel.Models
{
    public class Player
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public int TeamId { get; set; }
        
        [ForeignKey("TeamId")]
        public Team? Team { get; set; }
        
        public int? ExternalId { get; set; }
    }
}
