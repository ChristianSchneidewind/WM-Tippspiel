using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data.FifaDtos;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class PlayerSeeder
{
    private static readonly HttpClient Http = new();

    public static async Task SeedPlayersAsync(ApplicationDbContext context)
    {
        var teams = await context.Teams
            .Where(t => t.ExternalId != null)
            .ToListAsync();

        foreach (var team in teams)
        {
            try
            {
                Console.WriteLine($"Importiere Spieler für {team.Name}");

                var url =
                    $"https://api.fifa.com/api/v3/teams/{team.ExternalId}/squad" +
                    "?idCompetition=17&idSeason=285023&language=de";

                var json = await Http.GetStringAsync(url);

                var response =
                    JsonSerializer.Deserialize<FifaSquadResponse>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (response?.Players == null)
                    continue;

                int imported = 0;

                foreach (var fifaPlayer in response.Players)
                {
                    if (await context.Players.AnyAsync(
                            p => p.ExternalId == fifaPlayer.IdPlayer))
                        continue;

                    var playerName =
                        fifaPlayer.PlayerName?
                            .FirstOrDefault()?
                            .Description
                        ?? "Unbekannt";

                    var position =
                        fifaPlayer.PositionLocalized?
                            .FirstOrDefault()?
                            .Description;

                    context.Players.Add(new Player
                    {
                        Name = playerName,
                        ExternalId = fifaPlayer.IdPlayer,
                        TeamId = team.Id,
                        Position = position,
                        Appearances = fifaPlayer.MatchesPlayed ?? 0
                    });

                    imported++;
                }

                await context.SaveChangesAsync();

                Console.WriteLine(
                    $"{imported} Spieler importiert."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Fehler bei {team.Name}: {ex.Message}"
                );
            }
        }
    }
}