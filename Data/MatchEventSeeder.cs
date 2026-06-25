using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data.FifaDtos;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class MatchEventSeeder
{
    private static readonly HttpClient Http = new();

    public static async Task SeedMatchEventsAsync(ApplicationDbContext context)
    {
        var games = await context.Games
            .Where(g => g.ExternalId != null)
            .ToListAsync();

        foreach (var game in games)
        {
            try
            {
                Console.WriteLine(
                    $"Importiere MatchEvents für Spiel {game.ExternalId}");

                var json =
                    await Http.GetStringAsync(
                        $"https://api.fifa.com/api/v3/timelines/{game.ExternalId}?language=de");

                var timeline =
                    JsonSerializer.Deserialize<FifaTimelineResponse>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (timeline?.Event == null)
                    continue;

                foreach (var fifaEvent in timeline.Event)
                {
                    var eventType = MapEventType(fifaEvent.Type);

                    if (eventType == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(fifaEvent.IdPlayer))
                        continue;

                    var player =
                        await context.Players
                            .FirstOrDefaultAsync(
                                p => p.ExternalId == fifaEvent.IdPlayer);

                    if (player == null)
                        continue;

                    var externalId =
                        int.TryParse(
                            fifaEvent.EventId,
                            out var parsedId)
                            ? parsedId
                            : (int?)null;

                    if (externalId != null &&
                        await context.MatchEvents.AnyAsync(
                            e => e.ExternalId == externalId))
                        continue;

                    var minute = ParseMinute(
                        fifaEvent.MatchMinute);

                    context.MatchEvents.Add(
                        new MatchEvent
                        {
                            GameId = game.Id,
                            PlayerId = player.Id,
                            EventType = eventType,
                            Minute = minute,
                            ExternalId = externalId
                        });
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Fehler bei Spiel {game.ExternalId}: {ex.Message}");
            }
        }
    }

    private static string? MapEventType(int type)
    {
        return type switch
        {
            0 => "Goal",
            1 => "Assist",
            2 => "YellowCard",
            3 => "RedCard",
            _ => null
        };
    }

    private static int ParseMinute(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var digits =
            new string(value.TakeWhile(char.IsDigit).ToArray());

        return int.TryParse(digits, out var minute)
            ? minute
            : 0;
    }
}