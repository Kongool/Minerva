// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D27SastashaHard.D272CaptainMadison;

public enum OID : uint
{
    Boss = 0xCBB,
    CaptainsGuard = 0xCBC,
    CaptainsBoy = 0x0CBD,
    CaptainsLeech = 0xCBE,
    CaptainsSlave = 0xCBF
}

public enum AID : uint
{
    Water = 971, // CBE->player, 1.0s cast, single-target
    AutoAttack = 870, // CBD/Boss/CBC->player, no cast, single-target
    Hornswaggle = 3062, // Boss->self, no cast, range 5+R ?-degree cone
    Rive = 1299, // CBD->self, 2.5s cast, range 30+R width 2 rect
    Bloodstain = 1099, // CBC->self, 2.5s cast, range 5 circle
    Thunder = 968, // CBE->player, 1.0s cast, single-target
    Tackle = 3068 // CBF->player, no cast, single-target
}

class Bloodstain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Bloodstain, new AOEShapeCircle(7.5f));

// TODO: Confirm angle
class Hornswaggle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hornswaggle, new AOEShapeCone(5f, 45f.Degrees()));

class Rive(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rive, new AOEShapeRect(18f, 1.5f));

class Water(ModuleBase module) : Components.SingleTargetInstant(module, (uint)AID.Water, 1f);

class CaptainMadisonAdds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.CaptainsGuard, (uint)OID.CaptainsBoy, (uint)OID.CaptainsLeech]);

class D272CaptainMadisonStates : StateMachineBuilder
{
    public D272CaptainMadisonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CaptainMadisonAdds>()
            .ActivateOnEnter<Hornswaggle>()
            .ActivateOnEnter<Rive>()
            .ActivateOnEnter<Water>()
            .ActivateOnEnter<Bloodstain>();
    }
}

[ModuleInfo(CFCID = 28u, NameID = 3015u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Zaventh (ported from BMR)")]
public class D272CaptainMadison(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-103f, 136f), new ArenaBoundsRect(18f, 20f));
