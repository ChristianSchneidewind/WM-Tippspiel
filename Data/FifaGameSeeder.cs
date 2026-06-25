using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data.FifaDtos;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class FifaGameSeeder
{
    private static readonly HttpClient Http = new();

    private const string CompetitionId = "17";
    private const string SeasonId = "285023";
    private const string StageId = "289273";

    public static async Task SeedGroupGamesAsync(ApplicationDbContext db)
    {
        var url =
            $"https://api.fifa.com/api/v3/calendar/matches" +
            $"?language=de" +
            $"&idCompetition={CompetitionId}" +
            $"&idSeason={SeasonId}" +
            $"&idStage={StageId}" +
            $"&count=400";

        Console.WriteLine("[FIFA GAME IMPORT] Lade Spiele...");

        var json = await Http.GetStringAsync(url);
        File.WriteAllText("matches.json", json);

        var response =
            JsonSerializer.Deserialize<FifaMatchResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (response?.Results == null || response.Results.Count == 0)
        {
            Console.WriteLine("[FIFA GAME IMPORT] Keine Spiele gefunden.");
            return;
        }

        var imported = 0;

        foreach (var fifaMatch in response.Results)
        {
            if (!int.TryParse(fifaMatch.Home.IdTeam, out var homeExternalId))
                continue;

            if (!int.TryParse(fifaMatch.Away.IdTeam, out var awayExternalId))
                continue;

            var homeTeam = await db.Teams
                .FirstOrDefaultAsync(t => t.ExternalId == homeExternalId);

            var awayTeam = await db.Teams
                .FirstOrDefaultAsync(t => t.ExternalId == awayExternalId);

            if (homeTeam == null || awayTeam == null)
            {
                Console.WriteLine(
                    $"[FIFA GAME IMPORT] Team nicht gefunden: {homeExternalId} vs {awayExternalId}");
                continue;
            }

            var groupName =
                fifaMatch.GroupName.FirstOrDefault()?.Description
                ?? "Unbekannt";

            var group = await db.Groups
                .FirstOrDefaultAsync(g => g.Name == groupName);

            if (group == null)
            {
                group = new Group
                {
                    Name = groupName
                };

                db.Groups.Add(group);
                await db.SaveChangesAsync();
            }

            var externalId =
                int.TryParse(
                    fifaMatch.IdMatch,
                    out var parsedExternalId)
                    ? parsedExternalId
                    : (int?)null;

            var game = await db.Games
                .FirstOrDefaultAsync(g => g.ExternalId == externalId);

            if (game == null)
            {
                game = new Game();
                db.Games.Add(game);
            }

            game.ExternalId = externalId;
            game.MatchNumber = fifaMatch.MatchNumber;

            Console.WriteLine(
                $"Match {fifaMatch.IdMatch} -> IFES = {fifaMatch.Properties?.IdIFES}");
            game.IfesId = fifaMatch.Properties?.IdIFES;

            game.GroupId = group.Id;

            game.HomeTeamId = homeTeam.Id;
            game.AwayTeamId = awayTeam.Id;

            game.HomeTeamScore = fifaMatch.Home.Score;
            game.AwayTeamScore = fifaMatch.Away.Score;

            game.KickOff =
                DateTimeOffset.Parse(fifaMatch.Date);

            game.Venue =
                fifaMatch.Stadium?.Name?
                    .FirstOrDefault()?.Description;

            imported++;
        }

        await db.SaveChangesAsync();

        Console.WriteLine(
            $"[FIFA GAME IMPORT] {imported} Spiele importiert/aktualisiert.");
    }
}