// Written from minerva-20260920-164018 plus the game's own action sheets. BossmodReborn has V1 Sildihn
// Subterrane and V2 Mount Rokkon but no V3 Aloalo Island at all, so there is nothing upstream to port:
// every shape below is either what the sheet says or what the recording measured.
using System;
using Minerva;

namespace Minerva.Endwalker.Variant.V3AloaloIsland.V3Quaqua;

public enum OID : uint
{
    Boss = 0x40BA,              // Quaqua, R5.25
    Helper = 0x233C,
    AethericCharge = 0x40BB,    // R1.5, spawns in fives and drops the rings
    AnalaFamiliar = 0x4134,     // R1
    DrakeFamiliar = 0x4135,     // R1
    DrakeFamiliarLarge = 0x40BF, // R2.7
    Matsya = 0x3FE7,            // the NPC standing in, not part of the fight
}

public enum AID : uint
{
    ArcaneArmamentsVisual = 35720,  // Quaqua->self, 7.7s cast, single-target: the wind-up for the lines below
    RavagingAxe = 35722,            // AethericCharge->location, range 14 circle
    RingingQuoits = 35723,          // AethericCharge->location, 1.7s cast, range 4-18 donut
    ArcaneArmaments = 35728,        // Quaqua->location, 4.7s cast, single-target
    Rout1 = 35729,                  // Quaqua->location, 4.7s cast, range 45 width 16 rect
    Rout2 = 35730,                  // Quaqua->self, the same rect with NO cast bar: see the Rout component
    ArcaneArmamentsLine = 35731,    // Helper->location, 2.7s cast, range 40 width 10 rect
    MadeMagic = 35732,              // Quaqua->location, 4.7s cast, range 50 circle: the raidwide
    VioletStorm = 35733,            // Quaqua->location, 5.2s cast, range 32 120-degree cone
    Howl = 35734,                   // Quaqua->location, 3.7s cast, single-target
    CloudToGroundVisual = 35739,    // DrakeFamiliarLarge->location, 3.7s cast, single-target
    CloudToGroundSlow = 35740,      // DrakeFamiliar->location, 4.7s cast, range 6 circle
    CloudToGroundFast = 35741,      // DrakeFamiliar->location, 0.7s cast, range 6 circle
}

sealed class RingingQuoits(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RingingQuoits, new AOEShapeDonut(4f, 18f));
sealed class RavagingAxe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RavagingAxe, 14f);

/// <summary>
/// The telegraphed sweep, and a known gap behind it.
///
/// <para>Rout is cast once with a 4.7 second bar and then repeats three more times with no cast bar at
/// all, about 1.6 seconds apart, each at a fresh angle -- 90, then -25, -140, +70 degrees in the one
/// occurrence recorded. The first is drawn here. The other three arrive as resolved events, which is to
/// say the damage is already done by the time anything could draw them, and two of the three landed on
/// the recorded character for a vulnerability stack each.</para>
///
/// <para>The angles ARE fixed: two pulls fifty minutes apart (2026-09-20 16:40 and 17:33) produced the
/// same four to within three hundredths of a degree -- the telegraph, then 115.1 degrees back, 229.8 back
/// and 20.0 back, at about 1.6, 3.2 and 4.9 seconds after it lands. What is not fixed is where they sweep
/// from. The boss dashes between them: it stood at (-518.0, 81.5), then (-534.9, 117.5), then
/// (-560.6, 87.1) during one set, and no single origin reproduces which of the three caught the recorded
/// character and which missed. Predicting the angles from a guessed origin would draw three 45-yalm
/// rectangles across ground that may be safe while missing the ground that is not, so they stay undrawn
/// until a pull pins the origin -- most likely by sampling the boss at the instant each sweep fires rather
/// than at the last movement before it.</para>
/// </summary>
sealed class Rout(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Rout1, (uint)AID.Rout2], new AOEShapeRect(45f, 8f));
sealed class ArcaneArmamentsLine(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArcaneArmamentsLine, new AOEShapeRect(40f, 5f));
sealed class VioletStorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VioletStorm, new AOEShapeCone(32f, 60f.Degrees()));
sealed class MadeMagic(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MadeMagic);

/// <summary>
/// The drakes drop six-yalm circles where they aim, in a fast and a slow flavour. Grouped because they
/// land in waves -- 24 fast casts in one minute of the recording -- and a wave read one circle at a time
/// is a wave nobody can plan around.
/// </summary>
sealed class CloudToGround(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.CloudToGroundSlow, (uint)AID.CloudToGroundFast], 6f);

sealed class V3QuaquaStates : StateMachineBuilder
{
    public V3QuaquaStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<RingingQuoits>()
            .ActivateOnEnter<RavagingAxe>()
            .ActivateOnEnter<Rout>()
            .ActivateOnEnter<ArcaneArmamentsLine>()
            .ActivateOnEnter<VioletStorm>()
            .ActivateOnEnter<CloudToGround>()
            .ActivateOnEnter<MadeMagic>();
    }
}

/// <summary>
/// Quaqua, a route boss of the Aloalo Island variant dungeon.
///
/// <para>The arena is a guess and deliberately a small one. The recording that produced this module is
/// half a pull -- a plugin reload split it -- and half a pull does not show where the floor ends. A circle
/// that is too small only costs the dodge some ground it could have used; one that is too large walks
/// somebody off the edge. Widen it once a whole pull has been watched.</para>
///
/// <para>Unclassified, and each one single-target by the sheet, so drawn as nothing rather than as a
/// guess: Arcane Armaments (the 7.7s wind-up and its 4.7s follow-up), Howl, and the drakes' Cloud to
/// Ground visual. The wind-up is almost certainly the telegraph for the 40-yalm lines that follow it,
/// which is worth confirming against a full recording.</para>
/// </summary>
[ModuleInfo(CFCID = 961u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva, from a recording")]
public sealed class V3Quaqua(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-538f, 94.8f).Quantized(), new ArenaBoundsCircle(21f));
