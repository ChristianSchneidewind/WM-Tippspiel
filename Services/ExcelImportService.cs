using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Models;
using System.Text.RegularExpressions;
using Group = TippSpiel.Models.Group;

namespace TippSpiel.Services
{
    public class ExcelImportService
    {
        private readonly ApplicationDbContext _context;

        public ExcelImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ImportScheduleFromExcelAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[EXCEL ERROR] Datei nicht gefunden unter: {filePath}");
                return;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");

            try
            {
                // Kopiere die Datei an einen temporären Ort, um Locks zu umgehen
                File.Copy(filePath, tempPath, true);

                using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                using var workbook = new XLWorkbook(stream);
                
                // Nutze das "Matches" Sheet, das eine saubere Liste enthält
                var worksheet = workbook.Worksheet("Matches");
                var rows = worksheet.RangeUsed().RowsUsed().Skip(2); // Überspringe "Spiele" (Zeile 1) und Header (Zeile 3) -> Start bei Zeile 4 (Match 1)

                // --- KONFIGURATION FÜR SHEET "Matches" ---
                const int colMatchNr = 1; // Spalte A: Match No.
                const int colGroupInfo = 2; // Spalte B: A1, B2 etc.
                const int colKickOff = 4; // Spalte D: KickOff (local)
                const int colHome = 8;    // Spalte H: Team 1
                const int colAway = 9;    // Spalte I: Team 2

                int count = 0;
                foreach (var row in rows)
                {
                    // 0. Match-Nummer
                    var matchNrRaw = row.Cell(colMatchNr).GetValue<string>() ?? "";
                    if (!int.TryParse(matchNrRaw, out int matchNr)) continue;

                    // 1. KickOff
                    DateTime kickOffDate;
                    if (!row.Cell(colKickOff).TryGetValue(out kickOffDate))
                    {
                        if (!DateTime.TryParse(row.Cell(colKickOff).GetValue<string>(), out kickOffDate))
                        {
                            kickOffDate = DateTime.Now;
                        }
                    }
                    var kickOff = new DateTimeOffset(kickOffDate, TimeSpan.Zero);

                    // 2. Gruppe (Erster Buchstabe aus Spalte B, z.B. "A" aus "A1")
                    var groupInfo = row.Cell(colGroupInfo).GetValue<string>()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(groupInfo) || !char.IsLetter(groupInfo[0])) continue;
                    
                    var groupLetter = groupInfo[0].ToString().ToUpper();
                    var groupName = "Gruppe " + groupLetter;

                    // 3. Teams
                    var homeName = row.Cell(colHome).GetValue<string>()?.Trim() ?? "";
                    var awayName = row.Cell(colAway).GetValue<string>()?.Trim() ?? "";

                    if (string.IsNullOrEmpty(homeName) || string.IsNullOrEmpty(awayName)) continue;

                    // 1. Gruppe sicherstellen
                    var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
                    if (group == null)
                    {
                        group = new Group { Name = groupName };
                        _context.Groups.Add(group);
                        await _context.SaveChangesAsync();
                    }

                    // 2. Teams sicherstellen
                    var homeTeam = await GetOrCreateTeam(homeName);
                    var awayTeam = await GetOrCreateTeam(awayName);

                    // 3. Spiel suchen oder anlegen
                    var game = await _context.Games.FirstOrDefaultAsync(g => g.MatchNumber == matchNr);
                    if (game == null)
                    {
                        game = new Game
                        {
                            MatchNumber = matchNr,
                            GroupId = group.Id,
                            HomeTeamId = homeTeam?.Id,
                            AwayTeamId = awayTeam?.Id,
                            KickOff = kickOff
                        };
                        _context.Games.Add(game);
                    }
                    else
                    {
                        game.HomeTeamId = homeTeam?.Id;
                        game.AwayTeamId = awayTeam?.Id;
                        game.KickOff = kickOff;
                        game.GroupId = group.Id;
                    }
                    count++;
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"[EXCEL SUCCESS] {count} Spiele erfolgreich importiert.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEL ERROR] Fehler beim Verarbeiten der Excel-Datei: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { /* Ignore */ }
                }
            }
        }

        private async Task<Team?> GetOrCreateTeam(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains("Play-off") || name.Contains("Winner") || name.Contains("1st Group")) 
                return null;

            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == name);
            if (team == null)
            {
                team = new Team { Name = name };
                _context.Teams.Add(team);
                await _context.SaveChangesAsync();
            }
            return team;
        }
    }
}