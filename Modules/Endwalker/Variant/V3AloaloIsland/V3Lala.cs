// Built from a recording of the Aloalo Island variant dungeon (CFC 961, zone 1176), cross-checked against
// BossmodReborn's criterion module for the same boss (C03AAI/C032Lala) and the community Splatoon layout.
// BossmodReborn has no module for the variant route: its Lala is the criterion one, which is a different
// duty with its own action ids -- but the same fight, so its shapes and its status table both carry over.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Variant.V3AloaloIsland.V3Lala;

public enum OID : uint
{
    Boss = 0x4039,              // Lala
    Helper = 0x233C,            // also named Lala: casts the cleave, the pulses and the light
    Rodiaki = 0x403C,           // the route add
    ArrowBright = 0x1EB941,     // marks a line of squares that fires first
    ArrowDim = 0x1EB942,        // marks a line that waits for the bright ones to finish
    Matsya = 0x3FE7,            // the NPC standing in, not part of the fight
}

public enum AID : uint
{
    AutoAttack = 872,
    ArcaneBlightFront = 34927,      // Lala->self: the wind-up, always facing 180 whichever way it cleaves
    ArcaneBlightBack = 34930,       // the other wind-up seen in the recording
    ArcaneBlightAOE = 34931,        // Helper->self: the cleave itself, and the only one that carries the angle
    Teleport = 34932,               // Lala->location, no cast
    ArcanePlot = 34933,             // Lala->self: creates the arrows
    ArcanePlot2 = 34934,            // the second form, same effect
    BrightPulseFirst = 34936,       // Helper->self, ~11.9s cast: the opening square of every line
    BrightPulseRest = 34937,        // Helper->self, no cast: every square after it
    Analysis = 34939,               // Lala->self: hands out the unseen-side debuffs
    CalculatedTrajectory = 34941,   // Lala->self, no damage in the recording
    StrategicStrike = 34942,        // Lala->player: tankbuster, hit only its target
    InfernoTheorem = 34943,         // Lala->self: raidwide
    FaunalFigure = 34946,           // Lala->self: summons the golems
    FlailSmash = 34947,             // Rodiaki->self: caught everyone out to 47 yalms
    FlailSmash2 = 35436,            // the second form
    TargetedLight = 36060,          // Lala->self: the wind-up
    TargetedLightAOE = 36061,       // Helper->player, one cast each: the weak-side check
}

public enum SID : uint
{
    FrontUnseen = 3726,         // extra 0x297
    BackUnseen = 3727,          // extra 0x298
    RightUnseen = 3728,         // extra 0x299
    LeftUnseen = 3729,          // extra 0x29A
}

/// <summary>
/// The 270-degree cleave, drawn from the helper's cast and not the boss's.
///
/// <para>Lala winds up with its own cast, and that cast reads rotation 180 every single time whichever way
/// the cleave actually goes -- the helper's cast is the one carrying the angle. Three cleaves in the
/// recording, at -90, +90 and 0, and a cone of 135 either side of those separates every hit from every
/// spare: caught at 113, 104, 89 and 79 degrees off the facing, spared at 168, 163, 174, 154, 176, 137,
/// 159 and 180. A quarter of the room is safe and it is the quarter behind.</para>
/// </summary>
sealed class ArcaneBlight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArcaneBlightAOE, new AOEShapeCone(60f, 135f.Degrees()));

/// <summary>Raidwides, both of them. Flail Smash is the add's and reached 47 yalms, which is the whole
/// room and then some.</summary>
sealed class InfernoTheorem(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.InfernoTheorem);

/// <inheritdoc cref="InfernoTheorem"/>
sealed class FlailSmash(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.FlailSmash, (uint)AID.FlailSmash2]);

/// <summary>Hit its target and nobody else, at five yalms, with three other people inside eleven.</summary>
sealed class StrategicStrike(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.StrategicStrike);

