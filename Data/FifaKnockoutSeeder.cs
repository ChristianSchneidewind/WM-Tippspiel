using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class FifaKnockoutSeeder
{
    private static readonly HttpClient Http = new();
    private const string CompetitionExternalId = "285023";

    private static readonly Dictionary<string, (int Start, int End, string GermanName)> RoundMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Round of 32"] = (73, 88, "Sechzehntelfinale"),
        ["Round of 16"] = (89, 96, "Achtelfinale"),
        ["Quarter-final"] = (97, 100, "Viertelfinale"),
        ["Semi-final"] = (101, 102, "Halbfinale"),
        ["Play-off for third place"] = (103, 103, "Spiel um Platz 3"),
        ["Final"] = (104, 104, "Finale")
    };

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var token = await GetGameDayTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[FIFA KO IMPORT] Kein GameDay-Token verfügbar. Nutze Legacy-Fallback.");
            /* await KnockoutSeeder.SeedAsync(db, new Dictionary<int, string>()); */
            return;
        }

        var competition = await GetCompetitionAsync(token);
        if (competition == null)
        {
            Console.WriteLine("[FIFA KO IMPORT] Wettbewerb nicht gefunden. Nutze Legacy-Fallback.");
            /* await KnockoutSeeder.SeedAsync(db, new Dictionary<int, string>()); */
            return;
        }

        var venueCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;

        foreach (var stageRef in competition.Stages)
        {
            if (string.IsNullOrWhiteSpace(stageRef.Link))
                continue;

            var stage = await GetStageAsync(token, stageRef.Link);
            if (stage == null || !string.Equals(stage.StageType, "urn:gd:stage:type:knockout", StringComparison.OrdinalIgnoreCase))
                continue;

            var roundNameEn = stageRef.Name?.Eng ?? stage.Name?.Eng;
            if (string.IsNullOrWhiteSpace(roundNameEn) || !RoundMap.TryGetValue(roundNameEn, out var round))
                continue;

            if (stage.Events == null || stage.Events.Count == 0)
            {
                Console.WriteLine($"[FIFA KO IMPORT] Noch keine FIFA-Spiele für {round.GermanName}. Legacy-Fallback bleibt aktiv.");
                continue;
            }

            var group = await GetOrCreateGroupAsync(db, round.GermanName);
            var orderedEvents = stage.Events
                .OrderBy(e => ParseDateTimeOffset(e.DateTime))
                .ToList();

            for (var index = 0; index < orderedEvents.Count; index++)
            {
                var stageEvent = orderedEvents[index];
                var detail = await GetEventDetailAsync(token, stageEvent.Link);
                if (detail == null)
                    continue;

                var teams = detail.Participants
                    .Where(p => string.Equals(p.KeyType, "team", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (teams.Count < 2)
                    continue;

                var home = teams.FirstOrDefault(p => string.Equals(p.Role, "Home Team", StringComparison.OrdinalIgnoreCase))
                           ?? teams.OrderBy(p => p.Number ?? int.MaxValue).First();
                var away = teams.FirstOrDefault(p => string.Equals(p.Role, "Away Team", StringComparison.OrdinalIgnoreCase))
                           ?? teams.OrderByDescending(p => p.Number ?? int.MinValue).First();

                var homeExternalId = ParseTeamExternalId(home.ExternalTeamId);
                var awayExternalId = ParseTeamExternalId(away.ExternalTeamId);
                if (homeExternalId == null || awayExternalId == null)
                    continue;

                var homeTeam = await db.Teams.FirstOrDefaultAsync(t => t.ExternalId == homeExternalId.Value);
                var awayTeam = await db.Teams.FirstOrDefaultAsync(t => t.ExternalId == awayExternalId.Value);
                if (homeTeam == null || awayTeam == null)
                    continue;

                var venueName = string.Empty;
                if (!string.IsNullOrWhiteSpace(detail.ExternalVenueId))
                {
                    if (!venueCache.TryGetValue(detail.ExternalVenueId!, out var cachedVenueName))
                    {
                        venueName = await GetVenueNameAsync(token, detail.ExternalVenueId!);
                        venueCache[detail.ExternalVenueId!] = venueName;
                    }
                    else
                    {
                        venueName = cachedVenueName;
                    }
                }

                var kickoff = ParseDateTimeOffset(detail.DateTime);
                var externalGameId = int.TryParse(detail.ExternalId, out var parsedExternalGameId)
                    ? parsedExternalGameId
                    : (int?)null;

                var matchNumber = round.Start + index;
                var game = await db.Games.FirstOrDefaultAsync(g => g.MatchNumber == matchNumber);
                if (game == null)
                {
                    game = new Game();
                    db.Games.Add(game);
                }

                game.MatchNumber = matchNumber;
                game.ExternalId = externalGameId;
                game.GroupId = group.Id;
                game.HomeTeamId = homeTeam.Id;
                game.AwayTeamId = awayTeam.Id;
                game.KickOff = kickoff;
                game.Venue = venueName;
                game.HomeTeamScore = home.Score;
                game.AwayTeamScore = away.Score;

                imported++;
            }
        }

        await db.SaveChangesAsync();

        if (imported == 0)
        {
            Console.WriteLine("[FIFA KO IMPORT] Keine FIFA-K.o.-Spiele gefunden. Legacy-Fallback wird verwendet.");
            /* await KnockoutSeeder.SeedAsync(db, new Dictionary<int, string>()); */
            return;
        }

        Console.WriteLine($"[FIFA KO IMPORT] {imported} K.o.-Spiele importiert/aktualisiert.");
    }

    private static async Task<string?> GetGameDayTokenAsync()
    {
        var json = await Http.GetStringAsync("https://cxm-api.fifa.com/fifaplusweb/api/external/gameDay/token");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("token", out var tokenElement)
            ? tokenElement.GetString()
            : null;
    }

    private static async Task<CompetitionDto?> GetCompetitionAsync(string token)
    {
        var json = await GetJsonAsync($"https://gameday-prod.fifa.mangodev.co.uk/1-0/competitions?query=_externalId==`{CompetitionExternalId}`", token);
        var response = JsonSerializer.Deserialize<CompetitionResponse>(json, JsonOptions);
        return response?.Items.FirstOrDefault(c => c.ExternalId == CompetitionExternalId);
    }

    private static async Task<StageResponse?> GetStageAsync(string token, string stageUrl)
    {
        var json = await GetJsonAsync(stageUrl, token);
        return JsonSerializer.Deserialize<StageResponse>(json, JsonOptions);
    }

    private static async Task<EventDetailResponse?> GetEventDetailAsync(string token, string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return null;

        var json = await GetJsonAsync(link, token);
        return JsonSerializer.Deserialize<EventDetailResponse>(json, JsonOptions);
    }

    private static async Task<string> GetVenueNameAsync(string token, string venueId)
    {
        var json = await GetJsonAsync($"https://gameday-prod.fifa.mangodev.co.uk/1-0/venues/fifa/{venueId}?aggregated=true", token);
        var venue = JsonSerializer.Deserialize<VenueResponse>(json, JsonOptions);
        return venue?.Name?.Deu ?? venue?.Name?.Eng ?? string.Empty;
    }

    private static async Task<string> GetJsonAsync(string url, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
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
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static int? ParseTeamExternalId(string? externalTeamId)
    {
        if (string.IsNullOrWhiteSpace(externalTeamId))
            return null;

        var rawId = externalTeamId.Split('_').LastOrDefault();
        return int.TryParse(rawId, out var id) ? id : null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class CompetitionResponse
    {
        [JsonPropertyName("items")]
        public List<CompetitionDto> Items { get; set; } = [];
    }

    private sealed class CompetitionDto
    {
        [JsonPropertyName("_externalId")]
        public string ExternalId { get; set; } = string.Empty;

        [JsonPropertyName("stages")]
        public List<CompetitionStageDto> Stages { get; set; } = [];
    }

    private sealed class CompetitionStageDto
    {
        [JsonPropertyName("_externalStageId")]
        public string ExternalStageId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public LocalizedTextDto? Name { get; set; }

        [JsonPropertyName("_link")]
        public string? Link { get; set; }
    }

    private sealed class StageResponse
    {
        [JsonPropertyName("events")]
        public List<StageEventDto> Events { get; set; } = [];

        [JsonPropertyName("stageType")]
        public string? StageType { get; set; }

        [JsonPropertyName("name")]
        public LocalizedTextDto? Name { get; set; }
    }

    private sealed class StageEventDto
    {
        [JsonPropertyName("dateTime")]
        public string DateTime { get; set; } = string.Empty;

        [JsonPropertyName("_link")]
        public string? Link { get; set; }
    }

    private sealed class EventDetailResponse
    {
        [JsonPropertyName("_externalId")]
        public string ExternalId { get; set; } = string.Empty;

        [JsonPropertyName("_externalVenueId")]
        public string? ExternalVenueId { get; set; }

        [JsonPropertyName("dateTime")]
        public string DateTime { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public LocalizedTextDto? Name { get; set; }

        [JsonPropertyName("participants")]
        public List<EventParticipantDto> Participants { get; set; } = [];
    }

    private sealed class EventParticipantDto
    {
        [JsonPropertyName("keyType")]
        public string? KeyType { get; set; }

        [JsonPropertyName("number")]
        public int? Number { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("_externalTeamId")]
        public string? ExternalTeamId { get; set; }
    }

    private sealed class VenueResponse
    {
        [JsonPropertyName("name")]
        public LocalizedTextDto? Name { get; set; }
    }

    private sealed class LocalizedTextDto
    {
        [JsonPropertyName("deu")]
        public string? Deu { get; set; }

        [JsonPropertyName("eng")]
        public string? Eng { get; set; }
    }
}
