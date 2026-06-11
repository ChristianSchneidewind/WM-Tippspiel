using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TippSpiel.Data;
using TippSpiel.Helpers;
using TippSpiel.Models;

namespace TippSpiel.Services;

public class KnockoutProgressionService
{
    private static readonly Regex GroupPattern = new(@"^(?<position>[123])\. Gruppe (?<groups>[A-Z](?:/[A-Z])*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex WinnerPattern = new(@"^Sieger aus Spiel #(?<match>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LoserPattern = new(@"^Verlierer aus Spiel #(?<match>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

    private readonly GroupStandingsService _groupStandingsService;

    public KnockoutProgressionService(GroupStandingsService groupStandingsService)
    {
        _groupStandingsService = groupStandingsService;
    }

    public async Task SyncAsync(ApplicationDbContext db)
    {
        var groups = await db.Groups.AsNoTracking().ToListAsync();
        var standingsByGroup = BuildStandingsByGroup(groups);
        var thirdPlaceAssignments = BuildThirdPlaceAssignments(standingsByGroup);

        var knockoutGames = await db.Games
            .Where(g => g.MatchNumber >= 73 && g.MatchNumber <= 104)
            .ToListAsync();

        var gamesByMatch = knockoutGames
            .Where(g => g.MatchNumber.HasValue)
            .ToDictionary(g => g.MatchNumber!.Value, g => g);

        var cache = new Dictionary<(int MatchNumber, bool IsHome), int?>();
        var changed = false;

        foreach (var game in knockoutGames.OrderBy(g => g.MatchNumber ?? int.MaxValue))
        {
            if (!game.MatchNumber.HasValue)
                continue;

            var matchNumber = game.MatchNumber.Value;
            var homeTeamId = ResolveTeamId(matchNumber, isHome: true, gamesByMatch, standingsByGroup, thirdPlaceAssignments, cache);
            var awayTeamId = ResolveTeamId(matchNumber, isHome: false, gamesByMatch, standingsByGroup, thirdPlaceAssignments, cache);

            if (game.HomeTeamId != homeTeamId)
            {
                game.HomeTeamId = homeTeamId;
                changed = true;
            }

            if (game.AwayTeamId != awayTeamId)
            {
                game.AwayTeamId = awayTeamId;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    private Dictionary<string, IReadOnlyList<GroupStanding>> BuildStandingsByGroup(IEnumerable<TippSpiel.Models.Group> groups)
    {
        var result = new Dictionary<string, IReadOnlyList<GroupStanding>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var letter = ExtractGroupLetter(group.Name);
            if (string.IsNullOrWhiteSpace(letter))
                continue;

            var standings = _groupStandingsService.CalculateGroupStandings(group.Id);
            if (standings.Count == 0)
                continue;

            result[letter] = standings;
        }

        return result;
    }

    private Dictionary<int, GroupStanding> BuildThirdPlaceAssignments(IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup)
    {
        var candidates = standingsByGroup
            .Select(kvp => new
            {
                GroupLetter = kvp.Key,
                Standing = kvp.Value.ElementAtOrDefault(2)
            })
            .Where(x => x.Standing?.Team != null)
            .Select(x => new ThirdPlaceCandidate(x.GroupLetter, x.Standing!))
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

        var slot = slots[slotIndex];
        if (!ThirdPlaceSlotRules.TryGetValue(slot, out var allowedGroups))
            return false;

        foreach (var candidate in candidates)
        {
            if (usedGroups.Contains(candidate.GroupLetter))
                continue;

            if (!allowedGroups.Contains(candidate.GroupLetter, StringComparer.OrdinalIgnoreCase))
                continue;

            assignment[slot] = candidate;
            usedGroups.Add(candidate.GroupLetter);

            if (AssignThirdPlaceSlots(slots, slotIndex + 1, candidates, assignment, usedGroups))
                return true;

            usedGroups.Remove(candidate.GroupLetter);
            assignment.Remove(slot);
        }

        return false;
    }

    private int? ResolveTeamId(
        int matchNumber,
        bool isHome,
        IReadOnlyDictionary<int, Game> gamesByMatch,
        IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup,
        IReadOnlyDictionary<int, GroupStanding> thirdPlaceAssignments,
        Dictionary<(int MatchNumber, bool IsHome), int?> cache)
    {
        if (cache.TryGetValue((matchNumber, isHome), out var cached))
            return cached;

        var placeholder = GameHelper.GetPlaceholderName(matchNumber, isHome);
        int? resolved = null;

        if (!string.IsNullOrWhiteSpace(placeholder) &&
            !placeholder.Equals("TBD", StringComparison.OrdinalIgnoreCase) &&
            !placeholder.Equals("Noch offen", StringComparison.OrdinalIgnoreCase))
        {
            resolved = ResolveFromPlaceholder(matchNumber, isHome, placeholder, gamesByMatch, standingsByGroup, thirdPlaceAssignments, cache);
        }

        cache[(matchNumber, isHome)] = resolved;
        return resolved;
    }

    private int? ResolveFromPlaceholder(
        int matchNumber,
        bool isHome,
        string placeholder,
        IReadOnlyDictionary<int, Game> gamesByMatch,
        IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup,
        IReadOnlyDictionary<int, GroupStanding> thirdPlaceAssignments,
        Dictionary<(int MatchNumber, bool IsHome), int?> cache)
    {
        var groupMatch = GroupPattern.Match(placeholder);
        if (groupMatch.Success)
        {
            var position = int.Parse(groupMatch.Groups["position"].Value);
            var groups = groupMatch.Groups["groups"].Value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (position == 3)
            {
                if (thirdPlaceAssignments.TryGetValue(matchNumber, out var thirdPlaceStanding) && thirdPlaceStanding.Team != null)
                    return thirdPlaceStanding.Team.Id;

                return null;
            }

            foreach (var groupLetter in groups)
            {
                if (!standingsByGroup.TryGetValue(groupLetter, out var standings))
                    continue;

                var index = position - 1;
                if (index < 0 || index >= standings.Count)
                    continue;

                var team = standings[index].Team;
                if (team != null)
                    return team.Id;
            }

            return null;
        }

        var winnerMatch = WinnerPattern.Match(placeholder);
        if (winnerMatch.Success)
        {
            var sourceMatch = int.Parse(winnerMatch.Groups["match"].Value);
            return ResolveOutcomeTeamId(sourceMatch, winner: true, gamesByMatch, standingsByGroup, thirdPlaceAssignments, cache);
        }

        var loserMatch = LoserPattern.Match(placeholder);
        if (loserMatch.Success)
        {
            var sourceMatch = int.Parse(loserMatch.Groups["match"].Value);
            return ResolveOutcomeTeamId(sourceMatch, winner: false, gamesByMatch, standingsByGroup, thirdPlaceAssignments, cache);
        }

        return null;
    }

    private int? ResolveOutcomeTeamId(
    int sourceMatch,
    bool winner,
    IReadOnlyDictionary<int, Game> gamesByMatch,
    IReadOnlyDictionary<string, IReadOnlyList<GroupStanding>> standingsByGroup,
    IReadOnlyDictionary<int, GroupStanding> thirdPlaceAssignments,
    Dictionary<(int MatchNumber, bool IsHome), int?> cache)
    {
        if (!gamesByMatch.TryGetValue(sourceMatch, out var sourceGame))
            return null;

        if (!sourceGame.HomeTeamScore.HasValue || !sourceGame.AwayTeamScore.HasValue)
            return null;

        var homeTeamId = sourceGame.HomeTeamId ?? ResolveTeamId(sourceMatch, true, gamesByMatch, standingsByGroup, thirdPlaceAssignments, cache);
        var awayTeamId = sourceGame.AwayTeamId ?? ResolveTeamId(sourceMatch, false, gamesByMatch, standingsByGroup, thirdPlaceAssignments, cache);

        if (!homeTeamId.HasValue || !awayTeamId.HasValue)
            return null;

        // 1. Logik für ein normales Spielergebnis (Entscheidung nach 90 oder 120 Minuten)
        if (sourceGame.HomeTeamScore != sourceGame.AwayTeamScore)
        {
            var homeWonNormal = sourceGame.HomeTeamScore > sourceGame.AwayTeamScore;
            return winner
                ? (homeWonNormal ? homeTeamId : awayTeamId)
                : (homeWonNormal ? awayTeamId : homeTeamId);
        }

        // 2. Fallback-Logik für Gleichstand -> Entscheidung im Elfmeterschießen
        if (sourceGame.HomeTeamPenaltyScore.HasValue && sourceGame.AwayTeamPenaltyScore.HasValue)
        {
            // Falls das Elfmeterschießen unentschieden eingetragen wurde (Fehlerfallabsicherung)
            if (sourceGame.HomeTeamPenaltyScore.Value == sourceGame.AwayTeamPenaltyScore.Value)
                return null;

            var homeWonPenalties = sourceGame.HomeTeamPenaltyScore.Value > sourceGame.AwayTeamPenaltyScore.Value;
            return winner
                ? (homeWonPenalties ? homeTeamId : awayTeamId)
                : (homeWonPenalties ? awayTeamId : homeTeamId);
        }

        // Das Spiel steht Unentschieden, aber es wurde noch kein Elfmeterschießen eingetragen (z.B. während das Spiel läuft)
        return null;
    }

    private static string? ExtractGroupLetter(string? groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return null;

        var trimmed = groupName.Trim();
        var match = Regex.Match(trimmed, @"([A-L])$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private sealed record ThirdPlaceCandidate(string GroupLetter, GroupStanding Standing);
}