/// <summary>
/// Targeted Light: turn your unseen side toward the light. The same mechanic Mustadio runs in Rabanastre,
/// and Minerva already has the component for it.
///
/// <para>Worth stating what it is not, because two obvious readings are both wrong and the recording
/// refutes each of them. It is not a look-away gaze and it is not a look-at gaze: at 102.8s three of the
/// four players were facing the light to within 1.2 degrees and two of those three still failed, while the
/// one player who was 116 degrees off it passed. Facing has to be read against the debuff.</para>
///
/// <para>Analysis hands each player one of four statuses naming the side that cannot see -- in game it is
/// a barrier at your feet with a gap in it, and the gap has to face the boss as the cast finishes -- so
/// the light has to arrive on that side. All eight player-casts in the recording agree, with the pass and fail
/// recorded by the game itself as icons 503 and 504 and confirmed by who took the vulnerability: front
/// unseen passes with the light dead ahead, left unseen fails with it dead ahead, and right unseen passes
/// with it 116 degrees round. The variant uses the criterion's own status ids, extras and all, so the
/// quarter-turn table is BossmodReborn's unchanged.</para>
///
/// <para>The second set turns the weak point a quarter before it resolves, and the game applies that turn
/// by simply swapping the debuff: at 161.6s the four players were given Back, Front, Left and Right along
/// with a turn icon each (493 clockwise, 494 counter-clockwise), and at 171.6s -- 1.3 seconds before the
/// cast finished -- every one of them was re-issued as Left, Left, Front and Front, which is each original
/// turned ninety degrees the way its icon said. Reading the status rather than the icon therefore handles
/// the turn for free, and always shows the answer the game is actually checking. The first set carried no
/// turn icons and no swap.</para>
///
/// <para>The icons are the early tell, eleven seconds ahead of the swap rather than 1.3, so predicting
/// from them would buy warning -- but it would also have to be unwound when the swap lands, and the turn
/// was exactly ninety degrees every time, with none of the criterion's times-three or times-five statuses
/// present to make it anything else.</para>
///
/// <para>The eye is put on the caster, which is the same spot as the boss: the light is cast from
/// (135, -870) and the boss stood at (135.00, -870.02) for all four of these casts, so 'turn the opening
/// toward the boss' and 'toward the caster' are the same instruction here.</para>
/// </summary>
sealed class TargetedLight(ModuleBase module) : Components.CastWeakpoint(
    module,
    (uint)AID.TargetedLightAOE,
    60f,
    (uint)SID.FrontUnseen,
    (uint)SID.BackUnseen,
    (uint)SID.LeftUnseen,
    (uint)SID.RightUnseen);

/// <summary>
/// The squares that march down the arrows, which is the mechanic with no cast bar to see.
///
/// <para>Arrows spawn around the rim pointing across the floor, and eight-yalm squares detonate along each
/// arrow's line one after another, 1.2 seconds apart. Only the very first square of the set has a cast bar
/// -- one action id, 34936 -- and every square after it is 34937 with nothing at all. Drawing only what is
/// cast would show the opening square and then let the rest of the line land unannounced, which is how the
/// recorded party ate two of them.</para>
///
/// <para>So the whole schedule is laid out when the arrows appear. Each arrow's line runs from the arrow
/// across the floor in eight-yalm steps until it leaves the arena, and the bright arrows go first: the dim
/// ones start one full bright line later, not at some fixed offset. Checked against the recording, the
/// second set matched all twenty detonations on cell and time; the first matched all twenty-five cells,
/// with four timing collisions where two lines cross on the same square.</para>
///
/// <para>The clock comes from that one cast bar, which starts a tenth of a second before the arrows and
/// runs 11.9 seconds, so the set is drawn about twelve seconds ahead. The fallback exists for a pull that
/// somehow shows an arrow without the cast.</para>
///
/// <para>The offline validator reports Bright Pulse as an uncovered helper cast, and that is the report
/// being wrong rather than this. It marks an action covered when the number of active AOEs rises at the
/// cast, and a schedule that was laid out twelve seconds earlier raises nothing at the cast itself.
/// Checked directly instead: at the pulse that caught the recorded character at 197.6s the component had
/// that character inside the square it was about to detonate, and at every pulse of the first set -- none
/// of which touched them -- it had them outside.</para>
/// </summary>
sealed class ArcaneArray(ModuleBase module) : Components.GenericAOEs(module)
{
    /// <summary>Eight yalms square, centred on the cell rather than thrown forward from it.</summary>
    private static readonly AOEShapeRect Cell = new(4f, 4f, 4f);

    private const float Step = 8f;

    /// <summary>Squares per line: forty yalms of floor in eight-yalm steps.</summary>
    private const int Cells = 5;
    private const double Cadence = 1.2d;

    /// <summary>Only used if an arrow ever appears without the cast that times it.</summary>
    private const double Fallback = 11.8d;

    private readonly List<AOEInstance> aoes = [];
    private readonly List<(bool Bright, WPos Origin, Angle Dir)> arrows = [];

    /// <summary>When the opening square lands. Stamped at the cast, never recomputed per frame.</summary>
    private DateTime first;

