namespace Minerva;

/// <summary>
/// Maps party members to the eight standard role slots (MT/OT/H1/H2/M1/M2/R1/R2). Ported modules take an
/// <see cref="Assignment"/> in <c>AddAIHints</c> so role-based positioning logic compiles unchanged.
/// Matches BossmodReborn's <c>PartyRolesConfig</c> surface used by modules (BSD-3; see THIRD-PARTY-NOTICES.txt).
///
/// <para>This is the root every <see cref="GroupAssignment"/> resolves through: a raid module's strategy is
/// expressed in roles, and this is what turns a role into the party slot standing in front of you.</para>
/// </summary>
[ConfigDisplay(Name = "Party role assignments", Order = -100)]
public sealed class PartyRolesConfig : ConfigNode
{
    public enum Assignment { MT, OT, H1, H2, M1, M2, R1, R2, Unassigned }

    /// <summary>Role per party member, by content id. Populated from the role UI or over IPC.</summary>
    public Dictionary<ulong, Assignment> Assignments = [];

    /// <summary>Role assigned to the member with the given content id, or Unassigned.</summary>
    public Assignment this[ulong contentID] => this.Assignments.GetValueOrDefault(contentID, Assignment.Unassigned);

    /// <summary>
    /// The role each party slot effectively plays: the assigned one where set, otherwise the role derived
    /// from their job. Mechanics that split "supports here, DD there" use this so they still resolve for a
    /// party that has assigned nothing.
    /// </summary>
    public Role[] EffectiveRolePerSlot(PartyState party)
    {
        var res = new Role[PartyState.MaxSlots];
        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            res[i] = this[party.Slots[i].ContentID] switch
            {
                Assignment.MT or Assignment.OT => Role.Tank,
                Assignment.H1 or Assignment.H2 => Role.Healer,
                Assignment.M1 or Assignment.M2 => Role.Melee,
                Assignment.R1 or Assignment.R2 => Role.Ranged,
                _ => party[i]?.Role ?? Role.None,
            };
        }
        return res;
    }

    /// <summary>
    /// Party slot for each role, indexed by <see cref="Assignment"/>.
    /// <para>Empty when the roles do not form a clean eight-slot arrangement — someone unassigned, or two
    /// people given the same role. Callers treat empty as "no plan", which is why every
    /// <see cref="GroupAssignment.Resolve"/> yields nothing in that case rather than half a plan.</para>
    /// </summary>
    /// <summary>
    /// The role each party slot holds, indexed by slot — the inverse of <see cref="SlotsPerAssignment"/>.
    /// <para>Slots with nobody in them, or nobody assigned, come back <see cref="Assignment.Unassigned"/>
    /// rather than the whole array being empty: a component asking "what is slot 3" wants an answer per
    /// slot, where one asking "who is MT" needs the entire plan to be coherent before any of it means
    /// anything.</para>
    /// </summary>
    public Assignment[] AssignmentsPerSlot(PartyState party)
    {
        var res = new Assignment[PartyState.MaxSlots];
        for (var i = 0; i < PartyState.MaxSlots; ++i)
            res[i] = this[party.Slots[i].ContentID];
        return res;
    }

    public int[] SlotsPerAssignment(PartyState party)
    {
        var res = new int[(int)Assignment.Unassigned];
        Array.Fill(res, PartyState.MaxSlots);
        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            var r = this[party.Slots[i].ContentID];
            if (r == Assignment.Unassigned)
                return [];
            if (res[(int)r] != PartyState.MaxSlots)
                return []; // two members claim the same role
            res[(int)r] = i;
        }
        return res;
    }
}
