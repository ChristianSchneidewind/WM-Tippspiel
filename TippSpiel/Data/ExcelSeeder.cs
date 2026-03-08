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
        public static void SeedFromExcel(ApplicationDbContext db, string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            if (db.Groups.Any() || db.Games.Any())
            {
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
            });

            var teamToGroup = LoadGroups(dataSet);
            if (teamToGroup.Count == 0)
            {
                return;
            }

            var groups = teamToGroup.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .Select(name => new Group { Name = name })
                .ToList();

            var groupLookup = groups.ToDictionary(group => group.Name, StringComparer.OrdinalIgnoreCase);

            if (!groupLookup.ContainsKey("Finalrunde"))
            {
                var knockoutGroup = new Group { Name = "Finalrunde" };
                groups.Add(knockoutGroup);
                groupLookup[knockoutGroup.Name] = knockoutGroup;
            }

            db.Groups.AddRange(groups);
            db.SaveChanges();

            var games = LoadMatches(dataSet, teamToGroup, groupLookup);
            if (games.Count > 0)
            {
                db.Games.AddRange(games);
                db.SaveChanges();
            }
        }

        private static Dictionary<string, string> LoadGroups(DataSet dataSet)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!dataSet.Tables.Contains("Groups"))
            {
                return result;
            }

            var table = dataSet.Tables["Groups"];
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

                var groupName = teamCode.Trim().Substring(0, 1).ToUpperInvariant();
                if (!result.ContainsKey(teamName))
                {
                    result[teamName] = groupName;
                }
            }

            return result;
        }

        private static List<Game> LoadMatches(DataSet dataSet, Dictionary<string, string> teamToGroup, Dictionary<string, Group> groupLookup)
        {
            var games = new List<Game>();
            if (!dataSet.Tables.Contains("Matches"))
            {
                return games;
            }

            var table = dataSet.Tables["Matches"];
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
            var dateCol = FindColumnIndex(headerRow, "Date   (my time)");
            if (dateCol < 0)
            {
                dateCol = FindColumnIndex(headerRow, "Date   (local time host)");
            }

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

                var groupName = ResolveGroupName(teamToGroup, team1, team2, teamsCode);
                if (!groupLookup.TryGetValue(groupName, out var group))
                {
                    group = groupLookup["Finalrunde"];
                }

                games.Add(new Game
                {
                    HomeTeam = team1,
                    AwayTeam = team2,
                    KickOff = kickOff,
                    MatchNumber = matchNumber,
                    GroupId = group.Id
                });
            }

            return games;
        }

        private static string ResolveGroupName(Dictionary<string, string> teamToGroup, string team1, string team2, string teamsCode)
        {
            var groupFromCode = GetGroupFromTeamsCode(teamsCode);
            if (!string.IsNullOrWhiteSpace(groupFromCode))
            {
                return groupFromCode;
            }

            if (string.IsNullOrWhiteSpace(teamsCode))
            {
                if (teamToGroup.TryGetValue(team1, out var groupName))
                {
                    return groupName;
                }

                if (teamToGroup.TryGetValue(team2, out groupName))
                {
                    return groupName;
                }
            }

            return "Finalrunde";
        }

        private static string GetGroupFromTeamsCode(string teamsCode)
        {
            if (string.IsNullOrWhiteSpace(teamsCode))
            {
                return string.Empty;
            }

            var trimmed = teamsCode.Trim();
            if (trimmed.Length < 2)
            {
                return string.Empty;
            }

            var firstChar = trimmed[0];
            var secondChar = trimmed[1];
            if (char.IsLetter(firstChar) && char.IsDigit(secondChar))
            {
                return firstChar.ToString().ToUpperInvariant();
            }

            return string.Empty;
        }

        private static int FindHeaderRow(DataTable table, params string[] requiredHeaders)
        {
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var rowValues = row.ItemArray
                    .Select(cell => cell?.ToString()?.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                if (requiredHeaders.All(header => rowValues.Any(value => string.Equals(value, header, StringComparison.OrdinalIgnoreCase))))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindColumnIndex(DataRow row, string columnName)
        {
            for (var i = 0; i < row.ItemArray.Length; i++)
            {
                var value = row[i]?.ToString()?.Trim();
                if (string.Equals(value, columnName, StringComparison.OrdinalIgnoreCase))
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

            if (value is double doubleValue)
            {
                return new DateTimeOffset(DateTime.FromOADate(doubleValue));
            }

            if (value is float floatValue)
            {
                return new DateTimeOffset(DateTime.FromOADate(floatValue));
            }

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
