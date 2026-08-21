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
    private readonly AOEShapeCircle shape = new(radius);
    private readonly Func<ModuleBase, IEnumerable<Actor>>? sourcesFunc;
    private readonly List<AOEInstance> active = [];

    public Voidzone(ModuleBase module, float radius, uint oid) : this(module, radius, [oid]) { }

    /// <summary>BMR form: the live voidzone actors come from a callback (e.g. <c>m =&gt; m.Enemies(OID.Puddle)</c>).</summary>
    public Voidzone(ModuleBase module, float radius, Func<ModuleBase, IEnumerable<Actor>> sources, float moveHintLength = default) : this(module, radius, [])
        => this.sourcesFunc = sources;

    private IEnumerable<Actor> Sources()
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
            this.Arena.ZoneShape(this.shape, a.Position, default, Colors.AOE);
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
        foreach (var a in this.Sources())
            hints.AddForbiddenZone(this.shape, a.Position, default, this.World.CurrentTime);
    }

    /// <summary>
    /// The live puddles as AOE instances, already active (activation is "now"). BMR's Voidzone derives
    /// from GenericAOEs, so ported modules query this to test whether a destination sits in a puddle.
    /// </summary>
    public ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        this.active.Clear();
        foreach (var a in this.Sources())
            this.active.Add(new AOEInstance(this.shape, a.Position, default, this.World.CurrentTime));
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
