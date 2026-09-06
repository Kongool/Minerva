// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P4S2Hesperos;

// state related to demigod double mechanic (shared tankbuster)
class DemigodDouble(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.DemigodDouble, 6);

// state related to heart stake mechanic (dual hit tankbuster with bleed)
// TODO: consider showing some tank swap / invul hint...
class HeartStake(ModuleBase module) : Components.CastCounter(module, (uint)AID.HeartStakeSecond);

[ModuleInfo(CFCID = 801u, NameID = 10744u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class P4S2(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsCircle(20))
{
    // common wreath of thorns constants
    public const float WreathAOERadius = 20;
    public const float WreathTowerRadius = 4;
}
