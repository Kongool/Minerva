// BossmodReborn's components for this fight that Minerva's own module (V3Lala.cs, written from recordings before
// BossmodReborn had one) has nothing for: Aero II and Volcanic Coordinates. Activated from that module's states. Everything
// else in BossmodReborn's file covers actions the hand-written module already handles, and on the recorded routes the
// hand-written handling does better in the closed loop (review/C8.md), so it was left out. Ported 2026-11-26 from
// BossmodReborn 1aa851810; not seen in a recording unless the hand-written module's notes say so.
// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V03Aloalo.V033TheLala;

public enum OID : uint
{
    Boss = 0x4039, // R4.500, x1
    Helper = 0x233C, // R0.500, x?, Helper type
    Rodiaki = 0x403C, // R2.000, x?, Faunal Figure armadillos
    ArrowBright = 0x1EB941, // R0.500, EventObj type, Arcane Plot arrows
    ArrowDim = 0x1EB942, // R0.500, EventObj type, Arcane Plot redirect arrows
    Kapokapo = 0x403B, // R2.040, x?, route 6 Floral Figure seedlings
    AloaloGolem = 0x403D, // R1.900, x?, route 7 Constructive Figure golems
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Teleport = 34932, // Boss->location, no cast, single-target

    InfernoTheorem = 34943, // Boss->self, 5.0s cast, range 80 circle, raidwide
    StrategicStrike = 34942, // Boss->players, 5.0s cast, single-target, tankbuster

    ArcaneBlightVisual1 = 34927, // Boss->self, 6.0s cast, single-target
    ArcaneBlightVisual2 = 34928, // Boss->self, 6.0s cast, single-target
    ArcaneBlightVisual3 = 34929, // Boss->self, 6.0s cast, single-target
    ArcaneBlightVisual4 = 34930, // Boss->self, 6.0s cast, single-target
    ArcaneBlight = 34931, // Helper->self, 6.0s cast, range 60 270-degree cone

    ArcanePlot = 34933, // Boss->self, 5.0s cast, single-target
    ArcanePlotAlt = 34934, // Boss->self, 5.0s cast, single-target
    BrightPulseFirst = 34936, // Helper->self, 12.0s cast, range 8 width 8 rect
    BrightPulseRest = 34937, // Helper->self, no cast, range 8 width 8 rect

    Analysis = 34939, // Boss->self, 3.0s cast, single-target, apply Unseen
    TargetedLight = 36060, // Boss->self, 9.5+0.5s cast, single-target
    TargetedLightAOE = 36061, // Helper->player, 10.0s cast, single-target, inverted gaze

    CalculatedTrajectory = 34941, // Boss->self, 3.0s cast, single-target, Forced March setup

    FaunalFigure = 34946, // Boss->self, 3.0s cast, single-target, spawn Rodiaki
    FlailSmash = 34947, // Rodiaki->location, 7.5s cast, range 80 circle, proximity
    FlailSmashLong = 35436, // Rodiaki->location, 20.0s cast, range 80 circle, proximity (with Arcane Plot)

    FloralFigure = 34944, // Boss->self, 3.0s cast, single-target, spawn Kapokapo
    RollingSpout = 34945, // Kapokapo->self, 5.0s cast, range 4-12 donut

    ConstructiveFigure = 34948, // Boss->self, 3.0s cast, single-target, spawn AloaloGolem
    AeroII = 34949, // AloaloGolem->self, 9.3s cast, range 50 width 8 rect

    VolcanicCoordinates = 36140, // Boss->self, 3.0s cast, single-target, visual
    VolcanicCoordinatesAOE = 36141, // Helper->location, 5.0s cast, range 6 circle
}

public enum SID : uint
{
    TimesThreePlayer = 3721, // Boss->player
    TimesFivePlayer = 3790, // Boss->player
    FrontUnseen = 3726, // Boss->player
    BackUnseen = 3727, // Boss->player
    RightUnseen = 3728, // Boss->player
    LeftUnseen = 3729, // Boss->player
    ForwardMarch = 3715, // Boss->player
    ForcedMarch = 3719, // Boss->player
}

public enum IconID : uint
{
    BossRotateCW = 484, // Boss
    BossRotateCCW = 485, // Boss
    PlayerRotateCW = 493, // player
    PlayerRotateCCW = 494, // player
}

public enum TetherID : uint
{
    TargetedLight = 17, // player->Boss
}

// I bastardised this from criterion

sealed class AeroII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AeroII, new AOEShapeRect(50, 4));
sealed class VolcanicCoordinates(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VolcanicCoordinatesAOE, 6);

