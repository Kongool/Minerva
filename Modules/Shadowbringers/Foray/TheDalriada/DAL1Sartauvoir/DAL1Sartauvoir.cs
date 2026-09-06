// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL1Sartauvoir;

sealed class PyrokinesisAOE(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.PyrokinesisAOE);

sealed class Flamedive(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Flamedive, new AOEShapeRect(55f, 2.5f));
sealed class BurningBlade(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.BurningBlade);

sealed class MannatheihwonFlameRW(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MannatheihwonFlameRaidwide);
sealed class MannatheihwonFlameRect(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MannatheihwonFlameRect, new AOEShapeRect(50f, 4f));
sealed class MannatheihwonFlameCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MannatheihwonFlameCircle, 10f);

sealed class Brand(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.LeftBrand, (uint)AID.RightBrand], new AOEShapeCone(40f, 90f.Degrees()));

sealed class Pyrocrisis(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Pyrocrisis, 6f);
sealed class Pyrodoxy(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Pyrodoxy, 6f, 8);

sealed class ThermalGustAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThermalGustAOE, new AOEShapeRect(44f, 5f));
sealed class GrandCrossflameAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandCrossflameAOE, new AOEShapeCross(40f, 9f));
sealed class TimeEruption(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ReverseTimeEruption1, (uint)AID.ReverseTimeEruption2,
(uint)AID.TimeEruption1, (uint)AID.TimeEruption2], new AOEShapeRect(20f, 10f), 2, 4);

[ModuleInfo(Group = ModuleGroup.TheDalriada, GroupID = 778u, CFCID = 778u, NameID = 9384u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DAL1Sartauvoir(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(631f, 157f), new ArenaBoundsSquare(19f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
    }
}
