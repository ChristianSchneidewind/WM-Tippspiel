using System;
using System.Collections.Generic;

namespace TippSpiel.Models.ApiResponses
{
    public class FootballApiResponse
    {
        public List<MatchData> Response { get; set; } = new();
    }

    public class MatchData
    {
        public Fixture Fixture { get; set; } = new();
        public League League { get; set; } = new(); // NEU: Für die Gruppen-Logik
        public Teams Teams { get; set; } = new();
        public Goals Goals { get; set; } = new();
    }

    public class Fixture
    {
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; } // NEU: Wichtig für den Spielplan!
        public Status Status { get; set; } = new();
    }

    public class League // NEU: Enthält die Information über die Runde/Gruppe
    {
        public string Round { get; set; } = string.Empty; // z.B. "Group A" oder "Round of 16"
    }

    // ... (Deine restlichen Klassen: Status, Teams, ApiTeam, Goals bleiben gleich)

    public class Status
    {
        public string Short { get; set; } = string.Empty;
    }

    public class Teams
    {
        public ApiTeam Home { get; set; } = new();
        public ApiTeam Away { get; set; } = new();
    }

    public class ApiTeam
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Logo { get; set; }
    }

    public class Goals
    {
        public int? Home { get; set; }
        public int? Away { get; set; }
    }

    // --- Events Teil ---

    public class FixtureEventsResponse
    {
        public List<ApiEvent> Response { get; set; } = new();
    }

    public class ApiEvent
    {
        public ApiTime Time { get; set; } = new();
        public ApiTeam Team { get; set; } = new();
        public ApiPlayer Player { get; set; } = new();
        public ApiPlayer Assist { get; set; } = new();
        public string Type { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public class ApiTime
    {
        public int Elapsed { get; set; }
    }

    public class ApiPlayer
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}