// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.ScaleModel;

public enum OID : uint
{
    DemiMedusa = 0x4C6A,
    Helper = 0x233C,
    DefectiveLamia = 0x4DD8, // R2.500, x0 (spawn during fight)
    DemiMedusa1 = 0x4C6C, // R1.000, x0 (spawn during fight)
    DemiMedusa2 = 0x4EC1, // R0.500, x0 (spawn during fight)
    DemiMedusa3 = 0x4EC2, // R0.500, x0 (spawn during fight)
    DemiMedusa4 = 0x4CAE, // R0.500, x0 (spawn during fight)
    DefectiveLamia1 = 0x4DD7, // R2.500, x0 (spawn during fight)
    DefectiveLamia2 = 0x4DD6, // R2.500, x0 (spawn during fight)
    DefectiveLamia3 = 0x4DD5, // R2.500, x0 (spawn during fight)
    DefectiveLamia4 = 0x4D54, // R2.500, x0 (spawn during fight)
    DefectiveLamia5 = 0x4D52, // R2.500, x0 (spawn during fight)
    DefectiveLamia6 = 0x4D53, // R2.500, x0 (spawn during fight)
    DefectiveLamia7 = 0x4D51, // R2.500, x0 (spawn during fight)
    CrescentAdamantoise = 0x4E28, // R5.000, x?
}

public enum AID : uint
{
    AutoAttack = 50538, // DemiMedusa->player, no cast, single-target
    Summon = 48300, // DemiMedusa->self, 3.0s cast, single-target
    CursedSight = 48252, // DemiMedusa->self, 5.0s cast, range 60 60.000-degree cone
    CursedSight1 = 48253, // 4DD6/4DD8/4DD7/4DD5/4D52/4D51/4D54/4D53->self, 5.0s cast, range 60 60.000-degree cone
    LamianLesion = 48254, // DemiMedusa->self, 5.0s cast, range 25 180.000-degree cone
    DarkCast = 48255, // DemiMedusa->self, 3.0s cast, single-target
    Dark = 48256, // 4C6C/4CAE/4EC1/4EC2->location, 3.0s cast, range 6 circle
    TortoiseStomp = 50598, // 4E28->self, 3.0s cast, range 8 circle
}

sealed class CursedSight(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.CursedSight, (uint)AID.CursedSight1],
    new AOEShapeCone(60.0f, 30.0f.Degrees()));
sealed class LamianLesion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LamianLesion, new AOEShapeCone(25.0f, 90.0f.Degrees()));
sealed class Dark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Dark, 6f);
sealed class TortoiseStomp(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TortoiseStomp, 8f);

[SkipLocalsInit]
sealed class DemiMedusaStates : StateMachineBuilder
{
    public DemiMedusaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CursedSight>()
            .ActivateOnEnter<LamianLesion>()
            .ActivateOnEnter<Dark>()
            .ActivateOnEnter<TortoiseStomp>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, CFCID = 1093u, NameID = 2083u, PrimaryActorOID = (uint)OID.DemiMedusa, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
public sealed class DemiMedusa(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
