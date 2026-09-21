// Written from minerva-20260920-164937 plus the game's own action sheets. This is the VARIANT Ketuduke,
// which shares its mechanics and nothing else with the criterion one: Another Aloalo Island's Ketuduke is
// CFC 979/980 and uses actions 35496-35565, this is CFC 961 and uses 35449-35494. BossmodReborn has no V3
// Aloalo Island at all, so there is nothing upstream to port; C031Ketuduke next door is the reference for
// what each mechanic does, not for its ids.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    ShatterMarker = 0x1EB936,   // the event object that parks where a shatter line will run
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
/// The alternating lines, read from the markers that park where each one will run.
///
/// <para>Left, right, left, right down a column and then again: four lines ten yalms apart, each facing
/// the opposite way to its neighbour, and **no cast bar on any of them**. Keying the component on the cast
/// covered nothing, which is why this caught the recorded character at 125.6s of the 19:46 pull with an
/// empty arena drawn.</para>
///
/// <para>What announces them is an event object, one per line. In that pull four parked at
/// (-790, -380), (-790, -390), (-790, -400) and (-790, -410) at 119.41s facing -90, +90, -90 and +90, and
/// the lines that fired six seconds later matched those facings exactly. So the markers are the telegraph,
/// and they give six seconds of warning where the cast gives none.</para>
///
/// <para>Rinse and repeat: after a volley resolves the clock is wound forward again rather than the
/// drawing being dropped, because the markers stay put and fire again. When the mechanic really is over
/// they despawn and this empties with them.</para>
/// </summary>
sealed class SphereShatter(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect Shape = new(20f, 5f);

    /// <summary>Marker spawn to the line landing, measured at 119.41s and 125.6s of the 19:46 pull.</summary>
    private const double Lead = 6.2d;

    private readonly List<AOEInstance> aoes = [];
    private DateTime fires;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.ShatterMarker)
            this.fires = World.FutureTime(Lead);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SphereShatter)
            this.fires = World.FutureTime(Lead);   // the set repeats while the markers stand
    }

    public override void Update()
    {
        this.aoes.Clear();
        foreach (var marker in World.Actors)
            if (marker.OID == (uint)OID.ShatterMarker && !marker.IsDestroyed)
                this.aoes.Add(new(Shape, marker.Position, marker.Rotation, this.fires));
    }
}
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
/// <para>The arena is derived from the boss rather than written down, because this duty puts the same
/// boss in a different room each run: Quaqua was fought at (-538, 94) and again at (50, -160), and a
/// hardcoded centre meant the dodge judged an arena that was several hundred yalms away -- every cell off
/// the floor, no safe spot anywhere, the module loaded and useless. The boss spawns on the room's axis
/// facing into it, so the centre is a fixed step along its own facing and survives a room that is turned
/// around.</para>
///
/// <para>Ketuduke spawns twelve yalms short of its room centre, which is why the step is twelve: from its
/// spawn at (-790, -405) that lands on (-790, -393), the centre measured from 22,154 in-combat samples.
/// The floor runs 21 yalms either side of it and the corners are populated, so it is a square and not the
/// circle the extractor proposed. Half-22 would cover every last sample, which is the wrong side to err on
/// for a boundary nobody has proven is solid.</para>
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
    : ModuleBase(ws, primary, (primary.Position + (12f * primary.Rotation.ToDirection())).Quantized(), new ArenaBoundsSquare(21f));
