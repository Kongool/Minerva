using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Generic "bait away from raid" mechanic: an AOE is aimed at a target (by cast, icon, or tether) who
/// must carry it clear of the group. Draws the local player's own bait as an outline and others' baits
/// filled, warns when baiting into people or being clipped, and feeds the auto-dodge engine. Ported from
/// BossmodReborn's GenericBaitAway (BSD-3; see THIRD-PARTY-NOTICES.txt), simplified for Minerva: no
/// predicted-damage weighting and cone/rect self-baits use a coarser AI hint (no ShapeDistance).
/// </summary>
public class GenericBaitAway(ModuleBase module, uint aid = default, bool alwaysDrawOtherBaits = true, bool centerAtTarget = false, bool tankbuster = false, bool onlyShowOutlines = false) : CastCounter(module, aid)
{
    public struct Bait(Actor source, Actor target, AOEShape shape, DateTime activation = default, BitMask forbidden = default, Angle? customRotation = null, int maxCasts = 1, WDir offset = default)
    {
        public Angle? CustomRotation = customRotation;
        public AOEShape Shape = shape;
        public Actor Source = source;
        public Actor Target = target;
        public DateTime Activation = activation;
        public BitMask Forbidden = forbidden;
        public int MaxCasts = maxCasts;
        public WDir Offset = offset;

        public readonly Angle Rotation => this.CustomRotation ?? (this.Source != this.Target ? Angle.FromDirection(this.Target.Position - this.Source.Position) : this.Source.Rotation);
    }

    public readonly bool AlwaysDrawOtherBaits = alwaysDrawOtherBaits; // draw others' baits even when they aren't clipping the player
    public readonly bool CenterAtTarget = centerAtTarget;             // AOE originates at the target instead of the source
    public bool OnlyShowOutlines = onlyShowOutlines;
    public bool AllowDeadTargets = true;
    public bool EnableHints = true;
    public bool IgnoreOtherBaits;
    public BitMask ForbiddenPlayers; // party members who should avoid baiting
    public readonly bool Tankbuster = tankbuster;
    public List<Bait> CurrentBaits = [];
    public const string BaitAwayHint = "Bait away from raid!";

    public List<Bait> ActiveBaits => this.CurrentBaits.FindAll(b => !b.Source.IsDead && (this.AllowDeadTargets || !b.Target.IsDead));
    public List<Bait> ActiveBaitsOn(Actor target) => this.CurrentBaits.FindAll(b => !b.Source.IsDead && b.Target == target);
    public List<Bait> ActiveBaitsNotOn(Actor target) => this.CurrentBaits.FindAll(b => !b.Source.IsDead && b.Target != target);
    public bool IsBaitTarget(Actor target) => this.CurrentBaits.Exists(b => !b.Source.IsDead && b.Target == target);

    public WPos BaitOrigin(in Bait bait) => this.CenterAtTarget ? bait.Target.Position : bait.Source.Position;
    public bool IsClippedBy(Actor actor, in Bait bait) => bait.Shape.Check(actor.Position, this.BaitOrigin(in bait), bait.Rotation);

