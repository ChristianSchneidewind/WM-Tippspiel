using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using TippSpiel.Models;

namespace TippSpiel.Data
{
    public static class ExcelSeeder
    {
        public static Dictionary<int, string> SeedFromExcel(ApplicationDbContext db, string filePath)
        {
            var venueMap = new Dictionary<int, string>();
            if (!File.Exists(filePath))
            {
                return venueMap;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
            });

            // Venues immer laden, auch wenn wir beim Seeding überspringen
            venueMap = LoadVenues(dataSet);

            // Falls schon Daten da sind, stoppen wir beim Seeding (verhindert Duplikate)
            if (db.Groups.Any() || db.Games.Any())
            {
                return venueMap;
            }

            // 1. Gruppenzuordnung aus dem Excel-Tab "Groups" laden
            var teamToGroup = LoadGroups(dataSet);
            if (teamToGroup.Count == 0)
            {
                return venueMap;
            }

            // 3. Gruppen-Lookup initialisieren
            var groupLookup = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);

            // 4. Spiele aus dem Tab "Matches" laden und dabei Gruppen dynamisch erstellen
            var games = LoadMatches(db, dataSet, teamToGroup, groupLookup, venueMap);

            if (games.Count > 0)
            {
                db.Games.AddRange(games);
                db.SaveChanges();
            }

