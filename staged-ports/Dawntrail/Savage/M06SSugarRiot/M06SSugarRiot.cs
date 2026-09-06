// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M06SSugarRiot;

sealed class SprayPain1 : Components.SimpleAOEs
{
    public SprayPain1(ModuleBase module) : base(module, (uint)AID.SprayPain1, 10f, 10)
    {
        MaxDangerColor = 5;
    }
}
sealed class SprayPain2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SprayPain2, 10f);
sealed class LightningBolt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightningBolt, 4f);

[ModuleInfo(CFCID = 1022u, NameID = 13822u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M06SSugarRiot(WorldState ws, Actor primary) : Raid.SugarRiotSharedBounds(ws, primary);