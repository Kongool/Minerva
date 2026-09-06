// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Hunt.RankA.Gajasura;

public enum OID : uint
{
    Boss = 0x1ABF, // R=3.23
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Spin = 8188, // Boss->self, 3.0s cast, range 5+R 120-degree cone
    Hurl = 8187, // Boss->location, 3.0s cast, range 6 circle
    Buffet = 8189, // Boss->none, 3.0s cast, single-target, randomly hits a target that isn't tanking, only happens when at least 2 actors are in combat with Gajasura (chocobos count)
}

class Spin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Spin, new AOEShapeCone(8.23f, 60.Degrees()));
class Hurl(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hurl, 6);
class Buffet(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Buffet, "Heavy damage on random target (except tank)");

class GajasuraStates : StateMachineBuilder
{
    public GajasuraStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Spin>()
            .ActivateOnEnter<Hurl>()
            .ActivateOnEnter<Buffet>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 5998u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Gajasura(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
