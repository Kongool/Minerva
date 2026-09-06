// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS3Dahu;

sealed class FallingRock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRock, 4f);
sealed class HotCharge(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.HotCharge, 4f);
sealed class Firebreathe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Firebreathe, new AOEShapeCone(60f, 45f.Degrees()));
sealed class HeadDown(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.HeadDown, 2f);
sealed class HuntersClaw(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HuntersClaw, 8f);

sealed class Burn(ModuleBase module) : Components.BaitAwayIcon(module, 30f, (uint)IconID.Burn, (uint)AID.Burn, 8.2f);

[ModuleInfo(CFCID = 761u, NameID = 9751u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class DRS3Dahu(WorldState ws, Actor primary) : Dahu(ws, primary)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        base.DrawEnemies(pcSlot, pc);
        Arena.Actors(Enemies((uint)OID.CrownedMarchosias));
    }
}
