// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN2Dahu;

sealed class FallingRock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRock, 4f);
sealed class HotCharge(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.HotCharge, 4f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (((++NumCasts) & 1) == 0)
        {
            return;
        }
        hints.GoalZones.Add(AIHints.GoalSingleTarget(Module.PrimaryActor.CastInfo?.LocXZ ?? Center, 6f, 5f)); // follow the charge
    }
}

sealed class Firebreathe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Firebreathe, new AOEShapeCone(60f, 45f.Degrees()));
sealed class HeadDown(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.HeadDown, 2f);
sealed class HuntersClaw(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HuntersClaw, 8f);
sealed class HeatBreath(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.HeatBreath, new AOEShapeCone(10f, 45f.Degrees()), endsOnCastEvent: true, tankbuster: true);
sealed class RipperClaw(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RipperClaw, new AOEShapeCone(10f, 45f.Degrees()));
sealed class TailSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TailSwing, 10f);

[ModuleInfo(CFCID = 760u, NameID = 9751u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DRN2Dahu(WorldState ws, Actor primary) : Dahu(ws, primary)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        base.DrawEnemies(pcSlot, pc);
        Arena.Actors(Enemies((uint)OID.Marchosias));
    }
}
