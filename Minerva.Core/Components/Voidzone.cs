using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// Persistent ground hazard: while any actor of the given OIDs exists, a circle of radius
/// <c>Radius</c> around it is dangerous. Draws the puddle and contributes it to the auto-dodge
/// engine as a standing forbidden zone (already active — activation is "now").
/// </summary>
public class Voidzone(ModuleBase module, float radius, uint[] oids) : ModuleComponent(module)
{
    public readonly float Radius = radius;
    public readonly uint[] OIDs = oids;
    /// <summary>The zone's footprint. Public because ported modules override <c>ActiveAOEs</c> and
    /// rebuild the instances themselves, which needs the Shape the component was built with.</summary>
    public readonly AOEShapeCircle Shape = new(radius);
    /// <summary>How far ahead a moving source is assumed to keep going, in yalms; 0 for a puddle that stays
    /// put. Twenty-three ported modules pass this and, until 2026-09-06, it was accepted and dropped.</summary>
    public readonly float MovementHintLength;
    /// <summary>A moving source's footprint: a capsule from where it stands to where it is heading.</summary>
    public readonly AOEShapeCapsule? MoveShape;
    private readonly Func<ModuleBase, IEnumerable<Actor>>? sourcesFunc;
    private readonly List<AOEInstance> active = [];

    public Voidzone(ModuleBase module, float radius, uint oid) : this(module, radius, [oid]) { }

    /// <summary>BMR form: the live voidzone actors come from a callback (e.g. <c>m =&gt; m.Enemies(OID.Puddle)</c>).</summary>
    public Voidzone(ModuleBase module, float radius, Func<ModuleBase, IEnumerable<Actor>> sources, float moveHintLength = default) : this(module, radius, [])
    {
        this.sourcesFunc = sources;
        this.MovementHintLength = moveHintLength;
        this.MoveShape = moveHintLength > 0f ? new AOEShapeCapsule(radius, moveHintLength) : null;
    }

    /// <summary>The live voidzone actors. Public because ported modules query another component's
    /// voidzones to place themselves relative to them.</summary>
    /// <summary>BossmodReborn's form, which passes the module back in. The module is ignored — this
    /// component already holds the one it belongs to.</summary>
    public IEnumerable<Actor> Sources(ModuleBase module) => this.Sources();

    public IEnumerable<Actor> Sources()
    {
        if (this.sourcesFunc != null)
        {
            foreach (var a in this.sourcesFunc(this.Module))
                if (!a.IsDeadOrDestroyed)
                    yield return a;
            yield break;
        }
        foreach (var a in this.World.Actors)
            if (!a.IsDeadOrDestroyed && Array.IndexOf(this.OIDs, a.OID) >= 0)
                yield return a;
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var a in this.Sources())
            this.Arena.ZoneShape(this.MoveShape ?? (AOEShape)this.Shape, a.Position, this.MoveShape != null ? a.Rotation : default, Colors.AOE);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var a in this.Sources())
        {
            if (actor.Position.InCircle(a.Position, this.Radius))
            {
                hints.Add("Leave the voidzone!");
                return;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (this.MovementHintLength <= 0f)
        {
            foreach (var a in this.Sources())
                hints.AddForbiddenZone(this.Shape, a.Position, default, this.World.CurrentTime);
            return;
        }

        // A walking hazard is forbidden where it is and, sooner or later, where it is heading: capsules of
        // half, one, two and three hint lengths along its facing, landing at 1.1s, 3s, 10s and never, as
        // BossmodReborn does. The dodge then leaves the lane rather than stepping a yalm aside and being
        // caught (Tiamat's Clone's heads, 2026-09-06: six hits from a hazard drawn as a 2-yalm circle).
        var near = this.World.FutureTime(1.1d);
        var soon = this.World.FutureTime(3d);
        var far = this.World.FutureTime(10d);
        foreach (var a in this.Sources())
        {
            var dir = a.Rotation.ToDirection();
            hints.AddForbiddenZone(new SDCapsule(a.Position, dir, this.MovementHintLength * 0.5f, this.Radius), near);
            hints.AddForbiddenZone(new SDCapsule(a.Position, dir, this.MovementHintLength, this.Radius), soon);
            hints.AddForbiddenZone(new SDCapsule(a.Position, dir, 2f * this.MovementHintLength, this.Radius), far);
            hints.AddForbiddenZone(new SDCapsule(a.Position, dir, 3f * this.MovementHintLength, this.Radius), DateTime.MaxValue);
            hints.TemporaryObstacles.Add(new SDCircle(a.Position, this.Radius));
        }
    }

    /// <summary>
    /// The live puddles as AOE instances, already active (activation is "now"). BMR's Voidzone derives
    /// from GenericAOEs, so ported modules query this to test whether a destination sits in a puddle.
    /// </summary>
    // virtual because subclasses legitimately narrow it -- a voidzone that only counts while something else
    // is happening, or that hides its own puddle from the player standing in it. BossmodReborn's is too.
    public virtual ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        this.active.Clear();
        foreach (var a in this.Sources())
            this.active.Add(new AOEInstance(this.MoveShape ?? (AOEShape)this.Shape, a.Position, this.MoveShape != null ? a.Rotation : default, this.World.CurrentTime));
        return CollectionsMarshal.AsSpan(this.active);
    }
}

/// <summary>
/// A voidzone that can flip meaning: normally the puddle is dangerous, but while "inverted" it is the
/// only safe ground (you must be standing in it when the mechanic resolves). Ported from BossmodReborn
/// (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class PersistentInvertibleVoidzone(ModuleBase module, float radius, Func<ModuleBase, IEnumerable<Actor>> sources, uint aid = default) : CastCounter(module, aid)
{
    public readonly AOEShapeCircle Shape = new(radius);
    public readonly Func<ModuleBase, IEnumerable<Actor>> Sources = sources;
    public DateTime InvertResolveAt;

    public bool Inverted => this.InvertResolveAt != default;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var inVoidzone = false;
        foreach (var s in this.Sources(this.Module))
        {
            if (this.Shape.Check(actor.Position, s))
            {
                inVoidzone = true;
                break;
            }
        }

        if (this.Inverted)
            hints.Add(inVoidzone ? "Stay in voidzone" : "Go to voidzone!", !inVoidzone);
        else if (inVoidzone)
            hints.Add("GTFO from voidzone!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var shapes = new List<ShapeDistance>();
        foreach (var source in this.Sources(this.Module))
            shapes.Add(this.Shape.Distance(source.Position, source.Rotation));
        if (shapes.Count == 0)
            return;
        // inverted: everything OUTSIDE the union of puddles is forbidden
        hints.AddForbiddenZone(this.Inverted ? new SDOutsideOfUnion([.. shapes]) : new SDUnion([.. shapes]), this.InvertResolveAt);
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        var color = this.Inverted ? Colors.SafeFromAOE : Colors.AOE;
        foreach (var s in this.Sources(this.Module))
            this.Arena.ZoneShape(this.Shape, s.Position, s.Rotation, color);
    }
}

/// <summary>Invertible voidzone that inverts while a specific cast is up, resolving when it ends.</summary>
public class PersistentInvertibleVoidzoneByCast(ModuleBase module, float radius, Func<ModuleBase, IEnumerable<Actor>> sources, uint aid) : PersistentInvertibleVoidzone(module, radius, sources, aid)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.InvertResolveAt = this.Module.CastFinishAt(cast);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.InvertResolveAt = default;
    }
}
