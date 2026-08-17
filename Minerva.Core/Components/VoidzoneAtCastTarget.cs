using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// A persistent voidzone that spawns at a cast's target location: the danger circle is predicted from
/// the cast, then handed off to the live voidzone actor (provided by <paramref name="sources"/>, e.g.
/// <c>m =&gt; m.Enemies(OID.Voidzone)</c>). Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class VoidzoneAtCastTarget(ModuleBase module, float radius, uint aid, Func<ModuleBase, IEnumerable<Actor>> sources, double castEventToSpawn = default) : GenericAOEs(module, aid, "GTFO from voidzone!")
{
    public readonly AOEShapeCircle Shape = new(radius);
    public readonly Func<ModuleBase, IEnumerable<Actor>> Sources = sources;
    public readonly double CastEventToSpawn = castEventToSpawn;
    protected readonly List<(WPos pos, DateTime time)> PredictedByEvent = [];
    protected readonly List<(Actor caster, DateTime time)> PredictedByCast = [];
    private readonly List<AOEInstance> aoes = [];

    public bool HaveCasters => this.PredictedByCast.Count > 0;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        this.aoes.Clear();
        foreach (var p in this.PredictedByCast)
        {
            var pos = this.World.Actors.Find(p.caster.CastInfo?.TargetID ?? 0)?.Position ?? p.caster.CastInfo?.LocXZ ?? p.caster.Position;
            this.aoes.Add(new AOEInstance(this.Shape, pos, default, p.time));
        }
        foreach (var p in this.PredictedByEvent)
            this.aoes.Add(new AOEInstance(this.Shape, p.pos, default, p.time));
        foreach (var z in this.Sources(this.Module))
            this.aoes.Add(new AOEInstance(this.Shape, z.Position));
        return CollectionsMarshal.AsSpan(this.aoes);
    }

    public override void Update()
    {
        if (this.PredictedByEvent.Count == 0)
            return;
        foreach (var s in this.Sources(this.Module))
        {
            for (var i = 0; i < this.PredictedByEvent.Count; ++i)
            {
                if (this.PredictedByEvent[i].pos.InCircle(s.Position, 2f))
                {
                    this.PredictedByEvent.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.PredictedByCast.Add((caster, this.Module.CastFinishAt(cast)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != this.WatchedAction)
            return;
        for (var i = 0; i < this.PredictedByCast.Count; ++i)
        {
            if (this.PredictedByCast[i].caster.InstanceID == caster.InstanceID)
            {
                this.PredictedByCast.RemoveAt(i);
                break;
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == this.WatchedAction)
            this.PredictedByEvent.Add((this.World.Actors.Find(spell.MainTargetID)?.Position ?? spell.TargetXZ, this.World.FutureTime(this.CastEventToSpawn)));
    }
}

/// <summary>A <see cref="VoidzoneAtCastTarget"/> keyed off several actions. Ported from BossmodReborn (BSD-3).</summary>
public class VoidzoneAtCastTargetGroup(ModuleBase module, float radius, uint[] aids, Func<ModuleBase, IEnumerable<Actor>> sources, double castEventToSpawn = default) : VoidzoneAtCastTarget(module, radius, default, sources, castEventToSpawn)
{
    private readonly uint[] AIDs = aids;
    private bool Watches(uint id) => Array.IndexOf(this.AIDs, id) >= 0;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (this.Watches(cast.Action.ID))
            this.PredictedByCast.Add((caster, this.Module.CastFinishAt(cast)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (this.Watches(spell.Action.ID))
        {
            ++this.NumCasts;
            this.PredictedByEvent.Add((this.World.Actors.Find(spell.MainTargetID)?.Position ?? spell.TargetXZ, this.World.FutureTime(this.CastEventToSpawn)));
        }
    }
}
