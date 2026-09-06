namespace Minerva;

/// <summary>Names for each group index of a <see cref="GroupAssignment"/> field, in order.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class GroupDetailsAttribute(string[] names) : Attribute
{
    public string[] Names { get; } = names;
}

/// <summary>A named one-click arrangement for a <see cref="GroupAssignment"/> field — "G1 N, G2 S, TMRH".</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class GroupPresetAttribute(string name, int[] preset) : Attribute
{
    public string Name { get; } = name;
    public int[] Preset { get; } = preset;
}

/// <summary>
/// Maps the eight role slots (MT/OT/H1/H2/M1/M2/R1/R2) onto numbered groups. Ported from BossmodReborn
/// (BSD-3; see THIRD-PARTY-NOTICES.txt).
///
/// <para>This is how a raid module is told the party's plan: "supports north, DD south" is a mapping from
/// role to group, and the module then resolves each mechanic by looking up which group the player is in.
/// It is deliberately expressed in roles rather than names, so it survives a different party.</para>
///
/// <para><see cref="Resolve"/> yields nothing at all when the assignment is invalid or the party's roles are
/// not fully assigned. That is the safe direction: a module that cannot tell which group you are in should
/// draw no group-specific advice rather than guess and send half the party the wrong way.</para>
/// </summary>
public class GroupAssignment
{
    /// <summary>Group index per role, indexed by <see cref="PartyRolesConfig.Assignment"/>. -1 is unset.</summary>
    public int[] Assignments = [-1, -1, -1, -1, -1, -1, -1, -1];

    public int this[PartyRolesConfig.Assignment r]
    {
        get => (int)r >= 0 && (int)r < this.Assignments.Length ? this.Assignments[(int)r] : -1;
        set => this.Assignments[(int)r] = value;
    }

    /// <summary>Whether this arrangement is usable — overridden per flavour to enforce its own shape.</summary>
    public virtual bool Validate() => true;

    /// <summary>
    /// Party slot to group index, for each of the eight roles. Empty when either this assignment or the
    /// party's role assignment is incomplete.
    /// </summary>
    public IEnumerable<(int slot, int group)> Resolve(PartyState party, PartyRolesConfig roles)
    {
        if (!this.Validate())
            yield break;
        var roleToSlot = roles.SlotsPerAssignment(party);
        if (roleToSlot.Length != this.Assignments.Length)
            yield break;
        for (var role = 0; role < this.Assignments.Length; ++role)
            yield return (roleToSlot[role], this.Assignments[role]);
    }

    /// <summary>Slot mask for one group; empty if the assignment cannot be resolved.</summary>
    public BitMask BuildGroupMask(int group, PartyState party, PartyRolesConfig roles)
    {
        BitMask mask = default;
        foreach (var (slot, g) in this.Resolve(party, roles))
            if (g == group)
                mask.Set(slot);
        return mask;
    }

    public IEnumerable<(int slot, int group)> Resolve(PartyState party) => this.Resolve(party, ConfigRoot.Instance.Get<PartyRolesConfig>());

    public BitMask BuildGroupMask(int group, PartyState party) => this.BuildGroupMask(group, party, ConfigRoot.Instance.Get<PartyRolesConfig>());
}

/// <summary>Two light parties, one tank/healer/melee/ranged each.</summary>
public class GroupAssignmentLightParties : GroupAssignment
{
    public static GroupAssignmentLightParties DefaultLightParties()
    {
        var r = new GroupAssignmentLightParties();
        r[PartyRolesConfig.Assignment.MT] = r[PartyRolesConfig.Assignment.H1] = r[PartyRolesConfig.Assignment.M1] = r[PartyRolesConfig.Assignment.R1] = 0;
        r[PartyRolesConfig.Assignment.OT] = r[PartyRolesConfig.Assignment.H2] = r[PartyRolesConfig.Assignment.M2] = r[PartyRolesConfig.Assignment.R2] = 1;
        return r;
    }

