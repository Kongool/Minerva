namespace Minerva.Components;

/// <summary>
/// A "shared" tankbuster that several players must stack into (or the tank invulns). Tracks the active
/// cast's source/target, draws the shared AOE, and advises stacking or getting clear. Ported from
/// BossmodReborn's GenericSharedTankbuster (BSD-3; see THIRD-PARTY-NOTICES.txt); Minerva has no per-actor
/// Role, so the hints don't distinguish tanks and there is no ShapeDistance AI weighting.
/// </summary>
public class GenericSharedTankbuster(ModuleBase module, uint aid, AOEShape shape, bool originAtTarget = false) : CastCounter(module, aid)
{
    public readonly AOEShape Shape = shape;
    public readonly bool OriginAtTarget = originAtTarget;
    protected Actor? Source;
    protected Actor? Target;
    protected DateTime Activation;

    public bool Active => this.Source != null;

    public GenericSharedTankbuster(ModuleBase module, uint aid, float radius) : this(module, aid, new AOEShapeCircle(radius), true) { }

    protected bool InAOE(Actor actor)
    {
        if (this.Source == null || this.Target == null)
            return false;
        return this.OriginAtTarget
            ? this.Shape.Check(actor.Position, this.Target.Position, this.Target.Rotation)
            : this.Shape.Check(actor.Position, this.Source.Position, Angle.FromDirection(this.Target.Position - this.Source.Position));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (this.Target == null)
            return;
        if (this.Target == actor)
            hints.Add("Shared tankbuster — stack with tanks or invuln!");
        else if (this.InAOE(actor))
            hints.Add("Sharing the tankbuster", false);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (this.Source == null || this.Target == null)
            return;
        if (this.OriginAtTarget)
            this.Arena.OutlineShape(this.Shape, this.Target.Position, this.Target.Rotation, Colors.Safe, 2f);
        else
            this.Arena.OutlineShape(this.Shape, this.Source.Position, Angle.FromDirection(this.Target.Position - this.Source.Position), Colors.Safe, 2f);
    }
}

/// <summary>Shared tankbuster keyed off a watched cast. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public class CastSharedTankbuster(ModuleBase module, uint aid, AOEShape shape, bool originAtTarget = false) : GenericSharedTankbuster(module, aid, shape, originAtTarget)
{
    public CastSharedTankbuster(ModuleBase module, uint aid, float radius) : this(module, aid, new AOEShapeCircle(radius), true) { }

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == this.WatchedAction)
        {
            this.Source = caster;
            this.Target = this.World.Actors.Find(cast.TargetID);
            this.Activation = this.Module.CastFinishAt(cast);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (caster == this.Source)
            this.Source = this.Target = null;
    }
}

/// <summary>Shared tankbuster marked by an overhead icon. Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public class IconSharedTankbuster(ModuleBase module, uint iconId, uint aid, AOEShape shape, double activationDelay = 5.1d, bool originAtTarget = false) : GenericSharedTankbuster(module, aid, shape, originAtTarget)
{
    public IconSharedTankbuster(ModuleBase module, uint iconId, uint aid, float radius, double activationDelay = 5.1d) : this(module, iconId, aid, new AOEShapeCircle(radius), activationDelay, true) { }

    public readonly uint IconID = iconId;
    public readonly double ActivationDelay = activationDelay;

    public virtual Actor? BaitSource(Actor target) => this.Module.PrimaryActor;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == this.IconID)
        {
            this.Source = this.BaitSource(actor);
            this.Target = actor;
            this.Activation = this.World.FutureTime(this.ActivationDelay);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == this.WatchedAction)
            this.Source = this.Target = null;
    }
}
