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
    /// near), so the pathfinder keeps the cleave pointed at empty ground.
    /// </summary>
    private void AddTargetSpecificHints(Actor actor, Actor source, AIHints hints)
    {
        foreach (var a in this.World.Party.WithoutSlot())
        {
            if (a == actor)
                continue;
            switch (this.Shape)
            {
                case AOEShapeCircle circle:
                    hints.AddForbiddenZone(circle, a.Position);
                    break;
                case AOEShapeCone cone:
                    hints.AddForbiddenZone(new SDCone(source.Position, 100f, source.AngleTo(a), cone.HalfAngle));
                    break;
                case AOEShapeRect rect:
                    var dist = (a.Position - source.Position).Length();
                    if (dist > 0f)
                        hints.AddForbiddenZone(new SDCone(source.Position, 100f, source.AngleTo(a), Angle.Asin(Math.Min(1f, rect.HalfWidth / dist))));
                    break;
            }
        }
    }

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
