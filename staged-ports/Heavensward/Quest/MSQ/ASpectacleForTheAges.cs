// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Quest.MSQ.ASpectacleForTheAges;

public enum OID : uint
{
    Boss = 0x154E,
    Tizona = 0x1552
}

public enum AID : uint
{
    FlamingTizona = 5763, // D25->location, 3.0s cast, range 6 circle
    TheCurse = 5765, // D25->self, 3.0s cast, range 7+R ?-degree cone
}

class FlamingTizona(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlamingTizona, 6);
class TheCurse(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheCurse, new AOEShapeDonutSector(2, 7, 90.Degrees()));

class Demoralize(ModuleBase module) : Components.Voidzone(module, 4, m => m.Enemies(0x1E9FA8).Where(e => e.EventState != 7));
class Tizona(ModuleBase module) : Components.Adds(module, (uint)OID.Tizona, 5);

class FlameGeneralAldynnStates : StateMachineBuilder
{
    public FlameGeneralAldynnStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FlamingTizona>()
            .ActivateOnEnter<TheCurse>()
            .ActivateOnEnter<Demoralize>()
            .ActivateOnEnter<Tizona>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 67775u, NameID = 4739u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class FlameGeneralAldynn(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-35.75f, -205.5f), new ArenaBoundsCircle(15));
