using System.ComponentModel.DataAnnotations;

namespace TippSpiel.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? FlagUrl { get; set; }

        public int? ExternalId { get; set; }

        public ICollection<Player> Players { get; set; } = new List<Player>();
    }
}
