// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.EyeToEye;

public enum OID : uint
{
    EvilSeer = 0x4BA7,
    Helper = 0x233C,
    EvilSeerHelper = 0x4BAA, // R0.500, x0 (spawn during fight)
    AccursedOrb = 0x4BA8, // R2.000, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 47146, // EvilSeer->player, no cast, single-target
    Ability = 45338, // EvilSeer->player, no cast, single-target
    AllEyes = 47147, // EvilSeer->self, 3.0+0.5s cast, range 30 circle
    JettaturaCast = 47150, // EvilSeer->self, 3.0s cast, single-target
    Jettatura = 47151, // 4BAA->location, 4.0s cast, range 8 circle
    ColdStare = 47149, // EvilSeer->self, 4.0s cast, range 40 90.000-degree cone
    SeeNoEvil = 47148, // EvilSeer->self, 5.0s cast, range 30 circle
    SinisterSight = 47152, // 4BA8->location, 5.0s cast, range 50 circle
}

sealed class AllEyes(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AllEyes);
sealed class Jettatura(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Jettatura, 8f);
sealed class ColdStare(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ColdStare, new AOEShapeCone(40.0f, 45.0f.Degrees()));
sealed class SeeNoEvil(ModuleBase module) : Components.CastGaze(module, (uint)AID.SeeNoEvil);
sealed class SinisterSight(ModuleBase module) : Components.CastGaze(module, (uint)AID.SinisterSight);

[SkipLocalsInit]
sealed class EyeToEyeStates : StateMachineBuilder
{
    public EyeToEyeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AllEyes>()
            .ActivateOnEnter<SeeNoEvil>()
            .ActivateOnEnter<Jettatura>()
            .ActivateOnEnter<ColdStare>()
            .ActivateOnEnter<SinisterSight>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 2075u, PrimaryActorOID = (uint)OID.EvilSeer, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
// BMR derives this from OpenWorldFate, which follows the boss and gates activation on being within 30y.
// Minerva takes a fixed centre, so this is the centre of the cast locations across a full pull
// (recording 2026-08-19, 87 ground casts spanning 52x50y) with BMR's own 30y FATE radius.
public sealed class EyeToEye(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(511.7f, -31f), new ArenaBoundsCircle(30f));
