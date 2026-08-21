namespace Minerva.Components;

/// <summary>
/// Multi-hit tankbuster that forces a tank swap: the first hit's target is locked when the mechanic
/// starts, and later hits follow whoever the boss is targeting — so the off-tank must provoke in between.
/// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt); BMR reads the resolved victim from the
/// cast event's per-target list, which Minerva's events don't carry, so the main target is used instead.
/// </summary>
public class TankSwap(ModuleBase module, uint bossCast, uint firstCast, uint subsequentHit, double delay1, double delay2, AOEShape? shape = null, bool centerAtTarget = false)
    : GenericBaitAway(module, centerAtTarget: centerAtTarget, tankbuster: true)
{
    public TankSwap(ModuleBase module, uint bossCast, uint firstCast, uint subsequentHit, double delay1, double delay2, float radius, bool centerAtTarget = true)
        : this(module, bossCast, firstCast, subsequentHit, delay1, delay2, new AOEShapeCircle(radius), centerAtTarget) { }

    protected Actor? Source;
    protected ulong PrevTarget; // before the first cast this is the first hit's target
    public readonly AOEShape? Shape = shape;
    public readonly double Delay1 = delay1;
    public readonly double Delay2 = delay2;
    public readonly uint BossCast = bossCast;
    public readonly uint FirstCast = firstCast;
    public readonly uint SubsequentHit = subsequentHit;

    public override void Update()
    {
        if (this.Source == null || this.Shape == null)
            return;
        var count = this.CurrentBaits.Count;
        if (count == 0 || this.World.Actors.Find(this.Source.TargetID) is not { } t)
            return;
        // re-point the pending hit at whoever currently holds aggro
        if (count == 1 && this.NumCasts == 1)
            this.CurrentBaits.Ref(0).Target = t;
        else if (count == 2)
            this.CurrentBaits.Ref(1).Target = t;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.Source?.TargetID == this.PrevTarget && actor.Role == Role.Tank)
            hints.Add(this.PrevTarget != actor.InstanceID ? "Provoke!" : "Pass aggro!");
        base.AddHints(slot, actor, hints);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        var id = cast.Action.ID;
        if (id == this.BossCast)
        {
            this.Source = caster;
            if (this.Shape != null && this.World.Actors.Find(caster.TargetID) is { } t)
            {
                this.CurrentBaits.Add(new Bait(caster, t, this.Shape, this.Module.CastFinishAt(cast, this.Delay1)));
                this.CurrentBaits.Add(new Bait(caster, t, this.Shape, this.Module.CastFinishAt(cast, this.Delay2)));
            }
        }
        else if (id == this.FirstCast)
        {
            this.NumCasts = 0;
            this.PrevTarget = cast.TargetID;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent cast)
    {
        var id = cast.Action.ID;
        if (id != this.FirstCast && id != this.SubsequentHit)
            return;
        ++this.NumCasts;
        this.PrevTarget = cast.MainTargetID;
        if (this.CurrentBaits.Count != 0)
            this.CurrentBaits.RemoveAt(0);
    }
}
