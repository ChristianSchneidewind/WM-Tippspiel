using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TippSpiel.Data;
using TippSpiel.Hubs;
using TippSpiel.Models.Admin;

namespace TippSpiel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScraperController : ControllerBase
    {
        private readonly IGameRepository _repository;
        private readonly IHubContext<GameResultHub> _hubContext;
        private readonly IConfiguration _configuration;

        public ScraperController(IGameRepository repository, IHubContext<GameResultHub> hubContext, IConfiguration configuration)
        {
            _repository = repository;
            _hubContext = hubContext;
            _configuration = configuration;
        }

        /// <summary>
        /// API-Endpoint für Scraper zum automatischen Eintragen von Ergebnissen
        /// Benötigt ein gültiges API-Token für Sicherheit
        /// </summary>
        [HttpPost("update-result")]
        public async Task<IActionResult> UpdateResultFromScraper([FromBody] ScraperResultRequest request)
        {
            // 🔒 Sicherheit: API-Token validieren
            var apiToken = Request.Headers["X-API-Token"].ToString();
            var configuredToken = _configuration["Scraper:ApiToken"];

            if (string.IsNullOrEmpty(apiToken) || apiToken != configuredToken)
            {
                return Unauthorized(new { error = "Ungültiges oder fehlendes API-Token" });
            }

            // Validierung
            if (request?.GameId <= 0)
                return BadRequest(new { error = "Ungültige GameId" });

            if (request.HomeScore < 0 || request.AwayScore < 0)
                return BadRequest(new { error = "Tore können nicht negativ sein" });

            try
            {
                var game = _repository.GetGame(request.GameId);
                if (game == null)
                    return NotFound(new { error = $"Spiel mit ID {request.GameId} nicht gefunden" });

                // 🎯 Aktualisiere das Ergebnis
                _repository.UpdateResult(request.GameId, request.HomeScore, request.AwayScore);

                // 🔔 SignalR: Benachrichtige alle verbundenen Clients
                await _hubContext.Clients.All.SendAsync("ReceiveResultUpdate", new
                {
                    GameId = game.Id,
                    HomeTeam = game.HomeTeam?.Name ?? game.HomeTeamName ?? "Home",
                    AwayTeam = game.AwayTeam?.Name ?? game.AwayTeamName ?? "Away",
                    HomeScore = request.HomeScore,
                    AwayScore = request.AwayScore,
                    UpdatedAt = DateTime.Now,
                    Source = "Scraper" // Zeigt, dass es vom Scraper kam
                });

                // 🔔 SignalR: Benachrichtige über Gruppentabellen-Update
                if (game.GroupId > 0)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveGroupStandingsUpdate", new
                    {
                        GroupId = game.GroupId,
                        UpdatedAt = DateTime.Now,
                        Source = "Scraper"
                    });
                }

                // 🔔 SignalR: Benachrichtige über Rankings-Update
                await _hubContext.Clients.All.SendAsync("ReceiveRankingsUpdate", new
                {
                    UpdatedAt = DateTime.Now,
                    Source = "Scraper"
                });

                return Ok(new
                {
                    success = true,
                    message = $"Ergebnis aktualisiert: {game.HomeTeamName} {request.HomeScore}:{request.AwayScore} {game.AwayTeamName}",
                    gameId = request.GameId,
                    homeScore = request.HomeScore,
                    awayScore = request.AwayScore
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Fehler beim Aktualisieren: {ex.Message}" });
            }
        }

        /// <summary>
        /// Batch-Update für mehrere Ergebnisse auf einmal
        /// </summary>
        [HttpPost("update-results-batch")]
        public async Task<IActionResult> UpdateResultsBatch([FromBody] List<ScraperResultRequest> requests)
        {
            // 🔒 Sicherheit: API-Token validieren
            var apiToken = Request.Headers["X-API-Token"].ToString();
            var configuredToken = _configuration["Scraper:ApiToken"];

            if (string.IsNullOrEmpty(apiToken) || apiToken != configuredToken)
                return Unauthorized(new { error = "Ungültiges oder fehlendes API-Token" });

            if (requests == null || requests.Count == 0)
                return BadRequest(new { error = "Keine Ergebnisse im Request" });

            var results = new List<object>();

            foreach (var request in requests)
            {
                try
                {
                    var game = _repository.GetGame(request.GameId);
                    if (game == null)
                    {
                        results.Add(new { gameId = request.GameId, success = false, error = "Spiel nicht gefunden" });
                        continue;
                    }

                    _repository.UpdateResult(request.GameId, request.HomeScore, request.AwayScore);
                    results.Add(new { gameId = request.GameId, success = true });
                }
                catch (Exception ex)
                {
                    results.Add(new { gameId = request.GameId, success = false, error = ex.Message });
                }
            }

            // 🔔 Sende nur einen Update nach dem Batch
            await _hubContext.Clients.All.SendAsync("ReceiveRankingsUpdate", new
            {
                UpdatedAt = DateTime.Now,
                Source = "Scraper",
                UpdatedCount = results.Count(r => (bool)(((dynamic)r).success ?? false))
            });

            return Ok(new
            {
                success = true,
                message = $"{results.Count} Ergebnisse verarbeitet",
                results = results
            });
        }

        /// <summary>
        /// Health-Check für Scraper
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok", timestamp = DateTime.Now });
        }
    }

    /// <summary>
    /// Request-Modell für Scraper
    /// </summary>
    public class ScraperResultRequest
    {
        public int GameId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}
