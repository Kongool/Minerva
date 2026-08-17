namespace Minerva.Components;

/// <summary>
/// Tether-telegraphed AOE: when an actor gains tether <c>TetherID</c>, a fixed shape is drawn on the
/// tether's target (the actor that will erupt), resolving after <c>Delay</c> and cleared when a cast of
/// <c>WatchedAction</c> finishes on that target. Common "leash then AOE" mechanic (e.g. Occult Crescent
/// CE StoneSwell/Rockslide). Set <c>onSource</c> for the variant that erupts at the tether source.
/// </summary>
public class TetherAOEs(ModuleBase module, uint tetherID, uint aid, AOEShape shape, double delay = 6d, bool onSource = false) : GenericAOEs(module, aid)
{
    public readonly uint TetherID = tetherID;
    public readonly AOEShape Shape = shape;
    public readonly double Delay = delay;
    public readonly bool OnSource = onSource;
    private readonly List<AOEInstance> aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this.aoes);

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID != this.TetherID)
            return;
        var erupter = this.OnSource ? source : (this.World.Actors.Find(tether.Target) ?? source);
        this.aoes.Add(new AOEInstance(this.Shape, erupter.Position, erupter.Rotation, this.World.FutureTime(this.Delay), actorID: erupter.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
            this.aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
    }
}

/// <summary>
/// Bait-away tether: whoever is tethered (tether id <c>TetherID</c>) should carry the mechanic away
/// from the group. Draws the tethered player and reminds them to bait.
/// </summary>
public class BaitAwayTethers(ModuleBase module, uint tetherID) : ModuleComponent(module)
{
    public readonly uint TetherID = tetherID;
    protected readonly List<ulong> Baiters = [];

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == this.TetherID)
            this.Baiters.Add(source.InstanceID);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether) => this.Baiters.Remove(source.InstanceID);

    public bool IsBaiter(Actor actor) => this.Baiters.Contains(actor.InstanceID);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var id in this.Baiters)
        {
            var a = this.World.Actors.Find(id);
            if (a != null)
                this.Arena.AddCircle(a.Position, 2f, Colors.Danger, 2f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.IsBaiter(actor))
            hints.Add("Bait away!");
    }
}
