// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA1Owain;

sealed class IvoryPalm(ModuleBase module) : Components.GenericGaze(module)
{
    public readonly List<(Actor target, Actor source)> Tethers = [];

    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor)
    {
        var count = Tethers.Count;
        if (count == 0)
            return [];

        for (var i = 0; i < count; ++i)
        {
            var tether = Tethers[i];
            if (tether.target == actor && !tether.source.IsDead) // apparently tethers don't get removed immediately upon death
            {
                return new Eye[1] { new(tether.source.Position, inverted: true) };
            }
        }
        return [];
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var eyes = ActiveEyes(slot, actor);
        var len = eyes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var eye = ref eyes[i];
            if (HitByEye(ref actor, eye) != eye.Inverted)
            {
                hints.Add("Face the hand to petrify it!");
                break;
            }
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        // only tethers in this fight are from this mechanic, so no need to check tether IDs
        Tethers.Add((World.Actors.Find(tether.Target)!, source));
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        Tethers.Remove((World.Actors.Find(tether.Target)!, source));
    }
}

sealed class IvoryPalmExplosion(ModuleBase module) : Components.CastHint(module, (uint)AID.Explosion, "Ivory Palm is enraging!", true);

sealed class EurekanAero(ModuleBase module) : Components.Cleave(module, (uint)AID.EurekanAero, new AOEShapeCone(6f, 60f.Degrees()), [(uint)OID.IvoryPalm])
{
    public override List<(Actor origin, Actor target, Angle angle)> OriginsAndTargets()
    {
        var enemies = Module.Enemies(EnemyOID);
        var count = enemies.Count;
        List<(Actor, Actor, Angle)> origins = [];
        for (var i = 0; i < count; ++i)
        {
            var enemy = enemies[i];
            if (enemy.IsDead || enemy.FindStatus((uint)SID.Petrification) != null)
                continue;

            var target = World.Actors.Find(enemy.TargetID);
            if (target != null)
                origins.Add(new(OriginAtTarget ? target : enemy, target, Angle.FromDirection(target.Position - enemy.Position)));
        }
        return origins;
    }
}