    private DateTime spawned;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == (uint)AID.BrightPulseFirst)
            this.first = Module.CastFinishAt(cast);
    }

    public override void OnActorCreated(Actor actor)
    {
        var bright = actor.OID == (uint)OID.ArrowBright;
        if (!bright && actor.OID != (uint)OID.ArrowDim)
            return;

        // a set spawns in one frame; anything later is the next set and starts clean
        if (World.CurrentTime > this.spawned.AddSeconds(5d))
            this.arrows.Clear();
        this.spawned = World.CurrentTime;

        if (this.first <= World.CurrentTime)
            this.first = World.FutureTime(Fallback);

        this.arrows.Add((bright, actor.Position, actor.Rotation));
        this.Rebuild();
    }

    public override void Update()
    {
        if (this.aoes.Count != 0)
        {
            var stale = World.CurrentTime.AddSeconds(-0.6d);
            this.aoes.RemoveAll(a => a.Activation < stale);
        }
    }

    private void Rebuild()
    {
        this.aoes.Clear();

        var lead = 0;
        foreach (var arrow in this.arrows)
            if (arrow.Bright)
                lead = Math.Max(lead, Line(arrow).Count);

        foreach (var arrow in this.arrows)
        {
            var start = arrow.Bright ? 0 : lead;
            var cells = Line(arrow);
            for (var i = 0; i < cells.Count; ++i)
                this.aoes.Add(new(Cell, cells[i], default, this.first.AddSeconds((start + i) * Cadence)));
        }

        this.aoes.Sort(static (a, b) => a.Activation.CompareTo(b.Activation));
    }

    /// <summary>
    /// Every square from the arrow to the far wall, in the direction the arrow points.
    ///
    /// <para>Five of them, counted rather than tested against the arena: the floor is forty yalms across
    /// and the squares are eight, so a line that starts on one rim reaches the other in five, and all
    /// eleven lines in the recording were exactly that. Walking the line until it leaves the bounds would
    /// be the obvious way to write this and it is the wrong one -- the boss strolls to the middle of the
    /// room over the first nineteen seconds and swings its facing to -81 on the way, so a module built
    /// mid-stroll has a centre that is off by yalms, and the bounds test then quietly drops real squares
    /// off one end of every line.</para>
    /// </summary>
    private static List<WPos> Line((bool Bright, WPos Origin, Angle Dir) arrow)
    {
        List<WPos> cells = [];
        var step = Step * arrow.Dir.ToDirection();
        var p = arrow.Origin;
        for (var i = 0; i < Cells; ++i, p += step)
            cells.Add(p);
        return cells;
    }
}

sealed class V3LalaStates : StateMachineBuilder
{
    public V3LalaStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<ArcaneBlight>()
            .ActivateOnEnter<ArcaneArray>()
            .ActivateOnEnter<TargetedLight>()
            .ActivateOnEnter<StrategicStrike>()
            .ActivateOnEnter<InfernoTheorem>()
            .ActivateOnEnter<FlailSmash>();
    }
}

/// <summary>
/// Lala, a route boss of the Aloalo Island variant dungeon.
///
/// <para>The arena is derived from the boss rather than written down, for the reason the Quaqua module
/// explains at length: this duty puts its bosses in whichever room the route picked, and a hardcoded
/// centre once left the dodge judging a floor several hundred yalms from the fight. Lala spawns 5.79
/// yalms short of the middle on its own facing, which from its spawn at (135, -875.8) facing north lands
/// on (135, -870).</para>
///
/// <para>Checked against the recording: the module builds with its centre on (135.003, -870.002), which
/// is the grid centre to three millimetres. It is right because the module is built when the boss spawns,
/// before it moves -- a module built mid-fight, which is what a plugin reload during a pull produces,
/// would read a boss that has already walked to the middle and turned, and place the floor several yalms
/// out. Nothing in the fight's danger depends on that centre any more, which is deliberate.</para>
///
/// <para>That centre is not a guess from position samples: the arrows spawn on a fixed eight-yalm grid,
/// and in this recording they occupied x of 119, 135 and 151 and z of -854 through -878, which is a grid
/// of -16, -8, 0, +8, +16 about exactly (135, -870). The squares are eight yalms across, so the grid
/// covers 40 by 40 and the floor is the square of half-20 that BossmodReborn also uses for the criterion
/// version of this room.</para>
///
/// <para>Left uncovered, and each of them a wind-up rather than damage: Teleport, the two Arcane Blight
/// visuals (the helper's cast carries the real angle and is drawn), both Arcane Plot visuals (the arrows
/// they create are drawn), Analysis and Calculated Trajectory. Faunal Figure is the real gap: it summons
/// the golems whose Aero is a 50-by-8 line in the criterion version, and in this recording the golems
/// never lived long enough to cast, so the variant's action id for it is still unknown. A pull where they
/// get a cast off would close it.</para>
/// </summary>
[ModuleInfo(CFCID = 961u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva, from a recording")]
public sealed class V3Lala(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, (primary.Position + (5.79f * primary.Rotation.ToDirection())).Quantized(), new ArenaBoundsSquare(20f));
