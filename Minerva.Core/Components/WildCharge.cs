namespace Minerva.Components;

/// <summary>
/// A "wild charge": a line AOE from a source to a target that some players must stay in (share) and
/// others must avoid. Modules assign each party slot a <see cref="PlayerRole"/> and set the source when
/// the mechanic starts. Ported from BossmodReborn's GenericWildCharge (BSD-3; see THIRD-PARTY-NOTICES.txt),
/// simplified for Minerva: no ShapeDistance union AI (avoiders get a forbidden rect; sharers get text only).
/// </summary>
public class GenericWildCharge(ModuleBase module, float halfWidth, uint aid = default, float fixedLength = default) : CastCounter(module, aid)
{
    public enum PlayerRole
    {
        Ignore,
        Target,          // the charge target
        TargetNotFirst,  // target that must hide behind another sharer
        Share,           // must stand in the AOE
        ShareNotFirst,   // must stand in the AOE, but not closest
        Avoid,           // must avoid the AOE
    }

    public readonly float HalfWidth = halfWidth;
    public readonly float FixedLength = fixedLength; // 0 => up to the target
    public Actor? Source;
    public DateTime Activation;
    public PlayerRole[] PlayerRoles = new PlayerRole[PartyState.MaxSlots];

    /// <summary>Geometry of one charge lane. Virtual so inverse variants can move the origin.</summary>
    protected virtual (WPos origin, WDir dir, float length) GetAOEForTarget(WPos sourcePos, WPos targetPos)
    {
        var toTarget = targetPos - sourcePos;
        var length = this.FixedLength > 0 ? this.FixedLength : toTarget.Length();
        return (sourcePos, toTarget.Normalized(), length);
    }

    protected bool InAOE((WPos origin, WDir dir, float length) aoe, Actor actor)
        => (actor.Position - aoe.origin).InRect(aoe.dir, aoe.length, 0, this.HalfWidth);

    protected IEnumerable<(WPos origin, WDir dir, float length)> EnumerateAOEs(int skipSlot = -1)
    {
        if (this.Source == null)
            yield break;
        foreach (var (i, p) in this.Raid.WithSlot().WhereSlot(i => i != skipSlot && this.PlayerRoles[i] is PlayerRole.Target or PlayerRole.TargetNotFirst))
            yield return this.GetAOEForTarget(this.Source.Position, p.Position);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.Source == null || slot < 0 || slot >= this.PlayerRoles.Length)
            return;
        switch (this.PlayerRoles[slot])
        {
            case PlayerRole.Share:
            case PlayerRole.ShareNotFirst:
                var shares = 0;
                foreach (var aoe in this.EnumerateAOEs())
                    if (this.InAOE(aoe, actor)) ++shares;
                if (shares == 0)
                    hints.Add("Stay inside charge!");
                else if (shares > 1)
                    hints.Add("Stay in a single charge!");
                break;
            case PlayerRole.Avoid:
                foreach (var aoe in this.EnumerateAOEs())
                    if (this.InAOE(aoe, actor)) { hints.Add("GTFO from charge!"); return; }
                break;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.Source == null || slot < 0 || slot >= this.PlayerRoles.Length)
            return;
        if (this.PlayerRoles[slot] == PlayerRole.Avoid)
            foreach (var aoe in this.EnumerateAOEs())
                hints.AddForbiddenZone(new AOEShapeRect(aoe.length, this.HalfWidth), aoe.origin, Angle.FromDirection(aoe.dir), this.Activation);
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (this.Source == null || pcSlot < 0 || pcSlot >= this.PlayerRoles.Length || this.PlayerRoles[pcSlot] == PlayerRole.Ignore)
            return;
        var dangerous = this.PlayerRoles[pcSlot] == PlayerRole.Avoid;
        foreach (var aoe in this.EnumerateAOEs())
            this.Arena.ZoneShape(new AOEShapeRect(aoe.length, this.HalfWidth), aoe.origin, Angle.FromDirection(aoe.dir), dangerous ? Colors.AOE : Colors.Safe);
    }
}

/// <summary>
/// Wild charge run backwards: the lane starts <c>DistanceBehind</c> yards *behind* the target and runs
/// toward the source, so sharers line up on the far side of the target rather than between them and the
/// boss. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class InverseWildCharge(ModuleBase module, float halfWidth, float distanceBehind, uint aid = default, float fixedLength = default)
    : GenericWildCharge(module, halfWidth, aid, fixedLength)
{
    public readonly float DistanceBehind = distanceBehind;

    protected override (WPos origin, WDir dir, float length) GetAOEForTarget(WPos sourcePos, WPos targetPos)
    {
        var toTarget = targetPos - sourcePos;
        var dir = toTarget.Normalized();
        // start behind the target and aim back at the source
        var origin = targetPos + this.DistanceBehind * dir;
        var length = this.FixedLength > 0 ? this.FixedLength : (origin - sourcePos).Length();
        return (origin, -dir, length);
    }
}
