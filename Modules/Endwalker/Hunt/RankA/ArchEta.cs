// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankA.ArchEta;

public enum OID : uint
{
    Boss = 0x35C0 // R7.200, x1
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    EnergyWave = 27269, // Boss->self, 3.0s cast, range 40 width 14 rect
    TailSwipe = 27270, // Boss->self, 4.0s cast, range 25 90-degree cone
    HeavyStomp = 27271, // Boss->self, 3.0s cast, range 17 circle
    SonicHowl = 27272, // Boss->self, 5.0s cast, range 30 circle
    SteelFang = 27273, // Boss->player, 5.0s cast, single-target
    FangedLunge = 27274 // Boss->player, no cast, single-target
}

class EnergyWave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EnergyWave, new AOEShapeRect(40, 7));
class TailSwipe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TailSwipe, new AOEShapeCone(25, 45.Degrees()));
class HeavyStomp(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeavyStomp, 17);
class SonicHowl(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SonicHowl);
class SteelFang(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.SteelFang);

class ArchEtaStates : StateMachineBuilder
{
    public ArchEtaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<EnergyWave>()
            .ActivateOnEnter<TailSwipe>()
            .ActivateOnEnter<HeavyStomp>()
            .ActivateOnEnter<SonicHowl>()
            .ActivateOnEnter<SteelFang>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10634u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class ArchEta(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
