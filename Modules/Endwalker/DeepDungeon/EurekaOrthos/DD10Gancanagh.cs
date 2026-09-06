// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.DeepDungeon.EurekaOrthos.DD10Gancanagh;

public enum OID : uint
{
    Boss = 0x3D54, // R1.8
    PachypodiumMine = 0x3D55 // R1.5
}

public enum AID : uint
{
    Attack = 6499, // Boss->player, no cast, single-target

    AuthoritativeShriek = 31477, // Boss->self, 3.0s cast, single-target
    Mandrashock1 = 31478, // PachypodiumMine->self, 5.0s cast, range 10 circle
    Mandrashock2 = 32700, // PachypodiumMine->self, 8.0s cast, range 10 circle
    Mandrastorm = 31479 // Boss->self, 5.0s cast, range 60 circle, damage fall off AOE
}

class MandraStorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Mandrastorm, 20f);
class Mandrashock1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Mandrashock1, 10f);
class Mandrashock2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Mandrashock2, 10f, 6);

class DD10GancanaghStates : StateMachineBuilder
{
    public DD10GancanaghStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Mandrashock1>()
            .ActivateOnEnter<Mandrashock2>()
            .ActivateOnEnter<MandraStorm>();
    }
}

[ModuleInfo(CFCID = 897u, NameID = 12240u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class DD10Gancanagh(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-300f, -300f), new ArenaBoundsSquare(19.5f));
