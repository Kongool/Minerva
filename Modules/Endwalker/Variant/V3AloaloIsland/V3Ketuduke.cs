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
    SphereShatter = 35463,          // Ketuduke->self, range 20 width 10 rect: the shattered sphere's line
    RoarVisual = 35474,             // Ketuduke->self, single-target
    BubbleNetVisual2 = 35475,       // Ketuduke->self, single-target
    BubbleNet2 = 35476,             // Ketuduke->self, range 65 circle: the second raidwide
    UpdraftVisual = 35477,          // Ketuduke->self, single-target
    Updraft = 35478,                // Helper->self, range 35 circle
    AerialShockVisual = 35483,      // Ketuduke->self, single-target
    AerialShock = 35484,            // Helper->self, range 35 circle
    TidalRoarVisual = 35493,        // Ketuduke->self, 4.7s cast, single-target
    TidalRoar = 35494,              // Helper->self, range 100 circle: the raidwide
    WaterIII = 36116,               // SummonedApa->player, 9.7s hardcast, single-target
}

sealed class SaturateSpheres(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SaturateSphere, (uint)AID.SaturateSphereFast], 8f);
/// <summary>
/// The crystals' lines, at the length they actually are.
///
/// <para>Drawn as seventy-six yalms thrown forward, one line swallowed most of the arena and several of
/// them left nothing legible -- the report was that the fill was not needed when only two rows are bad.
/// The sheet's seventy-six is the whole line, not the reach: the community layout for this duty draws it
/// as thirty-five either side of the crystal, and every hit measured in the 19:46 pull landed within
/// eighteen yalms of the crystal, so nothing is lost by centring it.</para>
/// </summary>
sealed class SaturateRects(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SaturateRect, (uint)AID.SaturateRectFast], new AOEShapeRect(38f, 5f, 38f));
sealed class FlukeTyphoon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlukeTyphoon, new AOEShapeRect(40f, 20f));
sealed class Hydrobomb(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hydrobomb, 5f);
sealed class BubbleNet(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.BubbleNet, (uint)AID.BubbleNet2]);

/// <summary>
/// Thirty-five yalms on a forty-two yalm floor, which leaves nowhere worth walking to, so both are named
/// rather than drawn. Updraft is the wind itself and Aerial Shock follows it; neither was covered at all
/// until the 19:46 pull turned them up.
/// </summary>
sealed class WindRaidwides(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.Updraft, (uint)AID.AerialShock]);

/// <summary>
/// The line a shattered crystal throws, and a warning that it may never draw.
///
/// <para>It caught the recorded character at 125.6s of the 19:46 pull with nothing drawn for it. This
/// component is keyed on the cast, and in that pull the action resolved four times with **no cast bar at
/// all** -- so it will cover the telegraphed version if one exists and cover nothing otherwise. What it
/// really needs is the same treatment as Quaqua's scalding waves: find the actor that throws it, confirm
/// it stands still beforehand, and draw the line off its pose. The shattered crystals are the obvious
/// candidate and the next recording with a shatter in it should settle it.</para>
/// </summary>
sealed class SphereShatter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SphereShatter, new AOEShapeRect(20f, 5f));
sealed class TidalRoar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TidalRoarVisual);

/// <summary>
/// All four tides, and they are all the same shape: stand in the middle.
///
/// <para>The names promise an in-and-out pair and the game sheet backs it, calling Receding Twintides and
/// Near Tide fourteen-yalm circles. The fight disagrees. Five casts across three pulls, covering all four
/// ids, spared everyone inside roughly eight yalms and caught everyone past ten: Near Tide spared players
/// at 3.0 and 7.1 while catching 10.1, 12.2 and 14.3, and Receding spared 1.7 and 8.7 in one pull while
/// catching 8.1 and 14.0 in another. A fourteen-yalm circle centred there would have killed the player
/// standing at 1.7, every time.</para>
///
/// <para>Drawing them as circles is the dangerous way to be wrong -- it walks people out of the one safe
/// ring and into the water -- so they are donuts until a pull actually shows a chariot. Splatoon's
/// community layout for this duty calls Encroaching a chariot and Receding a donut, which agrees with
/// neither the sheet nor this; if a recording ever catches someone inside eight yalms taking one of these,
/// that id is the one to split out.</para>
/// </summary>
sealed class Tides(ModuleBase module) : Components.SimpleAOEGroups(module,
    [(uint)AID.RecedingTwintides, (uint)AID.NearTide, (uint)AID.EncroachingTwintides, (uint)AID.FarTide],
    new AOEShapeDonut(8f, 60f));

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
            .ActivateOnEnter<Tides>()
            .ActivateOnEnter<AiryBubble>()
            .ActivateOnEnter<BubbleNet>()
            .ActivateOnEnter<WindRaidwides>()
            .ActivateOnEnter<SphereShatter>()
            .ActivateOnEnter<TidalRoar>();
    }
}

/// <summary>
/// Ketuduke, a route boss of the Aloalo Island variant dungeon.
///
/// <para>The arena is measured, and measured again after the first fit proved half a yalm short. Taking
/// only the 22,154 samples from after the pull -- the approach to the room was dragging the fit north --
/// the floor runs 21 yalms either side of (-789.6, -393.0), and the corners are populated, so it is a
/// square and not the circle the extractor proposed. The first attempt, half-20 about (-789.8, -392.5),
/// put 4.9% of the ground people actually stood on outside the arena, three quarters of it along the north
/// edge; this leaves 0.7%. Half-22 would cover every last sample, which is the wrong side to err on for a
/// boundary nobody has proven is solid.</para>
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
    : ModuleBase(ws, primary, new WPos(-789.6f, -393.0f).Quantized(), new ArenaBoundsSquare(21f));
