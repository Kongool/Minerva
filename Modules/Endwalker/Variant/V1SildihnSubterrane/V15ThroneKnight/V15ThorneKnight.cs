// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V15ThorneKnight;

sealed class BlisteringBlow(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.BlisteringBlow);
sealed class BlazingBeacon(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BlazingBeacon1, (uint)AID.BlazingBeacon2], new AOEShapeRect(50f, 8f));
sealed class SacredFlay(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SacredFlay1, (uint)AID.SacredFlay2], new AOEShapeCone(50f, 45f.Degrees()));
sealed class SignalFlare : Components.SimpleAOEs
{
    public SignalFlare(ModuleBase module) : base(module, (uint)AID.SignalFlare, 10f, 6)
    {
        MaxDangerColor = 3;
    }
}
sealed class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, new AOEShapeCross(50f, 3f));
sealed class ForeHonor(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForeHonor, new AOEShapeCone(50f, 90f.Degrees()));
sealed class Cogwheel(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Cogwheel);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 868u, CFCID = 868u, NameID = 11419u, PrimaryActorOID = (uint)OID.ThorneKnight, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V15ThorneKnight(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(289f, -230f), new ArenaBoundsSquare(17.5f, 45f.Degrees()));