            return venueMap;
        }

        public static Dictionary<int, string> LoadVenues(DataSet dataSet)
        {
            var result = new Dictionary<int, string>();
            var table = FindTable(dataSet, "Tagesplan");
            if (table == null)
            {
                Console.WriteLine("[EXCEL SEEDER] Tabelle 'Tagesplan' nicht gefunden.");
                return result;
            }

            // Suche Header-Zeile (Nr. und Austragungsort)
            var headerIndex = FindHeaderRow(table, "Nr.", "Austragungsort");
            if (headerIndex < 0)
            {
                Console.WriteLine("[EXCEL SEEDER] Header 'Nr.' oder 'Austragungsort' in 'Tagesplan' nicht gefunden.");
                return result;
            }

            var headerRow = table.Rows[headerIndex];
            var nrCol = FindColumnIndex(headerRow, "Nr.");
            var venueCol = FindColumnIndex(headerRow, "Austragungsort");

            if (nrCol < 0 || venueCol < 0) return result;

            for (var i = headerIndex + 1; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var nrStr = GetCell(row, nrCol);
                var venue = GetCell(row, venueCol);

                if (int.TryParse(nrStr, out var nr) && !string.IsNullOrWhiteSpace(venue))
                {
                    result[nr] = venue;
                }
            }

            Console.WriteLine($"[EXCEL SEEDER] {result.Count} Austragungsorte aus 'Tagesplan' geladen.");
            return result;
        }

        private static DataTable? FindTable(DataSet dataSet, string name)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                if (string.Equals(table.TableName?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return table;
                }
            }
            return null;
        }

        private static Dictionary<string, string> LoadGroups(DataSet dataSet)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!dataSet.Tables.Contains("Groups"))
            {
                return result;
            }

            var table = dataSet.Tables["Groups"];
            if (table == null)
            {
                Console.WriteLine("[EXCEL ERROR] Tabelle 'Groups' nicht gefunden.");
                return result;
            }

            var headerIndex = FindHeaderRow(table, "Team", "Name");
            if (headerIndex < 0)
            {
                return result;
            }

            var headerRow = table.Rows[headerIndex];
            var teamCol = FindColumnIndex(headerRow, "Team");
            var nameCol = FindColumnIndex(headerRow, "Name");

            if (teamCol < 0 || nameCol < 0)
            {
                return result;
            }

            for (var i = headerIndex + 1; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var teamCode = GetCell(row, teamCol);
                var teamName = GetCell(row, nameCol);

                if (string.IsNullOrWhiteSpace(teamCode) || string.IsNullOrWhiteSpace(teamName))
                {
                    continue;
                }

                // Extrahiert den Gruppenbuchstaben (z.B. "A" aus "A1")
                var groupName = teamCode.Trim().Substring(0, 1).ToUpperInvariant();
                if (!result.ContainsKey(teamName))
                {
                    result[teamName] = groupName;
                }
            }

            return result;
        }

        private static List<Game> LoadMatches(ApplicationDbContext db, DataSet dataSet, Dictionary<string, string> teamToGroup, Dictionary<string, Group> groupLookup, Dictionary<int, string> matchIdToVenue)
        {
            var games = new List<Game>();
            if (!dataSet.Tables.Contains("Matches"))
            {
                return games;
            }

            var table = dataSet.Tables["Matches"];
            if (table == null)
            {
                Console.WriteLine("[EXCEL ERROR] Tabelle 'Matches' nicht gefunden.");
                return games;
            }

            var headerIndex = FindHeaderRow(table, "Match No.", "Team 1", "Team 2");
            if (headerIndex < 0)
            {
                return games;
            }

            var headerRow = table.Rows[headerIndex];
            var matchNoCol = FindColumnIndex(headerRow, "Match No.");
            var teamsCol = FindColumnIndex(headerRow, "Teams");
            var team1Col = FindColumnIndex(headerRow, "Team 1");
            var team2Col = FindColumnIndex(headerRow, "Team 2");

            // Suche Datums-Spalte (flexibel wegen deiner Leerzeichen-Thematik)
            var dateCol = FindColumnIndex(headerRow, "Date");

            for (var i = headerIndex + 1; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var team1 = GetCell(row, team1Col);
                var team2 = GetCell(row, team2Col);

                if (string.IsNullOrWhiteSpace(team1) || string.IsNullOrWhiteSpace(team2))
                {
                    continue;
                }

                var kickOff = ParseExcelDate(row[dateCol]);
                var teamsCode = GetCell(row, teamsCol);
                var matchNumber = ParseMatchNumber(GetCell(row, matchNoCol));

                // Bestimme den Gruppennamen (A, B... oder Achtelfinale etc.)
                var groupName = ResolveGroupName(teamToGroup, team1, team2, teamsCode);

                // Nutze die neue dynamische Gruppen-Logik
                var group = GetOrCreateGroup(db, groupName, groupLookup);

                var homeTeam = GetOrCreateTeam(db, team1);
                var awayTeam = GetOrCreateTeam(db, team2);

                var venue = string.Empty;
                if (matchNumber.HasValue && matchIdToVenue.TryGetValue(matchNumber.Value, out var v))
                {
                    venue = v;
                }

                games.Add(new Game
                {
                    HomeTeamId = homeTeam.Id,
                    AwayTeamId = awayTeam.Id,
                    KickOff = kickOff,
                    MatchNumber = matchNumber,
                    GroupId = group.Id,
                    Venue = venue
                });
            }

            return games;
        }

        private static Group GetOrCreateGroup(ApplicationDbContext db, string name, Dictionary<string, Group> lookup)
        {
            if (lookup.TryGetValue(name, out var group)) return group;

            var dbGroup = db.Groups.FirstOrDefault(g => g.Name == name);
            if (dbGroup == null)
            {
                dbGroup = new Group { Name = name };
                db.Groups.Add(dbGroup);
                db.SaveChanges(); // Wichtig für die ID
            }
            lookup[name] = dbGroup;
            return dbGroup;
        }

        private static Team GetOrCreateTeam(ApplicationDbContext db, string name)
        {
            var team = db.Teams.FirstOrDefault(t => t.Name == name);
            if (team == null)
            {
                team = new Team { Name = name };
                db.Teams.Add(team);
                db.SaveChanges();
            }
            return team;
        }

        private static string ResolveGroupName(Dictionary<string, string> teamToGroup, string team1, string team2, string teamsCode)
        {
            var groupFromCode = GetGroupFromTeamsCode(teamsCode);
            if (!string.IsNullOrWhiteSpace(groupFromCode))
            {
                return groupFromCode;
            }

            if (!string.IsNullOrWhiteSpace(teamsCode))
            {
                var code = teamsCode.Trim().ToUpperInvariant();
                if (code.Contains("R32")) return "Sechzehntelfinale";
                if (code.Contains("R16")) return "Achtelfinale";
                if (code.Contains("QF")) return "Viertelfinale";
                if (code.Contains("SF")) return "Halbfinale";
                if (code.Contains("F")) return "Finale";
            }

            if (teamToGroup.TryGetValue(team1, out var gName)) return gName;
            if (teamToGroup.TryGetValue(team2, out gName)) return gName;

            return "Finalrunde";
        }

        private static string GetGroupFromTeamsCode(string teamsCode)
        {
            if (string.IsNullOrWhiteSpace(teamsCode)) return string.Empty;
            var trimmed = teamsCode.Trim();
            if (trimmed.Length >= 2 && char.IsLetter(trimmed[0]) && char.IsDigit(trimmed[1]))
            {
                return trimmed[0].ToString().ToUpperInvariant();
            }
            return string.Empty;
        }

        private static int FindHeaderRow(DataTable table, params string[] requiredHeaders)
        {
            var cleanRequired = requiredHeaders.Select(h => h.Replace(" ", "").Replace(".", "").ToLowerInvariant()).ToList();
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var rowValues = row.ItemArray
                    .Select(cell => cell?.ToString()?.Replace(" ", "").Replace(".", "").ToLowerInvariant() ?? "")
                    .ToList();

                if (cleanRequired.All(header => rowValues.Any(value => value.Contains(header))))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FindColumnIndex(DataRow row, string columnName)
        {
            var cleanTarget = columnName.Replace(" ", "").Replace(".", "").ToLowerInvariant();
            for (var i = 0; i < row.ItemArray.Length; i++)
            {
                var value = row[i]?.ToString()?.Replace(" ", "").Replace(".", "").ToLowerInvariant() ?? "";
                if (value.Contains(cleanTarget))
                {
                    return i;
                }
            }
            return -1;
        }

        private static string GetCell(DataRow row, int index)
        {
            if (index < 0 || index >= row.ItemArray.Length)
            {
                return string.Empty;
            }
            return row[index]?.ToString()?.Trim() ?? string.Empty;
        }

        private static DateTimeOffset ParseExcelDate(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return default;
            }

            if (value is DateTime dateTime)
            {
                return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Local));
            }

            if (value is double d) return new DateTimeOffset(DateTime.FromOADate(d));

            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                return new DateTimeOffset(parsed);
            }

            return default;
        }

        private static int? ParseMatchNumber(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var matchNumber))
            {
                return matchNumber;
            }
            return null;
        }
    }
}