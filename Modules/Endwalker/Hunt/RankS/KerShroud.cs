// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankS.KerShroud;

public enum OID : uint
{
    Boss = 0x3672, // R2.500, x1
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    AccursedPox = 27725, // Boss->location, 5.0s cast, range 8 circle
    EntropicFlame = 27724, // Boss->self, 4.0s cast, range 60 width 8 rect
}

class AccursedPox(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AccursedPox, 8);

class EntropicFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EntropicFlame, new AOEShapeRect(60, 4));

class KerShroudStates : StateMachineBuilder
{
    public KerShroudStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AccursedPox>()
            .ActivateOnEnter<EntropicFlame>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10616u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class KerShroud(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
