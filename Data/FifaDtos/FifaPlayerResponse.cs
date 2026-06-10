namespace TippSpiel.Data.FifaDtos;

public class FifaPlayerResponse
{
    public List<FifaPlayer> Players { get; set; } = [];
}

public class FifaPlayer
{
    public string IdPlayer { get; set; } = "";

    public List<FifaLocalizedText>? PlayerName { get; set; }

    public int? JerseyNum { get; set; }

    public int? Position { get; set; }

    public double? Height { get; set; }

    public double? Weight { get; set; }

    public int? MatchesPlayed { get; set; }
    public List<FifaLocalizedText>? PositionLocalized { get; set; }
}

public class FifaLocalizedText
{
    public string Description { get; set; } = "";
}