// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.TheOracleOfLight;

public enum OID : uint
{
    Boss = 0x299D,
    Helper = 0x233C,
}

public enum AID : uint
{
    HotPursuit1 = 17622, // 2AF0->location, 3.0s cast, range 5 circle
    NexusOfThunder1 = 17621, // 2AF0->self, 7.0s cast, range 60+R width 5 rect
    NexusOfThunder2 = 17823, // 2AF0->self, 8.5s cast, range 60+R width 5 rect
    Burn = 18035, // 2BE6->self, 4.5s cast, range 8 circle
    UnbridledWrath = 18036, // 299E->self, 5.5s cast, range 90 width 90 rect
}

class HotPursuit(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HotPursuit1, 5);

abstract class NoT(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(60.5f, 2.5f));
class NexusOfThunder1(ModuleBase module) : NoT(module, (uint)AID.NexusOfThunder1);
class NexusOfThunder2(ModuleBase module) : NoT(module, (uint)AID.NexusOfThunder2);

class Burn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Burn, 8, 8);
class UnbridledWrath(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.UnbridledWrath, 20, kind: Kind.DirForward, stopAtWall: true);

class RanjitStates : StateMachineBuilder
{
    public RanjitStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HotPursuit>()
            .ActivateOnEnter<NexusOfThunder1>()
            .ActivateOnEnter<NexusOfThunder2>()
            .ActivateOnEnter<Burn>()
            .ActivateOnEnter<UnbridledWrath>()
            ;
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68841u, NameID = 8374u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ranjit(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(126.75f, -311.25f), new ArenaBoundsCircle(20));
