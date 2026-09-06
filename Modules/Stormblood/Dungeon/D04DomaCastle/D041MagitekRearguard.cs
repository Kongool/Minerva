// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Dungeon.D04DomaCastle.D041MagitekRearguard;

public enum OID : uint
{
    Boss = 0x1BCC, // R3.5
    RearguardBit = 0x1BCF, // R0.9
    RearguardMine = 0x1BCE, // R0.9
    Helper = 0x1BCD
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    CermetPile = 8349, // Boss->self, no cast, range 40+R width 6 rect
    GarleanFireVisual = 8350, // Boss->self, 3.0s cast, single-target
    GarleanFire = 8351, // Helper->self, 3.0s cast, range 6 circle
    MagitekRay = 8353, // RearguardBit->self, 3.0s cast, range 45+R width 2 rect
    SelfDetonate = 8352 // RearguardMine->self, 3.0s cast, range 6 circle
}

class MagitekRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekRay, new AOEShapeRect(45.9f, 1));
class CermetPile(ModuleBase module) : Components.Cleave(module, (uint)AID.CermetPile, new AOEShapeRect(43.5f, 3), activeWhileCasting: false);

abstract class Circle6(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 6);
class GarleanFire(ModuleBase module) : Circle6(module, (uint)AID.GarleanFire);
class SelfDetonate(ModuleBase module) : Circle6(module, (uint)AID.SelfDetonate);

class RearguardMine(ModuleBase module) : Components.Voidzone(module, 0.9f, m => m.Enemies((uint)OID.RearguardMine).Where(x => !x.IsDead), 10);

class D041MagitekRearguardStates : StateMachineBuilder
{
    public D041MagitekRearguardStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MagitekRay>()
            .ActivateOnEnter<CermetPile>()
            .ActivateOnEnter<GarleanFire>()
            .ActivateOnEnter<SelfDetonate>()
            .ActivateOnEnter<RearguardMine>();
    }
}

[ModuleInfo(CFCID = 241u, NameID = 6200u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D041MagitekRearguard(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(124.64f, 17.54f), new ArenaBoundsSquare(19.5f, 76.5f.Degrees()));