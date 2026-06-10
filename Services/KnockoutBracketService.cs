using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Helpers;
using TippSpiel.Models;
using TippSpiel.Models.ViewModels;

namespace TippSpiel.Services;

public class KnockoutBracketService
{
    private static readonly IReadOnlyList<(string Name, int Start, int End)> Rounds = new[]
    {
        ("Sechzehntelfinale", 73, 88),
        ("Achtelfinale", 89, 96),
        ("Viertelfinale", 97, 100),
        ("Halbfinale", 101, 102),
        ("Spiel um Platz 3", 103, 103),
        ("Finale", 104, 104)
    };

    private static readonly IReadOnlyDictionary<int, DateTimeOffset> MatchSchedule = new Dictionary<int, DateTimeOffset>
    {
        [73] = new DateTimeOffset(2026, 6, 28, 19, 0, 0, TimeSpan.Zero),
        [74] = new DateTimeOffset(2026, 6, 29, 16, 0, 0, TimeSpan.Zero),
        [75] = new DateTimeOffset(2026, 6, 29, 19, 0, 0, TimeSpan.Zero),
        [76] = new DateTimeOffset(2026, 6, 29, 22, 0, 0, TimeSpan.Zero),
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
        [89] = new DateTimeOffset(2026, 7, 4, 16, 0, 0, TimeSpan.Zero),
        [90] = new DateTimeOffset(2026, 7, 4, 19, 0, 0, TimeSpan.Zero),
        [91] = new DateTimeOffset(2026, 7, 5, 16, 0, 0, TimeSpan.Zero),
        [92] = new DateTimeOffset(2026, 7, 5, 19, 0, 0, TimeSpan.Zero),
        [93] = new DateTimeOffset(2026, 7, 6, 16, 0, 0, TimeSpan.Zero),
        [94] = new DateTimeOffset(2026, 7, 6, 19, 0, 0, TimeSpan.Zero),
        [95] = new DateTimeOffset(2026, 7, 7, 16, 0, 0, TimeSpan.Zero),
        [96] = new DateTimeOffset(2026, 7, 7, 19, 0, 0, TimeSpan.Zero),
        [97] = new DateTimeOffset(2026, 7, 9, 19, 0, 0, TimeSpan.Zero),
        [98] = new DateTimeOffset(2026, 7, 10, 19, 0, 0, TimeSpan.Zero),
        [99] = new DateTimeOffset(2026, 7, 11, 16, 0, 0, TimeSpan.Zero),
        [100] = new DateTimeOffset(2026, 7, 11, 19, 0, 0, TimeSpan.Zero),
        [101] = new DateTimeOffset(2026, 7, 14, 19, 0, 0, TimeSpan.Zero),
        [102] = new DateTimeOffset(2026, 7, 15, 19, 0, 0, TimeSpan.Zero),
        [103] = new DateTimeOffset(2026, 7, 18, 19, 0, 0, TimeSpan.Zero),
        [104] = new DateTimeOffset(2026, 7, 19, 19, 0, 0, TimeSpan.Zero)
    };

    private static readonly IReadOnlyDictionary<int, string[]> ThirdPlaceSlotRules = new Dictionary<int, string[]>
    {
        [74] = ["C", "E", "F"],
        [75] = ["A", "C", "D"],
        [76] = ["A", "B", "F"],
        [79] = ["A", "B", "D"],
        [80] = ["B", "E", "F"],
        [81] = ["A", "B", "C"],
        [83] = ["D", "E", "G"],
        [87] = ["G", "H", "I"]
    };

    private static readonly IReadOnlyDictionary<int, GroupPlacement> RoundOf32Placements = new Dictionary<int, GroupPlacement>
    {
        [73] = new GroupPlacement("A", 2, "B", 2),
        [74] = new GroupPlacement("A", 1, null, null),
        [75] = new GroupPlacement("B", 1, null, null),
        [76] = new GroupPlacement("C", 1, null, null),
        [77] = new GroupPlacement("F", 1, "C", 2),
        [78] = new GroupPlacement("E", 2, "F", 2),
        [79] = new GroupPlacement("E", 1, null, null),
        [80] = new GroupPlacement("D", 1, null, null),
        [81] = new GroupPlacement("G", 1, null, null),
        [82] = new GroupPlacement("H", 1, "G", 2),
        [83] = new GroupPlacement("I", 1, null, null),
        [84] = new GroupPlacement("J", 1, "I", 2),
        [85] = new GroupPlacement("L", 1, "K", 2),
        [86] = new GroupPlacement("L", 2, "H", 2),
        [87] = new GroupPlacement("K", 1, null, null),
        [88] = new GroupPlacement("J", 2, "D", 2)
    };

    private readonly GroupStandingsService _groupStandingsService;

    public KnockoutBracketService(GroupStandingsService groupStandingsService)
    {
        _groupStandingsService = groupStandingsService;
    }

