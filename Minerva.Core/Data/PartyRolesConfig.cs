namespace Minerva;

/// <summary>
/// Maps party members to the eight standard role slots (MT/OT/H1/H2/M1/M2/R1/R2). Ported modules take
/// a <see cref="Assignment"/> in <c>AddAIHints</c> so role-based positioning logic compiles unchanged;
/// Minerva does not yet expose the config UI, so members default to <see cref="Assignment.Unassigned"/>.
/// Matches BossmodReborn's <c>PartyRolesConfig</c> surface used by modules (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public sealed class PartyRolesConfig
{
    public enum Assignment { MT, OT, H1, H2, M1, M2, R1, R2, Unassigned }

    public readonly Dictionary<ulong, Assignment> Assignments = [];

    /// <summary>Role assigned to the member with the given content id, or Unassigned.</summary>
    public Assignment this[ulong contentID] => this.Assignments.GetValueOrDefault(contentID, Assignment.Unassigned);
}
