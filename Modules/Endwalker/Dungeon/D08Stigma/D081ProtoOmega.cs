// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D08Stigma.D081ProtoOmega;

public enum OID : uint
{
    Boss = 0x3417, // R=8.99
    MarkIIGuidedMissile = 0x3418, // R1.000, x0 (spawn during fight)
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    Burn = 25385, // Helper->player, no cast, range 6 circle
    ChemicalMissile = 25384, // Boss->self, 3.0s cast, single-target
    ElectricSlide = 25386, // Boss->players, 5.0s cast, range 6 circle //Stack+Knockback
    GuidedMissile = 25382, // Boss->self, 3.0s cast, single-target //Tethered bait away
    IronKiss = 25383, // MarkIIGuidedMissile->self, no cast, range 3 circle 
    MustardBomb = 25387, // Boss->player, 5.0s cast, range 5 circle
    SideCannons1 = 25376, // Boss->self, 7.0s cast, range 60 180-degree cone
    SideCannons2 = 25377 // Boss->self, 7.0s cast, range 60 180-degree cone
}

class ElectricSlideKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ElectricSlide, 15f, stopAtWall: true);
class ElectricSlide(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.ElectricSlide, 6f, 4, 4);
class IronKiss(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IronKiss, 3f);

class SideCannons(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SideCannons1, (uint)AID.SideCannons2], new AOEShapeCone(60f, 90f.Degrees()));

class MustardBomb(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.MustardBomb);

class D081ProtoOmegaStates : StateMachineBuilder
{
    public D081ProtoOmegaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ElectricSlide>()
            .ActivateOnEnter<ElectricSlideKnockback>()
            .ActivateOnEnter<IronKiss>()
            .ActivateOnEnter<SideCannons>()
            .ActivateOnEnter<MustardBomb>();
    }
}

[ModuleInfo(CFCID = 784u, NameID = 10401u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class D081ProtoOmega(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-144, -136), new ArenaBoundsSquare(20));
