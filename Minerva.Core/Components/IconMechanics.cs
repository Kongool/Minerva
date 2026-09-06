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