    public async Task<IReadOnlyList<KnockoutRoundViewModel>> BuildAsync(ApplicationDbContext db)
    {
        var groups = await db.Groups.AsNoTracking().ToListAsync();
        var standingsByGroup = await LoadStandingsAsync(groups);
        var thirdPlaceAssignments = BuildThirdPlaceAssignments(standingsByGroup);

        var knockoutGames = await db.Games
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .Include(g => g.Group)
            .Where(g => g.MatchNumber >= 73 && g.MatchNumber <= 104)
            .AsNoTracking()
            .ToListAsync();

        var gamesByMatch = knockoutGames
            .Where(g => g.MatchNumber.HasValue)
            .ToDictionary(g => g.MatchNumber!.Value, g => g);

        var rounds = new List<KnockoutRoundViewModel>();

        foreach (var round in Rounds)
        {
            var matchList = new List<Game>();

            for (var matchNumber = round.Start; matchNumber <= round.End; matchNumber++)
            {
                var game = gamesByMatch.TryGetValue(matchNumber, out var existing)
                    ? existing
                    : new Game
                    {
                        MatchNumber = matchNumber,
                        KickOff = MatchSchedule.TryGetValue(matchNumber, out var kickoff) ? kickoff : default,
                        Venue = string.Empty
                    };

                PopulateResolvedTeams(game, gamesByMatch, standingsByGroup, thirdPlaceAssignments);
                matchList.Add(game);
            }

            rounds.Add(new KnockoutRoundViewModel
            {
                Name = round.Name,
                Games = matchList
            });
        }

        return rounds;
    }

    private Task<Dictionary<string, IReadOnlyList<GroupStanding>>> LoadStandingsAsync(IEnumerable<Group> groups)
    {
        var result = new Dictionary<string, IReadOnlyList<GroupStanding>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            if (string.IsNullOrWhiteSpace(group.Name) || group.Name.Contains("Finalrunde", StringComparison.OrdinalIgnoreCase))
                continue;

            var standings = _groupStandingsService.CalculateGroupStandings(group.Id);
            if (standings.Count == 0)
                continue;

            result[group.Name.Trim()] = standings;
        }

