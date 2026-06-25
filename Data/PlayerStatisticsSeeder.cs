using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class PlayerStatisticsSeeder
{
    private static readonly HttpClient Http = new();

    public static async Task SeedPlayerStatisticsAsync(
        ApplicationDbContext context)
    {
        Console.WriteLine("Starte Spielerstatistik-Import...");
        await context.Players
            .ExecuteUpdateAsync(p => p
            .SetProperty(x => x.Appearances, 0));

        var games = await context.Games
            .Where(g => g.ExternalId != null)
            .ToListAsync();

        foreach (var game in games)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(game.IfesId))
                    continue;
                var ifesId = game.IfesId;

                Console.WriteLine(
                    $"Spiel {game.MatchNumber}: IFES {ifesId}");

                var url =
                    $"https://fdh-api.fifa.com/v1/stats/match/{ifesId}/players.json";

                var json = await Http.GetStringAsync(url);

                File.WriteAllText($"stats_{ifesId}.json", json);
                Console.WriteLine($"JSON gespeichert: stats_{ifesId}.json");
                Console.WriteLine(json.Substring(0, 300));

                using var doc = JsonDocument.Parse(json);

                foreach (var playerEntry in doc.RootElement.EnumerateObject())
                {
                    var externalId = playerEntry.Name;

                    var player = await context.Players
                        .FirstOrDefaultAsync(
                            p => p.ExternalId == externalId);

                    if (player == null)
                        continue;

                    var matchesPlayed = GetInt(
                        playerEntry.Value,
                        "MatchesPlayed");

                    Console.WriteLine(
                        $"{player.Name} -> {matchesPlayed}");

                    player.Appearances += matchesPlayed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Fehler bei Spiel {game.ExternalId}: {ex.Message}");
            }
        }

        await context.SaveChangesAsync();

        Console.WriteLine(
            "Spielerstatistik-Import abgeschlossen.");
    }

    private static int GetInt(
    JsonElement stats,
    string statName)
    {
        foreach (var stat in stats.EnumerateArray())
        {
            if (stat.GetArrayLength() < 2)
                continue;

            if (stat[0].GetString() == statName)
            {
                if (stat[1].ValueKind == JsonValueKind.Number)
                    return stat[1].GetInt32();
            }
        }

        return 0;
    }
}