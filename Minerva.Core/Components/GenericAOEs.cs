using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Base for any component that exposes a set of active danger zones. Handles the common work:
/// drawing each zone on the radar and warning the local player when they stand inside a risky one.
/// Subclasses just implement <see cref="ActiveAOEs"/>. Derives from <see cref="CastCounter"/> (matching
/// BMR) so subclasses inherit <c>WatchedAction</c> / <c>NumCasts</c>.
/// </summary>
public abstract class GenericAOEs(ModuleBase module, uint aid = default, string warningText = "GTFO from AOE!") : CastCounter(module, aid)
{
    /// <summary>An AOE resolving within this many seconds is drawn in the brighter "imminent" colour.</summary>
    public const double ImminentSeconds = 2d;

    public readonly string WarningText = warningText;

    public abstract ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor);

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        var now = this.World.CurrentTime;
        var aoes = this.ActiveAOEs(pcSlot, pc);
        foreach (ref readonly var aoe in aoes)
            this.Arena.ZoneShape(aoe.Shape, aoe.Origin, aoe.Rotation, ColorFor(aoe, now));
    }

    /// <summary>Fill colour for an AOE: its own override, else brighter when it's about to resolve.</summary>
    public static uint ColorFor(in AOEInstance aoe, DateTime now)
    {
        if (aoe.Color != 0)
            return aoe.Color;
        var imminent = aoe.Activation != default && (aoe.Activation - now).TotalSeconds <= ImminentSeconds;
        return imminent ? Colors.AOEImminent : Colors.AOE;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var aoes = this.ActiveAOEs(slot, actor);
        foreach (ref readonly var aoe in aoes)
        {
            if (aoe.Risky && aoe.Check(actor.Position))
            {
                hints.Add(this.WarningText);
                break;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var aoes = this.ActiveAOEs(slot, actor);
        foreach (ref readonly var aoe in aoes)
            if (aoe.Risky)
                hints.AddForbiddenZone(aoe);
    }
}

/// <summary>
/// The workhorse: draws a fixed-shape AOE for every active cast of <c>AID</c>. Add one line per
/// cast-bar mechanic — e.g.
/// <c>sealed class Hydrowave(ModuleBase m) : Components.SimpleAOEs(m, (uint)AID.Hydrowave, new AOEShapeCone(60f, 15f.Degrees()));</c>
/// </summary>
public class SimpleAOEs(ModuleBase module, uint aid, AOEShape shape, int maxCasts = int.MaxValue, double riskyWithSecondsLeft = default) : GenericAOEs(module, aid)
{
    public SimpleAOEs(ModuleBase module, uint aid, float radius, int maxCasts = int.MaxValue, double riskyWithSecondsLeft = default)
        : this(module, aid, new AOEShapeCircle(radius), maxCasts, riskyWithSecondsLeft) { }

    public readonly AOEShape Shape = shape;
    public int MaxCasts = maxCasts;       // for staggered waves: cap how many are drawn at once
    public uint Color;                    // optional colour override for every instance
    public bool Risky = true;             // toggle whether these count as "you're in danger"
    public int? MaxDangerColor;           // colour the first N casters as Danger
    public int? MaxRisky;                 // only the first N casters count as risky
    public readonly double RiskyWithSecondsLeft = riskyWithSecondsLeft; // delay risk until within N seconds of resolve
    public readonly List<AOEInstance> Casters = [];

    public ReadOnlySpan<AOEInstance> ActiveCasters
    {
        get
        {
            var max = Math.Min(this.Casters.Count, this.MaxCasts);
            return CollectionsMarshal.AsSpan(this.Casters)[..max];
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = this.Casters.Count;
        if (count == 0)
            return [];

        var now = this.World.CurrentTime;
        var max = Math.Min(count, this.MaxCasts);
        var aoes = CollectionsMarshal.AsSpan(this.Casters);
        for (var i = 0; i < max; ++i)
        {
            ref var aoe = ref aoes[i];
            aoe.Color = (this.MaxDangerColor is { } mdc && i < mdc) ? Colors.Danger : this.Color;
            var risky = this.Risky && (this.MaxRisky is not { } mr || i < mr);
            if (this.RiskyWithSecondsLeft != default)
                risky &= aoe.Activation.AddSeconds(-this.RiskyWithSecondsLeft) <= now;
            aoe.Risky = risky;
        }
        return aoes[..max];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position; // location-targeted vs self-targeted
        this.Casters.Add(new AOEInstance(this.Shape, origin, cast.Rotation, this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.Casters.RemoveAll(c => c.ActorID == caster.InstanceID);
    }
}

/// <summary>
/// A <see cref="SimpleAOEs"/> that also records the hit as predicted raidwide damage (a proximity AOE
/// nobody fully escapes). Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class ProximityAOEs(ModuleBase module, uint aid, float radius) : SimpleAOEs(module, aid, radius)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (this.Casters.Count != 0)
            hints.AddPredictedDamage(this.World.Party.WithSlot().Mask(), this.Casters[0].Activation);
    }
}
