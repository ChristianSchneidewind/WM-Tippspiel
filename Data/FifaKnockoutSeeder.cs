using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class FifaKnockoutSeeder
{
    private static readonly HttpClient Http = new();

    private const string CompetitionId = "17";
    private const string SeasonId = "285023";

    private static readonly Dictionary<int, string> KnockoutStages = new()
    {
        [289287] = "Sechzehntelfinale",
        [289288] = "Achtelfinale",
        [289289] = "Viertelfinale",
        [289290] = "Halbfinale",
        [289291] = "Spiel um Platz 3",
        [289292] = "Finale"
    };

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        // KO-Runde einmalig anlegen, falls sie noch nicht existiert
        if (!await db.Games.AnyAsync(g => g.MatchNumber == 104))
        {
            Console.WriteLine("[FIFA KO IMPORT] Erzeuge KO-Baum...");
            await KnockoutSeeder.SeedAsync(db, new Dictionary<int, string>());
        }

        var imported = 0;

        foreach (var stageEntry in KnockoutStages)
        {
            var stageId = stageEntry.Key;
            var fallbackGroupName = stageEntry.Value;

            var response = await GetMatchesAsync(stageId);
            if (response?.Results == null || response.Results.Count == 0)
            {
                Console.WriteLine($"[FIFA KO IMPORT] Keine FIFA-Spiele für {fallbackGroupName} gefunden.");
                continue;
            }

            foreach (var match in response.Results)
            {
                if (match.MatchNumber < 73 || match.MatchNumber > 104)
                    continue;

                if (!int.TryParse(match.Home?.IdTeam, out var homeExternalId))
                {
                    Console.WriteLine($"[FIFA KO IMPORT] Heimteam ungültig bei Spiel {match.MatchNumber}: {match.Home?.IdTeam}");
                    continue;
                }

                if (!int.TryParse(match.Away?.IdTeam, out var awayExternalId))
                {
                    Console.WriteLine($"[FIFA KO IMPORT] Auswärtsteam ungültig bei Spiel {match.MatchNumber}: {match.Away?.IdTeam}");
                    continue;
                }

                var homeTeam = await db.Teams
                    .FirstOrDefaultAsync(t => t.ExternalId == homeExternalId);

                var awayTeam = await db.Teams
                    .FirstOrDefaultAsync(t => t.ExternalId == awayExternalId);

                if (homeTeam == null || awayTeam == null)
                {
                    Console.WriteLine(
                        $"[FIFA KO IMPORT] Team nicht gefunden bei Spiel {match.MatchNumber}: {homeExternalId} vs {awayExternalId}");
                    continue;
                }

                var groupName =
                    match.StageName?.FirstOrDefault()?.Description
                    ?? fallbackGroupName;

                var group = await GetOrCreateGroupAsync(db, groupName);

                var externalId =
                    int.TryParse(match.IdMatch, out var parsedExternalId)
                        ? parsedExternalId
                        : (int?)null;

                var game = await db.Games
                    .FirstOrDefaultAsync(g => g.MatchNumber == match.MatchNumber);

                if (game == null && externalId != null)
                {
                    game = await db.Games
                        .FirstOrDefaultAsync(g => g.ExternalId == externalId);
                }

                if (game == null)
                {
                    game = new Game();
                    db.Games.Add(game);
                }

                game.MatchNumber = match.MatchNumber;
                game.ExternalId = externalId;
                game.IfesId = match.Properties?.IdIFES;

                game.GroupId = group.Id;

                game.HomeTeamId = homeTeam.Id;
                game.AwayTeamId = awayTeam.Id;

                game.HomeTeamScore = match.Home?.Score;
                game.AwayTeamScore = match.Away?.Score;
                game.HomeTeamPenaltyScore = match.HomeTeamPenaltyScore;
                game.AwayTeamPenaltyScore = match.AwayTeamPenaltyScore;

                game.KickOff = ParseDateTimeOffset(match.Date);

                game.Venue =
                    match.Stadium?.Name?.FirstOrDefault()?.Description;

                imported++;
            }
        }

        await db.SaveChangesAsync();

        Console.WriteLine($"[FIFA KO IMPORT] {imported} K.o.-Spiele importiert/aktualisiert.");
    }

    private static async Task<FifaCalendarMatchesResponse?> GetMatchesAsync(int stageId)
    {
        var url =
            $"https://api.fifa.com/api/v3/calendar/matches" +
            $"?language=de" +
            $"&idCompetition={CompetitionId}" +
            $"&idSeason={SeasonId}" +
            $"&idStage={stageId}" +
            $"&count=400";

        Console.WriteLine($"[FIFA KO IMPORT] Lade Stage {stageId}...");

        var json = await Http.GetStringAsync(url);

        return JsonSerializer.Deserialize<FifaCalendarMatchesResponse>(
            json,
            JsonOptions);
    }

    private static async Task<Group> GetOrCreateGroupAsync(ApplicationDbContext db, string name)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == name);
        if (group != null)
            return group;

        group = new Group { Name = name };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    private static DateTimeOffset ParseDateTimeOffset(string value)
    {
        return DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class FifaCalendarMatchesResponse
    {
        public List<FifaCalendarMatchDto> Results { get; set; } = [];
    }

    private sealed class FifaCalendarMatchDto
    {
        public string IdMatch { get; set; } = string.Empty;
        public int MatchNumber { get; set; }
        public string Date { get; set; } = string.Empty;

        public List<FifaDescriptionDto> StageName { get; set; } = [];

        public FifaCalendarTeamDto? Home { get; set; }
        public FifaCalendarTeamDto? Away { get; set; }

        public int? HomeTeamPenaltyScore { get; set; }
        public int? AwayTeamPenaltyScore { get; set; }

        public FifaCalendarStadiumDto? Stadium { get; set; }
        public FifaCalendarPropertiesDto? Properties { get; set; }
    }

    private sealed class FifaCalendarTeamDto
    {
        public string IdTeam { get; set; } = string.Empty;
        public int? Score { get; set; }
    }

    private sealed class FifaCalendarStadiumDto
    {
        public List<FifaDescriptionDto> Name { get; set; } = [];
    }

    private sealed class FifaCalendarPropertiesDto
    {
        public string? IdIFES { get; set; }
    }

    private sealed class FifaDescriptionDto
    {
        public string Description { get; set; } = string.Empty;
    }
}
