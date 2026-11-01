namespace Minerva.Components;

/// <summary>
/// Cleaving auto-attacks: an enemy's melee swing covers a shape aimed at its current target, so everyone
/// except that target must stay out of it. Outlines the shape and warns anyone else standing in it. An
/// empty <paramref name="enemyOID"/> means the module's primary actor. Ported from BossmodReborn (BSD-3;
/// see THIRD-PARTY-NOTICES.txt).
/// </summary>
public class Cleave(ModuleBase module, uint aid, AOEShape shape, uint[]? enemyOID = null, bool activeForUntargetable = false, bool originAtTarget = false, bool activeWhileCasting = true) : CastCounter(module, aid)
{
    public readonly AOEShape Shape = shape;
    public readonly bool ActiveForUntargetable = activeForUntargetable;
    public readonly bool ActiveWhileCasting = activeWhileCasting;
    public readonly bool OriginAtTarget = originAtTarget;
    public DateTime NextExpected;
    public readonly uint[] EnemyOID = enemyOID ?? [module.PrimaryActor.OID];

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var e in this.OriginsAndTargets())
        {
            if (actor != e.target && this.Shape.Check(actor.Position, e.origin.Position, e.angle))
            {
                hints.Add("GTFO from cleave!");
                return;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var e in this.OriginsAndTargets())
        {
            if (actor != e.target)
                hints.AddForbiddenZone(this.Shape, e.origin.Position, e.angle, this.NextExpected);
            else
                this.AddTargetSpecificHints(actor, e.origin, hints);
        }
    }

    /// <summary>
    /// When the viewer IS the cleave target, they can't dodge it — instead they must not aim it at anyone.
    /// Each other party member becomes a forbidden direction (or, for a circle, a forbidden spot to stand
    /// near), so the pathfinder keeps the cleave pointed at empty ground. With <see cref="AvoidOwnPet"/>, the
    /// viewer's own pet is kept out of it too.
    /// </summary>
    protected void AddTargetSpecificHints(Actor actor, Actor source, AIHints hints, DateTime activation = default)
    {
        foreach (var a in this.World.Party.WithoutSlot())
        {
            if (a == actor)
                continue;
            this.AvoidAiming(a, source, hints, activation);
        }
        if (this.AvoidOwnPet)
            foreach (var a in this.World.Actors)
            {
                // a pet standing over the enemy's centre is in every swing, and the direction to it is noise
                if (a.OwnerID != actor.InstanceID || a.Type is not (ActorType.Pet or ActorType.Chocobo) || a.IsDead)
                    continue;
                var dist = (a.Position - source.Position).Length();
                if (dist > a.HitboxRadius)
                    this.AvoidAiming(a, source, hints, activation, a.HitboxRadius);
            }
    }

    // `reach` widens the shape by the actor's hitbox: a familiar is 1 to 2 yalms across, so a swing clips it well
    // outside the angle its centre sits at (measured at Ymir and Siren, 2026-10-31).
    private void AvoidAiming(Actor a, Actor source, AIHints hints, DateTime activation, float reach = 0f)
    {
        var dist = (a.Position - source.Position).Length();
        switch (this.Shape)
        {
            case AOEShapeCircle circle:
                hints.AddForbiddenZone(new AOEShapeCircle(circle.Radius + reach), a.Position, default, activation);
                break;
            case AOEShapeCone cone:
                var pad = reach > 0f && dist > 0f ? Angle.Asin(Math.Min(1f, reach / dist)) : default;
                hints.AddForbiddenZone(new SDCone(source.Position, 100f, source.AngleTo(a), cone.HalfAngle + pad), activation);
                break;
            case AOEShapeRect rect:
                if (dist > 0f)
                    hints.AddForbiddenZone(new SDCone(source.Position, 100f, source.AngleTo(a), Angle.Asin(Math.Min(1f, (rect.HalfWidth + reach) / dist))), activation);
                break;
        }
    }

    /// <summary>
    /// <para>Has a component before this one timed a zone to land: a cast, a bait, a spawn with a fuse? A cleave is an
    /// auto-attack, and stepping round it must never cost the walk to a safe spot. A component that asks this and adds
    /// nothing while it holds leaves the dodge planning for the mechanic alone. Ymir, 2026-10-31 (replayed): kept out
    /// of Head Snatch, which is always about to swing, the character took the long way to Tsunami's safe wall and was
    /// still walking when the wave came.</para>
    /// <para>It reads the zones already added this frame, so the cleave must be activated after the fight's other
    /// components. Untimed zones (scenery, a spot to walk to) and "stand here" instructions do not count. An overdue
    /// one does: a cast resolves a little after its bar ends, and its zone stays until it has. Guttler's Gyrocleave,
    /// 2026-10-31 (replayed): bar done at 54.61s, the line at 54.91s; counting only zones still ahead, the cleave stood
    /// back up in between and walked the character out of the safe notch into the line.</para>
    /// <para>An untimed zone the character stands in counts too: they are already on their way somewhere, so the
    /// cleave must not lengthen the walk. Guttler's Molten Metal "go to the spot", replayed: twice as long otherwise.</para>
    /// </summary>
    protected bool TimedMechanicPending(AIHints hints)
    {
        foreach (var z in hints.ForbiddenZones)
        {
            if (z.Activation == DateTime.MaxValue)
                continue;
            if (z.Activation != default || z.ShapeDistance.Distance(hints.PlayerPosition) <= 0f)
                return true;
        }
        return false;
    }

    /// <summary>
    /// <para>Also keep the cleave off the viewer's own pet when the viewer holds the enemy. For a Crucible of the
    /// Unbroken familiar, the tank: it stands in melee beside the beastmaster, so a swing at the beastmaster cleaves
    /// it as well, and under Cover it takes the beastmaster's hit on top of its own (the user, 2026-10-31: Guttler).</para>
    /// <para>Off by default, like <see cref="GenericBaitAway"/>'s: elsewhere a pet is not hurt by it.</para>
    /// </summary>
    protected virtual bool AvoidOwnPet => false;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var e in this.OriginsAndTargets())
            this.Arena.OutlineShape(this.Shape, e.origin.Position, e.angle, Colors.Danger);
    }

    /// <summary>Each live cleaving enemy paired with its current target and the angle between them.</summary>
    public virtual List<(Actor origin, Actor target, Angle angle)> OriginsAndTargets()
    {
        var origins = new List<(Actor, Actor, Angle)>();
        foreach (var enemy in this.Module.Enemies(this.EnemyOID))
        {
            if (enemy.IsDead)
                continue;
            if (!this.ActiveForUntargetable && !enemy.IsTargetable)
                continue;
            if (!this.ActiveWhileCasting && enemy.CastInfo != null)
                continue;
            if (this.World.Actors.Find(enemy.TargetID) is { } target)
                origins.Add((this.OriginAtTarget ? target : enemy, target, Angle.FromDirection(target.Position - enemy.Position)));
        }
        return origins;
    }
}
