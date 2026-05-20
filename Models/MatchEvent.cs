using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TippSpiel.Models
{
    public class MatchEvent
    {
        public int Id { get; set; }
        
        public int GameId { get; set; }
        
        [ForeignKey("GameId")]
        public Game? Game { get; set; }
        
        public int PlayerId { get; set; }
        
        [ForeignKey("PlayerId")]
        public Player? Player { get; set; }
        
        public int? AssistPlayerId { get; set; }
        
        [ForeignKey("AssistPlayerId")]
        public Player? AssistPlayer { get; set; }
        
        public string EventType { get; set; } = "Goal"; // e.g., Goal, YellowCard
        
        public int Minute { get; set; }
        
        public int? ExternalId { get; set; }
    }
}
