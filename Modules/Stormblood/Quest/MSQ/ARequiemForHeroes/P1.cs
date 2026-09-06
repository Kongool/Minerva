// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.ARequiemForHeroes;

public class ZenosP1States : StateMachineBuilder
{
    public ZenosP1States(ModuleBase module) : base(module)
    {
        TrivialPhase();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, PrimaryActorOID = (uint)OID.BossP1, CFCID = 68721u, NameID = 6039u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class ZenosP1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(233f, -93.25f), new ArenaBoundsCircle(20f));
