// Written from minerva-20260920-164937 plus the game's own action sheets. This is the VARIANT Ketuduke,
// which shares its mechanics and nothing else with the criterion one: Another Aloalo Island's Ketuduke is
// CFC 979/980 and uses actions 35496-35565, this is CFC 961 and uses 35449-35494. BossmodReborn has no V3
// Aloalo Island at all, so there is nothing upstream to port; C031Ketuduke next door is the reference for
// what each mechanic does, not for its ids.
using System;
using Minerva;

namespace Minerva.Endwalker.Variant.V3AloaloIsland.V3Ketuduke;

public enum OID : uint
{
    Boss = 0x4091,              // Ketuduke
    Helper = 0x233C,
    SpringCrystal = 0x4092,     // R4.2, casts the Saturate lines and spheres
    SpringCrystal2 = 0x4093,
    AiryBubble = 0x4095,        // R1.3, the bubbles left on the floor
    ZealBlindZozone = 0x4096,
    SummonedApa = 0x4113,       // R2.88, summoned add, hardcasts Water III
    Matsya = 0x3FE7,            // the NPC standing in, not part of the fight
}

public enum AID : uint
{
    SpringCrystals = 35449,         // Ketuduke->self, 2.7s cast, single-target: summons the crystals
    SaturateSphere = 35452,         // SpringCrystal->self, 2.7s cast, range 8 circle
    SaturateSphereFast = 35453,     // SpringCrystal->self, 0.7s cast, range 8 circle
    SaturateRect = 35454,           // SpringCrystal->self, 2.7s cast, range 76 width 10 rect
    SaturateRectFast = 35455,       // SpringCrystal->self, 0.7s cast, range 76 width 10 rect
    BubbleNetVisual = 35456,        // Ketuduke->self, 4.7s cast, single-target
    BubbleNet = 35457,              // Ketuduke->self, 4.7s cast, range 65 circle: the raidwide
    FlukeTyphoonVisual = 35460,     // Ketuduke->self, 2.7s cast, single-target
    FlukeTyphoon = 35461,           // Ketuduke->self, 4.7s cast, range 40 width 40 rect
    StrewnBubbles = 35462,          // Ketuduke->self, 2.7s cast, single-target
    BlowingBubbles = 35464,         // Ketuduke->self, 2.7s cast, single-target
    Summon = 35470,                 // ZealBlindZozone->self, 2.7s cast, single-target
    RecedingTwintides = 35485,      // Ketuduke->self, 4.7s cast, range 14 circle: get out
    NearTide = 35486,               // Ketuduke->self, range 14 circle: get out
    EncroachingTwintides = 35487,   // Ketuduke->self, range 8-60 donut: get in
    FarTide = 35488,                // Ketuduke->self, 1.2s cast, range 8-60 donut: get in
    HydrobombVisual = 35489,        // Ketuduke->self, 2.7s cast, single-target
    Hydrobomb = 35490,              // Ketuduke->location, 2.7s cast, range 5 circle puddle
    TidalRoarVisual = 35493,        // Ketuduke->self, 4.7s cast, single-target
    TidalRoar = 35494,              // Helper->self, range 100 circle: the raidwide
    WaterIII = 36116,               // SummonedApa->player, 9.7s hardcast, single-target
}

sealed class SaturateSpheres(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SaturateSphere, (uint)AID.SaturateSphereFast], 8f);
sealed class SaturateRects(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SaturateRect, (uint)AID.SaturateRectFast], new AOEShapeRect(76f, 5f));
sealed class FlukeTyphoon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlukeTyphoon, new AOEShapeRect(40f, 20f));
sealed class Hydrobomb(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hydrobomb, 5f);
sealed class BubbleNet(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BubbleNet);
sealed class TidalRoar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TidalRoarVisual);

/// <summary>
/// The in-and-out pair, drawn as two components because the answer is opposite: the circles want you out
/// and the donuts want you in, and a fight that alternates them punishes reading one as the other.
///
/// <para>The donut's inner edge is the one number the sheet does not carry, so it was measured instead:
/// in the recorded pull nobody inside 7.6 yalms was touched and the nearest player it caught stood at
/// 10.2, which puts the real edge between the two. The sheet's 8 sits inside that window, on the side that
/// draws more danger rather than less, so it is kept.</para>
/// </summary>
sealed class Twintides(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RecedingTwintides, (uint)AID.NearTide], 14f);
sealed class Tides(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.EncroachingTwintides, (uint)AID.FarTide], new AOEShapeDonut(8f, 60f));

/// <summary>The bubbles left standing on the floor. Radius is the actor's own hitbox.</summary>
sealed class AiryBubble(ModuleBase module) : Components.Voidzone(module, 1.3f, (uint)OID.AiryBubble);

sealed class V3KetudukeStates : StateMachineBuilder
{
    public V3KetudukeStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<SaturateSpheres>()
            .ActivateOnEnter<SaturateRects>()
            .ActivateOnEnter<FlukeTyphoon>()
            .ActivateOnEnter<Hydrobomb>()
            .ActivateOnEnter<Twintides>()
            .ActivateOnEnter<Tides>()
            .ActivateOnEnter<AiryBubble>()
            .ActivateOnEnter<BubbleNet>()
            .ActivateOnEnter<TidalRoar>();
    }
}

/// <summary>
/// Ketuduke, a route boss of the Aloalo Island variant dungeon.
///
/// <para>The arena is measured rather than guessed: 22,849 movement samples over the pull span 19.7 yalms
/// either side of -789.8 in x and 20.0 either side of -392.5 in z, with 468 of them in corners a circle
/// would not have -- a square of half-size twenty, which is what the criterion version of this room uses
/// too. The extractor proposed a circle, which would have forbidden those corners.</para>
///
/// <para>Left uncovered, each single-target by the sheet and each a wind-up for something else: Spring
/// Crystals (the crystals then cast the lines that are covered), the Bubble Net and Tidal Roar visuals
/// (their damage is covered), Fluke Typhoon's visual, Strewn Bubbles, Blowing Bubbles and Summon. The two
/// bubble mechanics are the real gap -- the pull carries two tethers that were not analysed, and the
/// criterion module next door solves them with dedicated components. That is the next piece of work on
/// this fight, along with Water III, which the summoned add hardcasts at one player for ten seconds.</para>
/// </summary>
[ModuleInfo(CFCID = 961u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva, from a recording")]
public sealed class V3Ketuduke(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-789.8f, -392.5f).Quantized(), new ArenaBoundsSquare(20f));
