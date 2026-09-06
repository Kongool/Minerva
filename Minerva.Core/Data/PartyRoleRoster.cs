using System.Text.Json;

namespace Minerva;

/// <summary>
/// Turns an external roster into <see cref="PartyRolesConfig"/> entries.
/// <para>
/// Minerva inherited BossmodReborn's role-CONSUMING half — every component's <c>AddAIHints</c> takes an
/// <see cref="PartyRolesConfig.Assignment"/> — but none of its producing half: no auto-assign, no
/// priority tables, no config UI. Nothing wrote <c>Assignments</c>, so every member resolved to
/// <see cref="PartyRolesConfig.Assignment.Unassigned"/> and anything keyed on a role — tower soaks,
/// tether pairs — silently did nothing.
/// </para>
/// <para>
/// The rules live here, in the game-free core, so they can be tested without a Dalamud host; the IPC
/// plumbing that feeds them lives in the plugin.
/// </para>
/// </summary>
public static class PartyRoleRoster
{
    /// <summary>
    /// Roster JSON to content-id/role pairs. Entries without a usable content id or a recognised role
    /// are skipped, which is how a member the producer could not place — a third tank, an unknown job —
    /// arrives as simply absent.
    /// <para>
    /// A role claimed by TWO members is dropped from both. That only happens when the two sides
    /// disagree about the party (a stale entry, a toon mid-swap), and picking one would send two
    /// players to the same tower — the one outcome worse than sending nobody.
    /// </para>
    /// </summary>
    public static Dictionary<ulong, PartyRolesConfig.Assignment> Parse(string json)
    {
        var seen = new Dictionary<ulong, PartyRolesConfig.Assignment>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return seen;

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;
            if (!entry.TryGetProperty("contentId", out var cid) || !cid.TryGetUInt64(out var contentId) || contentId == 0)
                continue;
            if (!entry.TryGetProperty("role", out var role) || role.ValueKind != JsonValueKind.String)
                continue;
            if (ParseAssignment(role.GetString()) is not { } assignment)
                continue;

            seen.TryAdd(contentId, assignment);
        }

        var used = new HashSet<PartyRolesConfig.Assignment>();
        var duplicated = new HashSet<PartyRolesConfig.Assignment>();
        foreach (var a in seen.Values)
            if (!used.Add(a))
                duplicated.Add(a);

        if (duplicated.Count > 0)
            foreach (var contentId in new List<ulong>(seen.Keys))
                if (duplicated.Contains(seen[contentId]))
                    seen.Remove(contentId);

        return seen;
    }

    /// <summary>
    /// The eight standard slots. Anything else — including the empty string a producer sends for a
    /// member it could not place — is not a role, and the modules already default to Unassigned.
    /// </summary>
    public static PartyRolesConfig.Assignment? ParseAssignment(string? role) => role switch
    {
        "MT" => PartyRolesConfig.Assignment.MT,
        "OT" => PartyRolesConfig.Assignment.OT,
        "H1" => PartyRolesConfig.Assignment.H1,
        "H2" => PartyRolesConfig.Assignment.H2,
        "M1" => PartyRolesConfig.Assignment.M1,
        "M2" => PartyRolesConfig.Assignment.M2,
        "R1" => PartyRolesConfig.Assignment.R1,
        "R2" => PartyRolesConfig.Assignment.R2,
        _ => null,
    };
}
