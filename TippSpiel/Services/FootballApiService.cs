using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Models;
using TippSpiel.Models.ApiResponses;
using Microsoft.Extensions.Configuration;

namespace TippSpiel.Services
{
    public class FootballApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FootballApiService(ApplicationDbContext context, HttpClient httpClient, IConfiguration config)
        {
            _context = context;
            _httpClient = httpClient;
            _apiKey = config["FootballApi:ApiKey"] ?? throw new Exception("API Key nicht in appsettings.json gefunden!");
        }

        public async Task InitialSeed()
        {
            PrepareHttpClient();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var url = "https://v3.football.api-sports.io/fixtures?league=1&season=2026";

            var apiResponse = await _httpClient.GetFromJsonAsync<FootballApiResponse>(url, options);

            if (apiResponse?.Response == null) return;

            foreach (var item in apiResponse.Response)
            {
                var homeTeam = await GetOrCreateTeam(item.Teams.Home);
                var awayTeam = await GetOrCreateTeam(item.Teams.Away);

                // Hier nutzen wir die Methode, die unten definiert ist
                string rawRound = item.League?.Round ?? "WM 2026";
                string cleanGroupName = CleanRoundName(rawRound); 

                var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name == cleanGroupName);
                if (group == null)
                {
                    group = new Group { Name = cleanGroupName };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                }

                var existingGame = await _context.Games.FirstOrDefaultAsync(g => g.ExternalId == item.Fixture.Id);
                if (existingGame == null)
                {
                    var game = new Game
                    {
                        ExternalId = item.Fixture.Id,
                        HomeTeamId = homeTeam.Id,
                        AwayTeamId = awayTeam.Id,
                        KickOff = item.Fixture.Date,
                        GroupId = group.Id,
                        HomeTeamScore = item.Goals.Home,
                        AwayTeamScore = item.Goals.Away
                    };
                    _context.Games.Add(game);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task SyncMatchResults()
        {
            PrepareHttpClient();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                var url = "https://v3.football.api-sports.io/fixtures?league=1&season=2026";
                var apiResponse = await _httpClient.GetFromJsonAsync<FootballApiResponse>(url, options);

                if (apiResponse?.Response == null) return;

                foreach (var item in apiResponse.Response)
                {
                    var dbGame = await _context.Games.FirstOrDefaultAsync(g => g.ExternalId == item.Fixture.Id);

                    if (dbGame != null)
                    {
                        if (dbGame.HomeTeamScore != item.Goals.Home || dbGame.AwayTeamScore != item.Goals.Away)
                        {
                            dbGame.HomeTeamScore = item.Goals.Home;
                            dbGame.AwayTeamScore = item.Goals.Away;
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim API-Sync: {ex.Message}");
            }
        }

        // --- HILFSMETHODEN ---

        private string CleanRoundName(string round)
        {
            if (string.IsNullOrEmpty(round)) return "WM 2026";

            // Macht aus "Group A" -> "A"
            if (round.StartsWith("Group ", StringComparison.OrdinalIgnoreCase))
            {
                return round.Replace("Group ", "", StringComparison.OrdinalIgnoreCase).Trim();
            }
            
            // K.o. Phasen übersetzen
            if (round.Contains("Round of 16")) return "Achtelfinale";
            if (round.Contains("Quarter-finals")) return "Viertelfinale";
            if (round.Contains("Semi-finals")) return "Halbfinale";
            if (round.Contains("Final")) return "Finale";

            return round;
        }

        private async Task<Team> GetOrCreateTeam(ApiTeam apiTeam)
        {
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.ExternalId == apiTeam.Id);
            if (team == null)
            {
                team = new Team
                {
                    Name = apiTeam.Name,
                    ExternalId = apiTeam.Id,
                    FlagUrl = apiTeam.Logo
                };
                _context.Teams.Add(team);
                await _context.SaveChangesAsync();
            }
            return team;
        }

        private void PrepareHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-apisports-key", _apiKey);
            }
        }
    }
}