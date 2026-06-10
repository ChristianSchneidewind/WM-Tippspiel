using Microsoft.AspNetCore.SignalR;

namespace TippSpiel.Hubs
{
    public class GameResultHub : Hub
    {
        /// <summary>
        /// Wird aufgerufen, wenn ein neues Ergebnis eingetragen wurde
        /// </summary>
        public async Task NotifyResultUpdated(int gameId, string homeTeam, string awayTeam, int homeScore, int awayScore)
        {
            // Benachrichtige alle verbundenen Clients
            await Clients.All.SendAsync("ReceiveResultUpdate", new
            {
                GameId = gameId,
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                HomeScore = homeScore,
                AwayScore = awayScore,
                UpdatedAt = System.DateTime.Now
            });
        }

        /// <summary>
        /// Wird aufgerufen, wenn sich die Gruppentabelle aktualisiert hat
        /// </summary>
        public async Task NotifyGroupStandingsUpdated(int groupId)
        {
            // Benachrichtige alle verbundenen Clients in der Gruppe
            await Clients.All.SendAsync("ReceiveGroupStandingsUpdate", new
            {
                GroupId = groupId,
                UpdatedAt = System.DateTime.Now
            });
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Rankings aktualisiert wurden
        /// </summary>
        public async Task NotifyRankingsUpdated()
        {
            // Benachrichtige alle verbundenen Clients
            await Clients.All.SendAsync("ReceiveRankingsUpdate", new
            {
                UpdatedAt = System.DateTime.Now
            });
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
