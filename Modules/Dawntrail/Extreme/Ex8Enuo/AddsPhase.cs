// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Extreme.Ex8Enuo;

sealed class LoomingShadowAdd(ModuleBase module) : Components.Adds(module, (uint)OID.LoomingShadow);

sealed class AggressiveShadowAdd(ModuleBase module) : Components.Adds(module, (uint)OID.AggressiveShadow);

sealed class SupportShadowAdds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.ProtectiveShadow, (uint)OID.SoothingShadow]);

sealed class BeaconAdd(ModuleBase module) : Components.Adds(module, (uint)OID.BeaconInTheDark);

sealed class LoomingEmptinessKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.LoomingEmptinessKnockback, 20f);

sealed class LoomingEmptinessKillZone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LoomingEmptinessKillzone, new AOEShapeCircle(8f));

sealed class EmptyShadowTower(ModuleBase module) : Components.CastTowers(module, (uint)AID.EmptyShadow, 7f)
{
    private static BitMask badsoakers;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.VoidTurbulanceCone)
        {
            var slot = Raid.FindSlot(targetID);
            badsoakers.Set(slot);
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.VoidalTurbulenceCone)
        {
            badsoakers = default;
        }
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            Towers.Add(new(spell.LocXZ, Radius, MinSoakers, MaxSoakers, activation: Module.CastFinishAt(spell), actorID: caster.InstanceID, forbiddenSoakers: badsoakers));
        }
    }
}

//Cone halfangle is an estimate here, but 30 Degrees looks pretty close.
sealed class VoidalTurbulanceCone(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(60f, 30f.Degrees()), (uint)IconID.VoidTurbulanceCone, (uint)AID.VoidalTurbulenceCone)
{
    public override Actor? BaitSource(Actor target)
    {
        var enemies = Module.Enemies((uint)OID.LoomingShadow);
        var count = enemies.Count;
        if (count == 0)
        {
            return null;
        }
        return enemies.First();
    }
}
sealed class DemonEye(ModuleBase module) : Components.CastGaze(module, (uint)AID.DemonEyeCastbar);

sealed class DrainTouch(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.DrainTouch);

sealed class WeightofNothing(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.WeightOfNothing, new AOEShapeRect(100f, 4f));

sealed class CurseoftheFlesh(ModuleBase module) : Components.CleansableDebuff(module, (uint)SID.Disease);
sealed class Nothingness(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Nothingness, new AOEShapeRect(100f, 2f));