        return Task.FromResult(result);
    }

    private void PopulateResolvedTeams(Game game, IReadOnlyDictionary<int, Game> gamesByMatch, IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup, IReadOnlyDictionary<int, GroupStanding> thirdPlaceAssignments)
    {
        if (!game.MatchNumber.HasValue)
            return;

        var matchNumber = game.MatchNumber.Value;

        if (game.HomeTeam == null)
        {
            var homeName = ResolveTeamName(matchNumber, isHome: true, gamesByMatch, standingsByGroup, thirdPlaceAssignments);
            if (!string.IsNullOrWhiteSpace(homeName))
            {
                game.HomeTeam = new Team { Name = homeName };
            }
        }

        if (game.AwayTeam == null)
        {
            var awayName = ResolveTeamName(matchNumber, isHome: false, gamesByMatch, standingsByGroup, thirdPlaceAssignments);
            if (!string.IsNullOrWhiteSpace(awayName))
            {
                game.AwayTeam = new Team { Name = awayName };
            }
        }
    }

    private string? ResolveTeamName(int matchNumber, bool isHome, IReadOnlyDictionary<int, Game> gamesByMatch, IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup, IReadOnlyDictionary<int, GroupStanding> thirdPlaceAssignments)
    {
        if (matchNumber >= 73 && matchNumber <= 88)
        {
            return ResolveRoundOf32Team(matchNumber, isHome, standingsByGroup, thirdPlaceAssignments);
        }

        var source = ResolveSource(matchNumber, isHome);
        if (source == null)
            return null;

        return ResolveMatchOutcome(source.Value.SourceMatch, source.Value.Winner, gamesByMatch, standingsByGroup, thirdPlaceAssignments);
    }

    private string? ResolveRoundOf32Team(int matchNumber, bool isHome, IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup, IReadOnlyDictionary<int, GroupStanding> thirdPlaceAssignments)
    {
        if (!RoundOf32Placements.TryGetValue(matchNumber, out var placement))
            return null;

        var groupLetter = isHome ? placement.HomeGroupLetter : placement.AwayGroupLetter;
        var position = isHome ? placement.HomePosition : placement.AwayPosition;

        if (string.IsNullOrWhiteSpace(groupLetter) || !position.HasValue)
            return null;

        if (position.Value == 3)
        {
            if (thirdPlaceAssignments.TryGetValue(matchNumber, out var thirdPlaceTeam))
            {
                return GameHelper.FixTeamName(thirdPlaceTeam.Team?.Name ?? string.Empty);
            }

            return null;
        }

        var groupName = $"Gruppe {groupLetter}";
        if (!standingsByGroup.TryGetValue(groupName, out var standings))
            return null;

        var index = position.Value - 1;
        if (index < 0 || index >= standings.Count)
            return null;

        return GameHelper.FixTeamName(standings[index].Team?.Name ?? string.Empty);
    }

    private string? ResolveMatchOutcome(int sourceMatchNumber, bool winner, IReadOnlyDictionary<int, Game> gamesByMatch, IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup, IReadOnlyDictionary<int, GroupStanding> thirdPlaceAssignments)
    {
        if (!gamesByMatch.TryGetValue(sourceMatchNumber, out var sourceGame))
            return null;

        if (!sourceGame.HomeTeamScore.HasValue || !sourceGame.AwayTeamScore.HasValue)
            return null;

        var homeName = ResolveTeamName(sourceMatchNumber, true, gamesByMatch, standingsByGroup, thirdPlaceAssignments);
        var awayName = ResolveTeamName(sourceMatchNumber, false, gamesByMatch, standingsByGroup, thirdPlaceAssignments);

        if (string.IsNullOrWhiteSpace(homeName) || string.IsNullOrWhiteSpace(awayName))
            return null;

        if (sourceGame.HomeTeamScore == sourceGame.AwayTeamScore)
            return null;

        var homeWon = sourceGame.HomeTeamScore > sourceGame.AwayTeamScore;
        return winner
            ? (homeWon ? homeName : awayName)
            : (homeWon ? awayName : homeName);
    }

    private (int SourceMatch, bool Winner)? ResolveSource(int matchNumber, bool isHome)
    {
        return matchNumber switch
        {
            89 => isHome ? (74, true) : (77, true),
            90 => isHome ? (73, true) : (75, true),
            91 => isHome ? (76, true) : (78, true),
            92 => isHome ? (79, true) : (80, true),
            93 => isHome ? (83, true) : (84, true),
            94 => isHome ? (81, true) : (82, true),
            95 => isHome ? (86, true) : (88, true),
            96 => isHome ? (85, true) : (87, true),
            97 => isHome ? (89, true) : (90, true),
            98 => isHome ? (93, true) : (94, true),
            99 => isHome ? (91, true) : (92, true),
            100 => isHome ? (95, true) : (96, true),
            101 => isHome ? (97, true) : (98, true),
            102 => isHome ? (99, true) : (100, true),
            103 => isHome ? (101, false) : (102, false),
            104 => isHome ? (101, true) : (102, true),
            _ => null
        };
    }

    private Dictionary<int, GroupStanding> BuildThirdPlaceAssignments(IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup)
    {
        var candidates = standingsByGroup
            .Select(kvp => new
            {
                GroupLetter = ExtractGroupLetter(kvp.Key),
                Standing = kvp.Value.ElementAtOrDefault(2)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.GroupLetter) && x.Standing?.Team != null)
            .Select(x => new ThirdPlaceCandidate(x.GroupLetter!, x.Standing!))
            .OrderByDescending(x => x.Standing.Points)
            .ThenByDescending(x => x.Standing.GoalDifference)
            .ThenByDescending(x => x.Standing.GoalsFor)
            .ThenBy(x => x.GroupLetter)
            .ToList();

        var assignment = new Dictionary<int, ThirdPlaceCandidate>();
        var usedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slots = ThirdPlaceSlotRules.Keys.OrderBy(x => x).ToList();

        if (!AssignThirdPlaceSlots(slots, 0, candidates, assignment, usedGroups))
            return new Dictionary<int, GroupStanding>();

        return assignment.ToDictionary(x => x.Key, x => x.Value.Standing);
    }

    private bool AssignThirdPlaceSlots(
        IReadOnlyList<int> slots,
        int slotIndex,
        IReadOnlyList<ThirdPlaceCandidate> candidates,
        Dictionary<int, ThirdPlaceCandidate> assignment,
        HashSet<string> usedGroups)
    {
        if (slotIndex >= slots.Count)
            return true;

        var nextSlot = slots[slotIndex];
        if (!ThirdPlaceSlotRules.TryGetValue(nextSlot, out var allowedGroups))
            return false;

        foreach (var candidate in candidates)
        {
            if (usedGroups.Contains(candidate.GroupLetter))
                continue;

            if (!allowedGroups.Contains(candidate.GroupLetter, StringComparer.OrdinalIgnoreCase))
                continue;

            assignment[nextSlot] = candidate;
            usedGroups.Add(candidate.GroupLetter);

            if (AssignThirdPlaceSlots(slots, slotIndex + 1, candidates, assignment, usedGroups))
                return true;

            usedGroups.Remove(candidate.GroupLetter);
            assignment.Remove(nextSlot);
        }

        return false;
    }

    private static string? ExtractGroupLetter(string? groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return null;

        var trimmed = groupName.Trim();
        var lastLetter = trimmed.LastOrDefault(char.IsLetter);
        return lastLetter == default ? null : lastLetter.ToString().ToUpperInvariant();
    }

    private sealed record GroupPlacement(string HomeGroupLetter, int? HomePosition, string? AwayGroupLetter, int? AwayPosition);
    private sealed record ThirdPlaceCandidate(string GroupLetter, GroupStanding Standing);
}
