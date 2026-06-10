namespace TippSpiel.Data.FifaDtos;

public class FifaPlayerDto
{
    public string IdPlayer { get; set; } = "";
    public string JerseyNum { get; set; } = "";
    public string Position { get; set; } = "";

    public List<FifaDescription> PlayerName { get; set; } = [];
}