    /// <summary>Party members (other than the bait's own target) currently inside the bait shape.</summary>
    public List<Actor> PlayersClippedBy(in Bait bait)
    {
        var clipped = new List<Actor>();
        var origin = this.BaitOrigin(in bait);
        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            var a = this.World.Party.Actor(i);
            if (a != null && a != bait.Target && bait.Shape.Check(a.Position, origin, bait.Rotation))
                clipped.Add(a);
        }
        return clipped;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!this.EnableHints)
            return;
        var baits = this.ActiveBaits;
        if (baits.Count == 0)
            return;

        if (this.ForbiddenPlayers[slot])
        {
            if (this.IsBaitTarget(actor))
                hints.Add("Avoid baiting!");
        }
        else
        {
            foreach (var bait in baits)
            {
                if (bait.Target == actor && this.PlayersClippedBy(in bait).Count != 0)
                {
                    hints.Add(BaitAwayHint);
                    break;
                }
            }
        }

        if (!this.IgnoreOtherBaits)
        {
            foreach (var bait in baits)
            {
                if (bait.Target != actor && this.IsClippedBy(actor, in bait))
                {
                    hints.Add("GTFO from baited aoe!");
                    break;
                }
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var bait in this.ActiveBaits)
        {
            if (bait.Target != actor)
                hints.AddForbiddenZone(bait.Shape, this.BaitOrigin(in bait), bait.Rotation, bait.Activation);
            else
                this.AddTargetSpecificHints(actor, in bait, hints);
        }
    }

    // for the player's own bait: avoid clipping party members. Position-anchored shapes (circle/donut/
    // cross) can be forbidden at each other member's position; source-anchored shapes (cone/rect) are
    // left to the "bait away" text hint (Minerva has no ShapeDistance cones for AI yet).
    private void AddTargetSpecificHints(Actor actor, in Bait bait, AIHints hints)
    {
        if (bait.Source == bait.Target)
            return;
        if (bait.Shape is not (AOEShapeCircle or AOEShapeDonut or AOEShapeCross))
            return;
        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            var a = this.World.Party.Actor(i);
            if (a != null && a != actor)
                hints.AddForbiddenZone(bait.Shape, a.Position - bait.Offset, bait.Rotation, bait.Activation);
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (this.OnlyShowOutlines || this.IgnoreOtherBaits)
            return;
        var baits = CollectionsMarshal.AsSpan(this.CurrentBaits);
        foreach (ref readonly var b in baits)
        {
            if (!b.Source.IsDead && b.Target.InstanceID != pc.InstanceID && (this.AlwaysDrawOtherBaits || this.IsClippedBy(pc, in b)))
                this.Arena.ZoneShape(b.Shape, this.BaitOrigin(in b), b.Rotation, Colors.AOE);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var baits = CollectionsMarshal.AsSpan(this.CurrentBaits);
        foreach (ref readonly var b in baits)
        {
            if (!b.Source.IsDead && (this.OnlyShowOutlines || b.Target.InstanceID == pc.InstanceID))
                this.Arena.OutlineShape(b.Shape, this.BaitOrigin(in b), b.Rotation, Colors.Danger, 2f);
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (this.Tankbuster && this.CurrentBaits.Count != 0)
            hints.Add("Tankbuster cleave");
    }
}

/// <summary>
/// A bait the whole party must STACK into (shared soak), rather than spread away. Subclasses add baits;
/// this advises stacking and — unlike <see cref="GenericBaitAway"/> — does not forbid the bait for the
/// auto-dodge engine (you approach it). Ported from BossmodReborn's GenericBaitStack (BSD-3; see
/// THIRD-PARTY-NOTICES.txt), simplified (no ShapeDistance overlap resolution).
/// </summary>
public abstract class GenericBaitStack(ModuleBase module, uint aid = default, bool onlyShowOutlines = false) : GenericBaitAway(module, aid, onlyShowOutlines: onlyShowOutlines)
{
    public const string HintStack = "Stack!";

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var baits = this.ActiveBaits;
        if (baits.Count == 0)
            return;
        // stacked if standing in any bait's shape
        foreach (var bait in baits)
            if (this.IsClippedBy(actor, in bait) || bait.Target == actor)
            {
                hints.Add(HintStack, false);
                return;
            }
        hints.Add(HintStack);
    }

    // stacks are approached, not avoided — contribute no forbidden zone
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) { }
}

