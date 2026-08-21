using System.Runtime.InteropServices;

namespace Minerva.Components;

/// <summary>
/// "Twisters": AOEs that spawn under wherever the players were standing at a snapshot moment, and can't
/// be predicted accurately until it's too late to react. The snapshot is taken on a cast or at component
/// creation, then replaced by the real actors once they spawn. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
/// </summary>
public class GenericTwister(ModuleBase module, float radius, uint oid, uint aid = default) : GenericAOEs(module, aid, "GTFO from twister!")
{
    private readonly AOEShapeCircle shape = new(radius);
    private readonly uint twisterOID = oid;
    protected readonly List<Actor> Twisters = module.Enemies(oid);
    protected DateTime PredictedActivation;
    protected readonly List<WPos> PredictedPositions = [];
    private readonly List<AOEInstance> active = [];

    /// <summary>Spawned twisters that haven't finished (event state 7 = done).</summary>
    public List<Actor> ActiveTwisters => this.Twisters.FindAll(t => t.EventState != 7);

    public bool Active => this.ActiveTwisters.Count != 0;

    /// <summary>Snapshot every player's current position as the predicted twister spots.</summary>
    public void AddPredicted(float activationDelay)
    {
        this.PredictedPositions.Clear();
        foreach (var a in this.World.Party.WithoutSlot())
            this.PredictedPositions.Add(a.Position);
        this.PredictedActivation = this.World.FutureTime(activationDelay);
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        this.active.Clear();
        foreach (var p in this.PredictedPositions)
            this.active.Add(new AOEInstance(this.shape, p, default, this.PredictedActivation));
        foreach (var t in this.ActiveTwisters)
            this.active.Add(new AOEInstance(this.shape, t.Position));
        return CollectionsMarshal.AsSpan(this.active);
    }

    public override void OnActorCreated(Actor actor)
    {
        // the real thing spawned — the prediction has served its purpose
        if (actor.OID == this.twisterOID)
            this.PredictedPositions.Clear();
    }
}

/// <summary>Twister that predicts as soon as the component is created.</summary>
public class ImmediateTwister : GenericTwister
{
    public ImmediateTwister(ModuleBase module, float radius, uint oid, float activationDelay)
        : base(module, radius, oid) => this.AddPredicted(activationDelay);
}

/// <summary>
/// Twister that predicts at (or slightly before) the end of a cast — <paramref name="predictBeforeCastEnd"/>
/// buys reaction time at the cost of accuracy.
/// </summary>
public class CastTwister(ModuleBase module, float radius, uint oid, uint aid, float activationDelay, float predictBeforeCastEnd = 0f) : GenericTwister(module, radius, oid, aid)
{
    private readonly float activationDelay = activationDelay; // cast end -> twister spawn
    private readonly float predictBeforeCastEnd = predictBeforeCastEnd;
    private DateTime predictStart = DateTime.MaxValue;

    public override void Update()
    {
        if (this.PredictedPositions.Count == 0 && this.Twisters.Count == 0 && this.World.CurrentTime >= this.predictStart)
        {
            this.AddPredicted(this.predictBeforeCastEnd + this.activationDelay);
            this.predictStart = DateTime.MaxValue;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction && this.predictStart == DateTime.MaxValue)
            this.predictStart = this.Module.CastFinishAt(cast, -this.predictBeforeCastEnd);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        // the cast ended sooner than predicted — snapshot now
        if (cast.Action.ID == this.WatchedAction && this.predictStart < DateTime.MaxValue)
        {
            this.AddPredicted(this.activationDelay);
            this.predictStart = DateTime.MaxValue;
        }
    }
}
