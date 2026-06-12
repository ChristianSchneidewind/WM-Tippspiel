using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class FifaGameSeeder
{
    private static readonly HttpClient Http = new();
    private const string CompetitionExternalId = "285023";

    public static async Task SeedGroupGamesAsync(ApplicationDbContext db)
    {
        var token = await GetGameDayTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[FIFA GAME IMPORT] Kein GameDay-Token verfügbar.");
            return;
        }

        var competition = await GetCompetitionAsync(token);
        if (competition == null)
        {
            Console.WriteLine($"[FIFA GAME IMPORT] Wettbewerb {CompetitionExternalId} nicht gefunden.");
            return;
        }

        var venueCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matchNumber = 1;

        foreach (var stageRef in competition.Stages)
        {
            if (string.IsNullOrWhiteSpace(stageRef.ExternalStageId))
                continue;

            var stage = await GetStageAsync(token, stageRef.ExternalStageId);
            if (stage?.Events == null || stage.Events.Count == 0)
                continue;

            var groupName = stageRef.Name?.Deu ?? stageRef.Name?.Eng ?? stageRef.ExternalStageId;
            var group = await GetOrCreateGroupAsync(db, groupName);

            foreach (var stageEvent in stage.Events
                         .OrderBy(e => ParseDateTimeOffset(e.DateTime)))
            {
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
                {
                    Console.WriteLine($"[FIFA GAME IMPORT] Team-ID fehlt für {detail.Name?.Eng ?? detail.ExternalId}");
                    continue;
                }

                var homeTeam = await db.Teams.FirstOrDefaultAsync(t => t.ExternalId == homeExternalId.Value);
                var awayTeam = await db.Teams.FirstOrDefaultAsync(t => t.ExternalId == awayExternalId.Value);

                if (homeTeam == null || awayTeam == null)
                {
                    Console.WriteLine($"[FIFA GAME IMPORT] Team nicht gefunden: {homeExternalId} vs {awayExternalId}");
                    continue;
                }

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

                matchNumber++;
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"[FIFA GAME IMPORT] {matchNumber - 1} Gruppenspiele importiert/aktualisiert.");
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

    private static async Task<StageResponse?> GetStageAsync(string token, string stageId)
    {
        var json = await GetJsonAsync($"https://gameday-prod.fifa.mangodev.co.uk/1-0/stages/fifa/{stageId}?aggregated=true", token);
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
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
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

        [JsonPropertyName("eventCompletionState")]
        public string? EventCompletionState { get; set; }
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
