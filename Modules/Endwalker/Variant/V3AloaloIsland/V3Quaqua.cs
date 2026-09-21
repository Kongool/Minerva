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
    HammerLanding = 35725,          // Quaqua->location, 7.7s cast: the first hammer, then it leaps
    HammerLanding2 = 35726,         // the two follow-ups, no cast bar; each also shoves 20y on landing
    MadeMagic = 35732,              // Quaqua->location, 4.7s cast, range 50 circle: the raidwide
    ArcaneArmaments3 = 35743,       // Quaqua->self, single-target
    ElementalImpact = 35744,        // range 14 circle
    FlowingLance = 35745,           // range 24 width 12 cross, 7.7s cast: the first of seven, steps -15 deg
    FlowingLance2 = 36049,          // the same cross from the other helper, stepping +15 deg
    FlowingLance3 = 35746,          // the six repeats from either helper, 0.7s cast each
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

/// <summary>The damage the hammer does on the way down, which is not the dangerous half: see
/// <see cref="HammerLanding"/> for the shove that follows it.</summary>
sealed class HammerBlow(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.HammerLanding, (uint)AID.HammerLanding2]);

/// <summary>
/// Three hammer landings, each a twenty-yalm shove away from the spot the hammer comes down on.
///
/// <para>This was named a raidwide, and a raidwide is not what hurt anyone. Quaqua casts for seven and a
/// half seconds, the hammer resolves where it stands, and then it leaps -- and the push comes from where
/// it lands, not from where it cast. At 100.8s of the 19:59 pull two characters standing in different
/// places were pushed along bearings -105 and -112; those two rays meet at (63.0, -152.5), which is
/// exactly where Quaqua arrived at 101.46s. The same fit at 279.0s lands on (37.0, -152.5), again the
/// arrival. Away-from-origin to within a degree, twice, and every target carried a knockback effect.</para>
///
/// <para>Where it lands is knowable ahead of time. The leap runs along the boss's own facing, which it
/// turns to four tenths of a second into the cast and then holds for the remaining seven and a third, and
/// the three landing spots sit on a ring fifteen yalms out from the middle of the room -- so the
/// destination is simply where that facing crosses the ring: fifteen yalms from the middle, twenty-six
/// from one spot to another. Both measured leaps matched that to a tenth of a yalm. The two follow-ups
/// carry no cast bar at all and the boss actor's own rotation still reads the previous leap for another
/// third of a second, so those are read from the cast event, which does carry the new facing, and give
/// nine tenths of a second of warning each.</para>
///
/// <para>Twenty yalms, from the two shoves that were not cut short: 18.7 and 18.9 measured with the
/// position sampling ending mid-slide. The wall is solid -- two shoves two minutes apart, travelling in
/// opposite directions, both stopped dead at 24.0 yalms from the middle -- so this cannot push anyone off
/// the floor, and it is drawn rather than forbidden.</para>
/// </summary>
sealed class HammerLanding(ModuleBase module) : Components.GenericKnockback(module, stopAtWall: true)
{
    private const float Distance = 20f;

    /// <summary>The ring the three landing spots sit on, measured out from the middle of the room.</summary>
    private const float Ring = 15f;

    /// <summary>Cast event to the shove: the leap itself takes 0.63s and the push follows a quarter of a
    /// second after the boss arrives.</summary>
    private const double Shove = 0.9d;

    private readonly List<Knockback> shoves = [];
    private WPos origin;
    private DateTime lands;

