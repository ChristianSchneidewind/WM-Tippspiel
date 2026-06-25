namespace TippSpiel.Data.FifaDtos;

public class FifaTimelineEvent
{
    public string? EventId { get; set; }
    public string? IdPlayer { get; set; }
    public string? IdTeam { get; set; }
    public string? MatchMinute { get; set; }
    public int Type { get; set; }
}