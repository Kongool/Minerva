using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Base for any component that exposes a set of active danger zones. Handles the common work:
/// drawing each zone on the radar and warning the local player when they stand inside a risky one.
/// Subclasses just implement <see cref="ActiveAOEs"/>.
/// </summary>
public abstract class GenericAOEs(ModuleBase module, string warningText = "GTFO from AOE!") : ModuleComponent(module)
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

    public override void AddAIHints(int slot, Actor actor, AIHints hints)
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
public class SimpleAOEs(ModuleBase module, uint aid, AOEShape shape, int maxCasts = int.MaxValue) : GenericAOEs(module)
{
    public SimpleAOEs(ModuleBase module, uint aid, float radius, int maxCasts = int.MaxValue)
        : this(module, aid, new AOEShapeCircle(radius), maxCasts) { }

    public readonly uint WatchedAction = aid;
    public readonly AOEShape Shape = shape;
    public int MaxCasts = maxCasts; // for staggered waves: cap how many are drawn at once
    public readonly List<AOEInstance> Casters = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = Math.Min(this.Casters.Count, this.MaxCasts);
        return CollectionsMarshal.AsSpan(this.Casters)[..count];
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
