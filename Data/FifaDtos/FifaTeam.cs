public class FifaTeamResponse
{
    public List<FifaTeam> Teams { get; set; } = [];
}

public class FifaTeam
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string TeamFlag { get; set; } = "";
    public string TeamPageUrl { get; set; } = "";
}