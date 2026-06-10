using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data;

public static class FifaSeeder
{
    private static readonly HttpClient Http = new();

    public static async Task SeedTeamsAsync(ApplicationDbContext context)
    {
        const string teamsUrl =
            "https://cxm-api.fifa.com/fifaplusweb/api/sections/teamsModule/4v5Yng3VdGD9c1cpnOIff1?locale=de&limit=200";

        var json = await Http.GetStringAsync(teamsUrl);

        var response = JsonSerializer.Deserialize<FifaTeamResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (response == null)
            return;

        foreach (var fifaTeam in response.Teams)
        {
            var slug = fifaTeam.TeamPageUrl.Split('/').Last();
            var fifaExternalId = int.Parse(fifaTeam.TeamId);
            var excelName = MapFifaNameToExcelName(fifaTeam.TeamName);

            var dbTeam = await context.Teams
                .FirstOrDefaultAsync(t =>
                    t.ExternalId == fifaExternalId ||
                    t.Name == fifaTeam.TeamName ||
                    t.Name == excelName
                );

            if (dbTeam != null)
            {
                dbTeam.ExternalId = fifaExternalId;
                dbTeam.FlagUrl = fifaTeam.TeamFlag;
                dbTeam.Slug = slug;

                Console.WriteLine($"UPDATE: {dbTeam.Name} -> {dbTeam.ExternalId} | {dbTeam.Slug}");
                continue;
            }

            context.Teams.Add(new Team
            {
                Name = fifaTeam.TeamName,
                ExternalId = fifaExternalId,
                FlagUrl = fifaTeam.TeamFlag,
                Slug = slug
            });

            Console.WriteLine($"ADD: {fifaTeam.TeamName} -> {fifaExternalId} | {slug}");
        }

        await context.SaveChangesAsync();
    }

    private static string MapFifaNameToExcelName(string fifaName)
    {
        return fifaName switch
        {
            "Republik Korea" => "Südkorea",
            "Bosnien und Herzegowina" => "Bosnien/Herzeg.",
            "IR Iran" => "Iran",
            "DR\u00A0Kongo" => "DR Kongo",
            "Saudi-Arabien" => "Saudi Arabien",
            "Curaçao" => "Curacao",
            "Kap Verde" => "Kap Verde",
            "Elfenbeinküste" => "Elfenbeinküste",
            "Türkei" => "Türkei",
            _ => fifaName
        };
    }
}