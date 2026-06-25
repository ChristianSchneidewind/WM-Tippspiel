namespace TippSpiel.Data.FifaDtos;

public class FifaMatchDto
{
    public string IdMatch { get; set; } = "";
    public int MatchNumber { get; set; }

    public string Date { get; set; } = "";

    public List<FifaDescription> GroupName { get; set; } = [];

    public FifaMatchTeam Home { get; set; } = new();
    public FifaMatchTeam Away { get; set; } = new();

    public FifaStadium Stadium { get; set; } = new();

    public FifaProperties? Properties { get; set; }
}