/// <summary>
/// Bait away driven by a cast: each cast of <c>WatchedAction</c> marks its target as the baiter. One
/// line per mechanic. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class BaitAwayCast(ModuleBase module, uint aid, AOEShape shape, bool centerAtTarget = false, bool endsOnCastEvent = false, bool tankbuster = false)
    : GenericBaitAway(module, aid, centerAtTarget: centerAtTarget, tankbuster: tankbuster)
{
    public BaitAwayCast(ModuleBase module, uint aid, float radius, bool centerAtTarget = true, bool endsOnCastEvent = false, bool tankbuster = false)
        : this(module, aid, new AOEShapeCircle(radius), centerAtTarget, endsOnCastEvent, tankbuster) { }

    public AOEShape Shape = shape;
    public bool EndsOnCastEvent = endsOnCastEvent;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && this.World.Actors.Find(cast.TargetID) is { } target)
            this.CurrentBaits.Add(new Bait(caster, target, this.Shape, this.Module.CastFinishAt(cast)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && !this.EndsOnCastEvent)
            this.RemoveBySource(caster.InstanceID);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == this.WatchedAction && this.EndsOnCastEvent)
            this.RemoveBySource(caster.InstanceID);
    }

    private void RemoveBySource(ulong sourceID)
    {
        for (var i = 0; i < this.CurrentBaits.Count; ++i)
        {
            if (this.CurrentBaits[i].Source.InstanceID == sourceID)
            {
                this.CurrentBaits.RemoveAt(i);
                return;
            }
        }
    }
}

/// <summary>
/// Bait away for a charge that ends at the target: a rectangle from caster to target whose length tracks
/// the target's live position. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class BaitAwayChargeCast(ModuleBase module, uint aid, float halfWidth) : GenericBaitAway(module, aid, tankbuster: true)
{
    private readonly float HalfWidth = halfWidth;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && this.World.Actors.Find(cast.TargetID) is { } target)
            this.CurrentBaits.Add(new Bait(caster, target, new AOEShapeRect((target.Position - caster.Position).Length(), this.HalfWidth), this.Module.CastFinishAt(cast)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        for (var i = 0; i < this.CurrentBaits.Count; ++i)
            if (this.CurrentBaits[i].Source.InstanceID == caster.InstanceID) { this.CurrentBaits.RemoveAt(i); return; }
    }

    public override void Update()
    {
        var baits = CollectionsMarshal.AsSpan(this.CurrentBaits);
        foreach (ref var b in baits)
            b.Shape = new AOEShapeRect((b.Target.Position - b.Source.Position).Length(), this.HalfWidth);
    }
}

/// <summary>
/// Bait away marked by an overhead icon: the iconed player carries the AOE, resolving after a delay (and
/// cleared when the paired action is cast, if given). Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public class BaitAwayIcon(ModuleBase module, AOEShape shape, uint iconID, uint aid = default, double activationDelay = 5.1d, bool centerAtTarget = false, Actor? source = null, bool tankbuster = false)
    : GenericBaitAway(module, aid, centerAtTarget: centerAtTarget, tankbuster: tankbuster)
{
    public BaitAwayIcon(ModuleBase module, float radius, uint iconID, uint aid = default, double activationDelay = 5.1d, bool centerAtTarget = true, Actor? source = null, bool tankbuster = false)
        : this(module, new AOEShapeCircle(radius), iconID, aid, activationDelay, centerAtTarget, source, tankbuster) { }

    public AOEShape Shape = shape;
    public uint IID = iconID;
    public double ActivationDelay = activationDelay;

    public virtual Actor? BaitSource(Actor target) => source ?? this.Module.PrimaryActor;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == this.IID && this.BaitSource(actor) is { } src)
            this.CurrentBaits.Add(new Bait(src, this.World.Actors.Find(targetID) ?? actor, this.Shape, this.World.FutureTime(this.ActivationDelay)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (this.CurrentBaits.Count != 0 && spell.Action.ID == this.WatchedAction)
            this.CurrentBaits.RemoveAt(0);
    }

    public override void Update()
    {
        for (var i = this.CurrentBaits.Count - 1; i >= 0; --i)
            if (this.CurrentBaits[i].Target.IsDead)
                this.CurrentBaits.RemoveAt(i);
    }
}
