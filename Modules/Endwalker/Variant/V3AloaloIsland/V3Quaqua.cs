// Written from minerva-20260920-164018 plus the game's own action sheets. BossmodReborn has V1 Sildihn
// Subterrane and V2 Mount Rokkon but no V3 Aloalo Island at all, so there is nothing upstream to port:
// every shape below is either what the sheet says or what the recording measured.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    HammerLanding = 35725,          // Quaqua->location, range 40 circle: raidwide-sized on this floor
    HammerLanding2 = 35726,         // the repeats, same circle
    MadeMagic = 35732,              // Quaqua->location, 4.7s cast, range 50 circle: the raidwide
    ArcaneArmaments3 = 35743,       // Quaqua->self, single-target
    ElementalImpact = 35744,        // range 14 circle
    FlowingLance = 35745,           // range 24 width 12 cross
    FlowingLance2 = 36049,          // the same cross
    ScaldingWavesWide = 35735,      // AnalaFamiliar->location, range 50 width 8 rect
    ScaldingWaves = 35736,          // AnalaFamiliar->location, range 50 width 4 rect, in waves
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
/// Kept as a raidwide, and not confidently.
///
/// <para>The sheet calls it a forty-yalm circle, which on this floor is everywhere, and it did hit the
/// whole party three times in the 19:05 pull. Splatoon's community layout for this duty calls it a
/// fourteen-yalm chariot instead -- dodgeable, if true. The recording cannot settle it: the three hammers
/// land two seconds apart while everyone is moving, so the hit distances from one landing point come out
/// as 9.6, 9.7 and 9.9 caught with 10.9 and 11.7 spared, which is no circle at all and simply means the
/// positions are a frame or two stale.</para>
///
/// <para>Naming it a raidwide is the conservative reading: it warns, and it never tells anyone that ground
/// forty yalms out is safe when it is not. A pull where the party holds still through one -- or one that
/// spares somebody standing well clear -- decides it.</para>
/// </summary>
sealed class HammerLanding(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.HammerLanding, (uint)AID.HammerLanding2]);

/// <summary>
/// The familiars' lines, which are dodgeable and were not drawn at all until now -- twenty of the narrow
/// ones in the 19:05 pull, one of which caught the recorded character while they were mid-cast. Two
/// components because the wide and narrow versions are genuinely different widths, and drawing the narrow
/// one at the wide one's size would forbid ground that is safe.
/// </summary>
/// <summary>The wide wave. Centred on the familiar, not thrown forward from it -- see ScaldingWaves.</summary>
/// <summary>Two mechanics from a route the earlier pulls never took, both dodgeable and both drawn by
/// nothing until the 19:59 pull: a fourteen-yalm circle, and a cross whose arms the sheet gives as
/// twenty-four by twelve.</summary>
sealed class ElementalImpact(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalImpact, 14f);
sealed class FlowingLance(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.FlowingLance, (uint)AID.FlowingLance2], new AOEShapeCross(24f, 6f));

sealed class ScaldingWavesWide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScaldingWavesWide, new AOEShapeRect(25f, 4f, 25f));

/// <summary>
/// The narrow waves, read from the familiars rather than from a cast bar they do not have.
///
/// <para>Nothing telegraphs an individual shot: no cast, no icon, no tether. What does telegraph it is the
/// familiars themselves. They teleport into a row along z=94 at four-yalm spacing, stand **perfectly
/// still** -- 0.0 yalms of movement in the second before every shot measured in the 19:05 pull -- and fire
/// along their own facing, which matched the line they cast to a tenth of a degree every time. So each
/// parked familiar is a fifty-yalm line waiting to happen, and drawing it from the actor gives the roughly
/// two seconds between waves as warning where there was none at all.</para>
///
/// <para>Twenty of these landed in that pull and one caught the recorded character mid-cast. The cadence
/// and the grace period below are what the same pull measured; the wave is assumed over when no shot has
/// landed for a few seconds, since the familiars simply stop rather than announce anything.</para>
/// </summary>
sealed class ScaldingWaves(ModuleBase module) : Components.GenericAOEs(module)
{
    /// <summary>
    /// The line runs both ways from the familiar, which is what "the fire waves were not full length"
    /// meant: it was drawn as fifty yalms thrown forward, so the ground behind the familiar -- real danger
    /// -- was never marked. In the 19:37 pull players were caught 9.3 and 8.1 yalms behind the line's own
    /// origin, and as far as 13.0 ahead. Twenty-five each way covers both the sheet's fifty-yalm range and
    /// the community layout's twenty either side.
    /// </summary>
    private static readonly AOEShapeRect Shape = new(25f, 2f, 25f);

    /// <summary>Measured gap between waves: 142.2, 144.3, 146.5, 148.5, 150.4, 152.5.</summary>
    private const double Cadence = 2.1d;

    /// <summary>How long after a shot the row is still assumed to be firing.</summary>
    private const double Grace = 3.5d;

    private readonly List<AOEInstance> aoes = [];
    private DateTime nextShot, until;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ScaldingWaves or (uint)AID.ScaldingWavesWide)
        {
            this.nextShot = World.FutureTime(Cadence);
            this.until = World.FutureTime(Grace);
        }
    }

    public override void Update()
    {
        this.aoes.Clear();
        if (World.CurrentTime > this.until)
            return;

        foreach (var familiar in Module.Enemies((uint)OID.AnalaFamiliar))
            if (!familiar.IsDeadOrDestroyed)
                this.aoes.Add(new(Shape, familiar.Position, familiar.Rotation, this.nextShot));
    }
}

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
            .ActivateOnEnter<MadeMagic>()
            .ActivateOnEnter<HammerLanding>()
            .ActivateOnEnter<ElementalImpact>()
            .ActivateOnEnter<FlowingLance>()
            .ActivateOnEnter<ScaldingWavesWide>()
            .ActivateOnEnter<ScaldingWaves>();
    }
}

/// <summary>
/// Quaqua, a route boss of the Aloalo Island variant dungeon.
///
/// <para>The arena is derived from the boss rather than written down, because this duty puts the same
/// boss in a different room each run: Quaqua was fought at (-538, 94) and again at (50, -160), and a
/// hardcoded centre meant the dodge judged an arena that was several hundred yalms away -- every cell off
/// the floor, no safe spot anywhere, the module loaded and useless. The boss spawns on the room's axis
/// facing into it, so the centre is a fixed step along its own facing and survives a room that is turned
/// around.</para>
///
/// <para>Quaqua stands on the centre itself, so the step is zero. The radius is still the small guess it
/// always was: a circle that is too small only costs the dodge ground it could have used, one that is too
/// large walks somebody off the edge.</para>
///
/// <para>Unclassified, and each one single-target by the sheet, so drawn as nothing rather than as a
/// guess: Arcane Armaments (the 7.7s wind-up and its 4.7s follow-up), Howl, and the drakes' Cloud to
/// Ground visual. The wind-up is almost certainly the telegraph for the 40-yalm lines that follow it,
/// which is worth confirming against a full recording.</para>
/// </summary>
[ModuleInfo(CFCID = 961u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva, from a recording")]
public sealed class V3Quaqua(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, primary.Position.Quantized(), new ArenaBoundsCircle(21f));
