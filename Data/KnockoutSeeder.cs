using Microsoft.EntityFrameworkCore;
using TippSpiel.Models;

namespace TippSpiel.Data
{
    public static class KnockoutSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db, Dictionary<int, string> matchIdToVenue)
        {
            // Wir prüfen, ob das Finale schon existiert. Wenn ja, überspringen wir alles.
            if (await db.Games.AnyAsync(g => g.MatchNumber == 104))
            {
                // Aber wir stellen sicher, dass alle existierenden K.o.-Spiele einen Venue haben
                var knockoutGames = await db.Games.Where(g => g.MatchNumber >= 73).ToListAsync();
                bool changed = false;
                foreach (var g in knockoutGames)
                {
                    if (g.MatchNumber.HasValue && string.IsNullOrEmpty(g.Venue) && matchIdToVenue.TryGetValue(g.MatchNumber.Value, out var v))
                    {
                        g.Venue = v;
                        changed = true;
                    }
                }
                if (changed) await db.SaveChangesAsync();
                return;
            }

            var knockoutRounds = new Dictionary<string, (int start, int end)>
            {
                { "Sechzehntelfinale", (73, 88) },
                { "Achtelfinale", (89, 96) },
                { "Viertelfinale", (97, 100) },
                { "Halbfinale", (101, 102) },
                { "Spiel um Platz 3", (103, 103) },
                { "Finale", (104, 104) }
            };

            // Präzise Termine für alle K.o.-Spiele (basierend auf FIFA 2026 Schedule)
            // Alle Zeiten sind in UTC angegeben (+2h für deutsche Sommerzeit)
            var matchSchedule = new Dictionary<int, DateTimeOffset>
            {
                // Sechzehntelfinale (R32)
                [73] = new DateTimeOffset(2026, 6, 28, 19, 0, 0, TimeSpan.Zero), // 21:00 DE
                [74] = new DateTimeOffset(2026, 6, 29, 16, 0, 0, TimeSpan.Zero), // 18:00 DE
                [75] = new DateTimeOffset(2026, 6, 29, 19, 0, 0, TimeSpan.Zero), // 21:00 DE
                [76] = new DateTimeOffset(2026, 6, 29, 22, 0, 0, TimeSpan.Zero), // 00:00 DE (+1)
                [77] = new DateTimeOffset(2026, 6, 30, 16, 0, 0, TimeSpan.Zero),
                [78] = new DateTimeOffset(2026, 6, 30, 19, 0, 0, TimeSpan.Zero),
                [79] = new DateTimeOffset(2026, 6, 30, 22, 0, 0, TimeSpan.Zero),
                [80] = new DateTimeOffset(2026, 7, 1, 16, 0, 0, TimeSpan.Zero),
                [81] = new DateTimeOffset(2026, 7, 1, 19, 0, 0, TimeSpan.Zero),
                [82] = new DateTimeOffset(2026, 7, 1, 22, 0, 0, TimeSpan.Zero),
                [83] = new DateTimeOffset(2026, 7, 2, 16, 0, 0, TimeSpan.Zero),
                [84] = new DateTimeOffset(2026, 7, 2, 19, 0, 0, TimeSpan.Zero),
                [85] = new DateTimeOffset(2026, 7, 2, 22, 0, 0, TimeSpan.Zero),
                [86] = new DateTimeOffset(2026, 7, 3, 16, 0, 0, TimeSpan.Zero),
                [87] = new DateTimeOffset(2026, 7, 3, 19, 0, 0, TimeSpan.Zero),
                [88] = new DateTimeOffset(2026, 7, 3, 22, 0, 0, TimeSpan.Zero),

                // Achtelfinale (R16)
                [89] = new DateTimeOffset(2026, 7, 4, 16, 0, 0, TimeSpan.Zero),
                [90] = new DateTimeOffset(2026, 7, 4, 19, 0, 0, TimeSpan.Zero),
                [91] = new DateTimeOffset(2026, 7, 5, 16, 0, 0, TimeSpan.Zero),
                [92] = new DateTimeOffset(2026, 7, 5, 19, 0, 0, TimeSpan.Zero),
                [93] = new DateTimeOffset(2026, 7, 6, 16, 0, 0, TimeSpan.Zero),
                [94] = new DateTimeOffset(2026, 7, 6, 19, 0, 0, TimeSpan.Zero),
                [95] = new DateTimeOffset(2026, 7, 7, 16, 0, 0, TimeSpan.Zero),
                [96] = new DateTimeOffset(2026, 7, 7, 19, 0, 0, TimeSpan.Zero),

                // Viertelfinale (QF)
                [97] = new DateTimeOffset(2026, 7, 9, 19, 0, 0, TimeSpan.Zero),
                [98] = new DateTimeOffset(2026, 7, 10, 19, 0, 0, TimeSpan.Zero),
                [99] = new DateTimeOffset(2026, 7, 11, 16, 0, 0, TimeSpan.Zero),
                [100] = new DateTimeOffset(2026, 7, 11, 19, 0, 0, TimeSpan.Zero),

                // Halbfinale (SF)
                [101] = new DateTimeOffset(2026, 7, 14, 19, 0, 0, TimeSpan.Zero),
                [102] = new DateTimeOffset(2026, 7, 15, 19, 0, 0, TimeSpan.Zero),

                // Spiel um Platz 3
                [103] = new DateTimeOffset(2026, 7, 18, 19, 0, 0, TimeSpan.Zero),

                // Finale
                [104] = new DateTimeOffset(2026, 7, 19, 19, 0, 0, TimeSpan.Zero)
            };

            foreach (var round in knockoutRounds)
            {
                var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == round.Key);
                if (group == null)
                {
                    group = new Group { Name = round.Key };
                    db.Groups.Add(group);
                    await db.SaveChangesAsync();
                }

                for (int mNr = round.Value.start; mNr <= round.Value.end; mNr++)
                {
                    // Nur hinzufügen, wenn diese Matchnummer noch nicht existiert
                    if (!await db.Games.AnyAsync(g => g.MatchNumber == mNr))
                    {
                        var venue = string.Empty;
                        matchIdToVenue.TryGetValue(mNr, out venue);

                        var game = new Game
                        {
                            MatchNumber = mNr,
                            GroupId = group.Id,
                            KickOff = matchSchedule.ContainsKey(mNr) ? matchSchedule[mNr] : DateTimeOffset.Now,
                            Venue = venue
                        };
                        db.Games.Add(game);
                    }
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