    /// <summary>While the cast runs the boss can still turn, so its facing is re-read every frame rather
    /// than stamped once. The time is stamped at the trigger, which is what makes it grow urgent.</summary>
    private bool leaping;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.shoves);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == (uint)AID.HammerLanding)
        {
            this.lands = Module.CastFinishAt(cast, Shove);
            this.leaping = true;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is not ((uint)AID.HammerLanding or (uint)AID.HammerLanding2))
            return;

        var from = spell.SourcePos == default ? caster.Position : new WPos(spell.SourcePos.X, spell.SourcePos.Z);
        this.origin = Landing(from, spell.Rotation);
        this.lands = World.FutureTime(Shove);
        this.leaping = false;
        ++this.NumCasts;
    }

    public override void Update()
    {
        this.shoves.Clear();
        if (this.lands == default)
            return;

        if (World.CurrentTime > this.lands.AddSeconds(1d))
        {
            this.lands = default;
            return;
        }

        var boss = Module.PrimaryActor;
        this.shoves.Add(new(this.leaping ? Landing(boss.Position, boss.Rotation) : this.origin, Distance, this.lands));
    }

    /// <summary>Where the leap ends: along the given facing, out to the ring the landing spots sit on. From
    /// the middle that is fifteen yalms; from one landing spot to another it is the chord, 25.98.</summary>
    private WPos Landing(WPos from, Angle rot)
    {
        var dir = rot.ToDirection();
        var rel = from - Module.Center;
        var along = rel.Dot(dir);
        var disc = (along * along) - rel.LengthSq() + (Ring * Ring);
        return disc <= 0f ? from + (Ring * dir) : from + ((MathF.Sqrt(disc) - along) * dir);
    }
}

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
/// <summary>
/// Two counter-rotating crosses, seven positions each, fifteen degrees apart.
///
/// <para>The module drew the first cross of each set and nothing else, because the six that follow carry a
/// different action id -- 35746 -- which was not in the enum. That is the one that killed the recorded
/// character: at 163.1s of the 19:59 pull it caught them, at 165.3s both crosses caught them, and 165.97s
/// is where their HP reached zero with an empty arena drawn. Ten hits across the pull, every one of them
/// invisible.</para>
///
/// <para>The shape is the same cross the first cast uses, confirmed against the hits rather than assumed:
/// with the character standing still through a whole rotation the caught positions sit 2.2, 2.8 and 5.8
/// yalms off an arm and the spared ones 6.6, 8.6, 9.9 and further, which is a half-width of six; one spared
/// at 25.1 yalms along an arm against hits out to 19.2 puts the length at the sheet's twenty-four. The
/// samples that look like a survivor standing inside an arm are all after 165.97s and are a corpse.</para>
///
/// <para>Drawn as a rotation rather than one cast at a time, because each repeat telegraphs for only seven
/// tenths of a second -- enough to be told, not enough to be walked out of, and stepping clear of one
/// blindly is how you stand in the next. The sequence is rigid: seven casts 2.1s apart, the 35745 helper
/// stepping -15 degrees and the 36049 helper +15, mirrored so they sweep into each other. All three sets in
/// the recording ran exactly that, including the mirrored set that starts at -135 rather than +135.</para>
/// </summary>
sealed class FlowingLance(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private static readonly AOEShapeCross Shape = new(24f, 6f);

    /// <summary>The opening cast plus its six repeats.</summary>
    private const int Casts = 7;

    /// <summary>Measured between resolves across all three sets: 2.0 to 2.2.</summary>
    private const double Step = 2.1d;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var increment = spell.Action.ID switch
        {
            (uint)AID.FlowingLance => -15f.Degrees(),
            (uint)AID.FlowingLance2 => 15f.Degrees(),
            _ => default
        };

        if (increment != default)
            this.Sequences.Add(new(Shape, caster.Position, spell.Rotation, increment, Module.CastFinishAt(spell), Step, Casts));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.FlowingLance or (uint)AID.FlowingLance2 or (uint)AID.FlowingLance3)
            this.AdvanceSequence(caster.Position, spell.Rotation, World.CurrentTime);
    }
}

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
            .ActivateOnEnter<HammerBlow>()
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
/// <para>Quaqua stands on the centre itself, so the step is zero. The shape is sized to cover every room
/// seen so far rather than the smallest: the first ran 19.7 yalms either side in x and 20.0 in z, the
/// second 24.1 and 15.2, so the rooms differ in proportion and not only in place. A rectangle of 25 by 21
/// holds both.</para>
///
/// <para>The width is measured rather than guessed: Hammer Landing shoves people into the east and west
/// walls, and two shoves two minutes apart, travelling in opposite directions, both stopped dead at 24.0
/// yalms from the middle. Twenty thousand in-combat position samples reach 24.3 and 24.4, so 24 is the
/// wall to within half a yalm. The north-south extent is still the old guess and is the weaker half: the
/// samples run 17.5 south and 22.6 north of the middle, which is either an off-centre room or somebody
/// standing in the doorway, and nothing so far distinguishes the two.</para>
///
/// <para>Erring large here rather than small, which is the opposite of the usual rule, because two things
/// make it survivable and one makes it necessary: the floor probe refuses a dodge target with no floor
/// under it, leaving this arena a bound on the search rather than the last word on the ground; and
/// stepping off in this duty costs a bleed rather than a life. What makes it necessary is the 19:59 pull,
/// where a circle sized to the smaller room left the solver with no reachable safe cell three separate
/// times while real ground stood empty to the east and west.</para>
///
/// <para>Unclassified, and each one single-target by the sheet, so drawn as nothing rather than as a
/// guess: Arcane Armaments (the 7.7s wind-up and its 4.7s follow-up), Howl, and the drakes' Cloud to
/// Ground visual. The wind-up is almost certainly the telegraph for the 40-yalm lines that follow it,
/// which is worth confirming against a full recording.</para>
/// </summary>
[ModuleInfo(CFCID = 961u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva, from a recording")]
public sealed class V3Quaqua(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, primary.Position.Quantized(), new ArenaBoundsRect(24f, 21f));
