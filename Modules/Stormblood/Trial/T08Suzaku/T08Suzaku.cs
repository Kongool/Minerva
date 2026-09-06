// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T08Suzaku;

class ScarletFever(ModuleBase module) : ModuleComponent(module)
{
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ScarletFever)
            Bounds = T08Suzaku.Phase2Arena;
    }
}

class ScreamsOfTheDamned(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ScreamsOfTheDamned);
class SouthronStar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SouthronStar);
class AshesToAshes(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AshesToAshes);
class ScarletFeverAOE(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ScarletFever);

class RuthlessRefrain(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.RuthlessRefrain, 8);
class Cremate(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Cremate);
class PhantomFlurryTankbuster(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.PhantomFlurryTankbuster);
class PhantomFlurryAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PhantomFlurryAOE, new AOEShapeCone(41, 90.Degrees()));
class FleetingSummer(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FleetingSummer, new AOEShapeCone(40, 45.Degrees()));
class Hotspot(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hotspot, new AOEShapeCone(21, 60.Degrees()));
class Swoop(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Swoop, new AOEShapeRect(55, 3));
class WellOfFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WellOfFlame, new AOEShapeRect(41, 10));

[ModuleInfo(CFCID = 596u, NameID = 6221u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class T08Suzaku(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsCircle(20))
{
    private static readonly WPos ArenaCenter = new(100, 100);
    public static readonly ArenaBoundsCustom Phase2Arena = new([new Donut(ArenaCenter, 4, 20)]);
}