    /// <summary>Each role pair (MT/OT, H1/H2, ...) must be split one per party.</summary>
    public override bool Validate()
    {
        for (var i = 0; i < (int)PartyRolesConfig.Assignment.Unassigned; i += 2)
        {
            var a = this.Assignments[i];
            var b = this.Assignments[i + 1];
            if (a is < 0 or >= 2 || b is < 0 or >= 2 || a == b)
                return false;
        }
        return true;
    }
}

/// <summary>Four pairs, each one support and one damage dealer.</summary>
public class GroupAssignmentDDSupportPairs : GroupAssignment
{
    public static GroupAssignmentDDSupportPairs DefaultOneMeleePerPair()
    {
        GroupAssignmentDDSupportPairs r = new();
        r[PartyRolesConfig.Assignment.MT] = r[PartyRolesConfig.Assignment.R1] = 0;
        r[PartyRolesConfig.Assignment.H1] = r[PartyRolesConfig.Assignment.M1] = 1;
        r[PartyRolesConfig.Assignment.OT] = r[PartyRolesConfig.Assignment.R2] = 2;
        r[PartyRolesConfig.Assignment.H2] = r[PartyRolesConfig.Assignment.M2] = 3;
        return r;
    }

    public static GroupAssignmentDDSupportPairs DefaultMeleeTogether()
    {
        GroupAssignmentDDSupportPairs r = new();
        r[PartyRolesConfig.Assignment.MT] = r[PartyRolesConfig.Assignment.M1] = 0;
        r[PartyRolesConfig.Assignment.OT] = r[PartyRolesConfig.Assignment.M2] = 1;
        r[PartyRolesConfig.Assignment.H1] = r[PartyRolesConfig.Assignment.R1] = 2;
        r[PartyRolesConfig.Assignment.H2] = r[PartyRolesConfig.Assignment.R2] = 3;
        return r;
    }

    /// <summary>Every group must get exactly one support (roles 0-3) and one DD (roles 4-7).</summary>
    public override bool Validate()
    {
        BitMask mask = default; // bits 0-3: support of group N; bits 4-7: DD of group N-4
        for (var i = 0; i < 4; ++i)
            if (this.Assignments[i] is >= 0 and < 4)
                mask.Set(this.Assignments[i]);
        for (var i = 4; i < 8; ++i)
            if (this.Assignments[i] is >= 0 and < 4)
                mask.Set(this.Assignments[i] + 4);
        return mask.Raw == 0xff;
    }
}

/// <summary>Eight distinct spots, one per role — conga orders, tower fills, clock positions.</summary>
public class GroupAssignmentUnique : GroupAssignment
{
    public static GroupAssignmentUnique Default()
    {
        GroupAssignmentUnique r = new();
        r[PartyRolesConfig.Assignment.MT] = 0;
        r[PartyRolesConfig.Assignment.M1] = 1;
        r[PartyRolesConfig.Assignment.OT] = 2;
        r[PartyRolesConfig.Assignment.M2] = 3;
        r[PartyRolesConfig.Assignment.R1] = 4;
        r[PartyRolesConfig.Assignment.H1] = 5;
        r[PartyRolesConfig.Assignment.R2] = 6;
        r[PartyRolesConfig.Assignment.H2] = 7;
        return r;
    }

    public static GroupAssignmentUnique DefaultRoles()
    {
        GroupAssignmentUnique r = new();
        r[PartyRolesConfig.Assignment.MT] = 0;
        r[PartyRolesConfig.Assignment.OT] = 1;
        r[PartyRolesConfig.Assignment.H1] = 2;
        r[PartyRolesConfig.Assignment.H2] = 3;
        r[PartyRolesConfig.Assignment.M1] = 4;
        r[PartyRolesConfig.Assignment.M2] = 5;
        r[PartyRolesConfig.Assignment.R1] = 6;
        r[PartyRolesConfig.Assignment.R2] = 7;
        return r;
    }

    /// <summary>All eight spots must be taken exactly once.</summary>
    public override bool Validate()
    {
        BitMask mask = default;
        for (var i = 0; i < 8; ++i)
        {
            if (this.Assignments[i] is < 0 or > 7)
                return false;
            mask.Set(this.Assignments[i]);
        }
        return mask.Raw == 0xff;
    }
}
