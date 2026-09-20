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
            if (this.OnViewersFloor(pc, aoe))
                this.Arena.ZoneShape(aoe.Shape, aoe.Origin, aoe.Rotation, ColorFor(aoe, now));
    }

    /// <summary>
    /// Is this AOE on the floor the viewer is standing on?
    ///
    /// <para>True for every fight on one level, which is nearly all of them. On a fight played on two --
    /// Shinryu Paradox casts the same crossing lines upstairs and downstairs at the same moment -- drawing
    /// both to everyone lays one floor's pattern over the other's and neither can be read, and forbidding
    /// both leaves the dodge a floor's worth of danger that cannot reach it.</para>
    /// </summary>
    protected bool OnViewersFloor(Actor viewer, in AOEInstance aoe)
        => this.Module.MechanicAppliesToArenaProjectionLayer(viewer, aoe.ArenaProjectionLayer, aoe.RestrictToArenaProjectionLayer);

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
            if (aoe.Risky && this.OnViewersFloor(actor, aoe))
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

    /// <summary>Is this the cast this component is waiting on? Group components watch several.</summary>
    protected virtual bool Watches(uint id) => id == this.WatchedAction;

    /// <summary>
    /// How long past its own resolution an unfinished cast is kept before it is treated as never coming.
    /// Generous on purpose: a cast event arriving three quarters of a second late has been measured.
    /// </summary>
    private const double UnresolvedGrace = 3d;

    /// <summary>
    /// Drop zones whose cast resolved long ago and was never taken off the list.
    ///
    /// <para>A zone is removed when its cast finishes. When that event never arrives -- the caster
    /// despawns mid-cast, a phase change takes the encounter somewhere else, the client stutters and the
    /// same cast is reported twice so one copy has no event left to claim -- the zone stays on the arena
    /// and in the dodge forever. Thunder God kept donuts from the 200-second mark alive at 380; one Double
    /// Trouble pair drew six 60-yalm half-planes of which four never cleared.</para>
    ///
    /// <para>Age alone is not the test, because plenty of AOEs are older than their activation and still
    /// real -- a standing voidzone is stamped when it appears and lives for minutes. The test is that the
    /// cast is over: past its own predicted resolution by the grace period, and the caster is no longer
    /// casting the thing we are waiting for. A slow cast is not a stale one, so a caster still mid-cast
    /// keeps its zone however long it takes.</para>
    /// </summary>
    public override void Update()
    {
        var count = this.Casters.Count;
        if (count == 0)
            return;

        var cutoff = this.World.CurrentTime.AddSeconds(-UnresolvedGrace);
        for (var i = count - 1; i >= 0; --i)
        {
            var aoe = this.Casters[i];
            if (aoe.Activation == default || aoe.Activation > cutoff)
                continue;   // no resolution predicted, or not yet overdue

            var caster = this.World.Actors.Find(aoe.ActorID);
            if (caster?.CastInfo is { } inflight && this.Watches(inflight.Action.ID))
                continue;   // still being cast: overdue is the prediction being wrong, not the zone being stale

            this.Casters.RemoveAt(i);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        var origin = cast.LocXZ != default ? cast.LocXZ : caster.Position; // location-targeted vs self-targeted
        // On a multi-floor arena the caster's own height says which floor this belongs to; on every other
        // arena there are no floors and this resolves to null, which restricts nothing.
        var layer = this.Module.ResolveArenaProjectionLayer(caster.PosRot.Y);
        this.Casters.Add(new AOEInstance(this.Shape, origin, cast.Rotation, this.Module.CastFinishAt(cast), actorID: caster.InstanceID,
            arenaProjectionLayer: layer, restrictToArenaProjectionLayer: layer != null));
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
