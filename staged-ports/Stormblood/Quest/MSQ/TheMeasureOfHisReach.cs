// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.TheMeasureOfHisReach;

public enum OID : uint
{
    Boss = 0x1C48,
    Whitefang = 0x1C5A,
    Helper = 0x233C
}

public enum AID : uint
{
    HowlingIcewind = 8397, // 1C4F->self, 2.5s cast, range 44+R width 4 rect
    Dragonspirit = 8450, // 1C5A/1C5B->self, 3.0s cast, range 6+R circle
    HowlingMoonlight = 8398, // 1C59->self, 7.0s cast, range 22+R circle
    HowlingBloomshower = 8399, // 1C4F->self, 2.5s cast, range 8+R ?-degree cone
}

class Moonlight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HowlingMoonlight, 10)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        // hits everyone (proximity damage)
        foreach (var c in Casters)
            hints.AddPredictedDamage(Raid.WithSlot(false, true, false).Mask(), c.Activation);
    }
}
class Icewind(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HowlingIcewind, new AOEShapeRect(44, 2));
class Dragonspirit(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Dragonspirit, 7.5f);
class Bloomshower(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HowlingBloomshower, new AOEShapeDonutSector(4, 8, 45.Degrees()));

class HakuroWhitefangStates : StateMachineBuilder
{
    public HakuroWhitefangStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Icewind>()
            .ActivateOnEnter<Moonlight>()
            .ActivateOnEnter<Dragonspirit>()
            .ActivateOnEnter<Bloomshower>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68088u, NameID = 5975u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class HakuroWhitefang(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(504, -133), new ArenaBoundsCircle(20));
