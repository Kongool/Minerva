// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A35UltimaP2;

class Redemption(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Redemption);
class Auralight1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Auralight1, new AOEShapeRect(50, 5));
class Auralight2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Auralight2, new AOEShapeRect(25, 5));
class Bombardment(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Bombardment, 6);
class Embrace2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Embrace2, 3);
class GrandCrossAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandCrossAOE, new AOEShapeCross(60, 7.5f));
class Holy(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Holy, 2);
class HolyIVBait(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HolyIVBait, 6);
class HolyIVSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HolyIVSpread, 6);
class Plummet(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Plummet, new AOEShapeRect(15, 7.5f));

class Cataclysm(ModuleBase module) : Components.StayMove(module)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.AccelerationBomb && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            PlayerStates[slot] = new(Requirement.Stay, status.ExpireAt);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.AccelerationBomb && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            PlayerStates[slot] = default;
    }
}

[ModuleInfo(CFCID = 636u, NameID = 7909u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A35UltimaP2(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600, -600), new ArenaBoundsSquare(30));
