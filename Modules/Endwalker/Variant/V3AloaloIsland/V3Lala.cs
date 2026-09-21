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
    Kapokapo = 0x403B,          // the flower the boss plants: one Rolling Spout each
    Rodiaki = 0x403C,           // the route add
    ArrowBright = 0x1EB941,     // marks a line of squares that fires first
    ArrowDim = 0x1EB942,        // marks a line that waits for the bright ones to finish
    Matsya = 0x3FE7,            // the NPC standing in, not part of the fight
}

public enum AID : uint
{
    AutoAttack = 872,
    ArcaneBlightFront = 34927,      // Lala->self: the wind-up, always facing 180 whichever way it cleaves
    ArcaneBlightLeft = 34928,       // a third wind-up, from the 21:08 pull
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
    FloralFigure = 34944,           // Lala->self: plants the flowers
    RollingSpout = 34945,           // Kapokapo->self, 4.9s cast: the flower's donut
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

    ForwardMarch = 3715,        // the four that land twelve seconds ahead of the walk, one per player
    AboutFace = 3716,
    LeftFace = 3717,
    RightFace = 3718,
    ForcedMarch = 3719,         // the walk itself; its extra confirms the direction at the last moment
}

public enum IconID : uint
{
    RotateCW = 493,             // subtracts a quarter turn from the arrow you were given
    RotateCCW = 494,            // adds one
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
/// The flowers' donut: stand on the flower, not near it.
///
/// <para>Floral Figure plants a Kapokapo at a fixed spot and each one casts this for 4.9 seconds. The
/// two that fired in the 21:08 pull spared everyone at 0.5, 1.6 and 3.5 yalms, caught the one character
/// at 10.4 and 12.2, and spared everyone again from 20.2 out -- a ring, with the flower itself safe. That
/// is the whole of the direct evidence, so the hole is bracketed between 3.5 and 10.4 and the outer edge
/// between 12.2 and 20.2.</para>
///
/// <para>The hole is four, which is what the community layout for this duty draws as the safe circle and
/// which sits at the near end of the bracket -- the end to be wrong at, because a hole drawn larger than
/// it is puts somebody in the ring believing they are clear.</para>
///
/// <para>The outer is fifteen, and it is the second wave that argues for it rather than the first. The
/// boss planted four flowers at 483.5s, at (143, -854), (127, -862), (143, -878) and (127, -886), and the
/// pull ended before they cast. The nearest two holes are 17.9 yalms apart, so any ring reaching that far
/// would swallow its neighbours' holes and the wave would have no answer at all -- which means the real
/// outer edge is comfortably under it, and fifteen is the round number between there and the 12.2 that
/// was measured. A pull that lives through a four-flower wave settles it properly.</para>
/// </summary>
sealed class RollingSpout(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RollingSpout, new AOEShapeDonut(4f, 15f));

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

    /// <summary>How long a square may outlive its predicted time before it is dropped unfired.</summary>
    private const double Lingers = 3d;

    /// <summary>
    /// How long the burned ground keeps bleeding after the last square of a set goes off.
    ///
    /// <para>Measured from who was bleeding and when: the 21:08 set finished at 464.6s and players were
    /// still being given the status at 474.0, which is 9.4 seconds later and covers the forced march that
    /// walked through it. The earlier set, which no march followed, stopped at 5.1. Ten seconds covers
    /// the long case; the lines are reported to go out when the march ends, and the march is what the set
    /// before it exists to set up.</para>
    /// </summary>
    private const double Burns = 10d;

    /// <summary>What the radar and the dodge see: the squares still to come, plus the ground already
    /// burned and still bleeding.</summary>
    private readonly List<AOEInstance> active = [];

    /// <summary>Squares that have not gone off yet, each with the moment it will.</summary>
    private readonly List<AOEInstance> schedule = [];

    /// <summary>Squares that have gone off and are still a bleed line underfoot.</summary>
    private readonly List<WPos> burned = [];

    private DateTime burnedUntil;
    private readonly List<(bool Bright, WPos Origin, Angle Dir)> arrows = [];

    /// <summary>When the opening square lands. Stamped at the cast, never recomputed per frame.</summary>
    private DateTime first;

    private DateTime spawned;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.active);

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

    /// <summary>
    /// A square comes off the radar when its own pulse fires, not when the schedule says it should have.
    ///
    /// <para>Clearing on the predicted time was clearing too early in game. The schedule is accurate --
    /// the cadence drifts at most 0.15s over ten steps -- but it is still a prediction, and a square that
    /// vanishes while the explosion is on screen reads as the danger being over. The game announces every
    /// square as it goes off, so that is what removes it.</para>
    /// </summary>
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is not ((uint)AID.BrightPulseFirst or (uint)AID.BrightPulseRest))
            return;

        var at = spell.SourcePos == default ? caster.Position : new WPos(spell.SourcePos.X, spell.SourcePos.Z);
        var i = this.schedule.FindIndex(a => (a.Origin - at).LengthSq() <= 1f);
        if (i >= 0)
            this.schedule.RemoveAt(i);

        if (!this.burned.Exists(b => (b - at).LengthSq() <= 1f))
            this.burned.Add(at);

        // every square pushes the whole set's expiry out, so the lines go together rather than one lane
        // at a time -- which is what the last set before the march does, covering everything but one row
        this.burnedUntil = World.FutureTime(Burns);
    }

    /// <summary>
    /// The march holds the lines open.
    ///
    /// <para>They are reported to go out when the walk ends, and the set before the march is the one that
    /// covers the floor bar a single row -- so the ten seconds is a floor, not the rule, and a walk that
    /// runs past it keeps the ground burning until it finishes. Both sets are treated the same way; only
    /// the second is usually followed by a march.</para>
    /// </summary>
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ForcedMarch && this.burned.Count != 0 && status.ExpireAt > this.burnedUntil)
            this.burnedUntil = status.ExpireAt;
    }

    public override void Update()
    {
        var now = World.CurrentTime;

        if (this.burned.Count != 0 && now > this.burnedUntil)
            this.burned.Clear();

        // the backstop, for a square whose pulse never arrives: generous, because the event is the real
        // clearer and this only exists so a missed one cannot sit on the floor forever
        if (this.schedule.Count != 0)
            this.schedule.RemoveAll(a => a.Activation < now.AddSeconds(-Lingers));

        this.active.Clear();
        this.active.AddRange(this.schedule);
        for (var i = 0; i < this.burned.Count; ++i)
            this.active.Add(new(Cell, this.burned[i], default, now));   // burning now, not later
    }

    private void Rebuild()
    {
        this.schedule.Clear();

        var lead = 0;
        foreach (var arrow in this.arrows)
            if (arrow.Bright)
                lead = Math.Max(lead, Line(arrow).Count);

        foreach (var arrow in this.arrows)
        {
            var start = arrow.Bright ? 0 : lead;
            var cells = Line(arrow);
            for (var i = 0; i < cells.Count; ++i)
                this.schedule.Add(new(Cell, cells[i], default, this.first.AddSeconds((start + i) * Cadence)));
        }

        this.schedule.Sort(static (a, b) => a.Activation.CompareTo(b.Activation));
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

/// <summary>
/// Forced March: the walk you do not steer, aimed by the way you were facing when it started.
///
/// <para>Four arrow debuffs land twelve seconds ahead, one per player -- Forward March, About Face, Left
/// Face, Right Face at 3715 to 3718 -- and two seconds after them each player gets a rotate icon, 493 or
/// 494, which turns that arrow a quarter. The sum is where you walk, measured from the way you face when
/// the walk begins. Then 3719 arrives, the game snaps you onto that heading and walks you.</para>
///
/// <para>The rotate icon is the whole puzzle and it is easy to miss, because without it the arrows look
/// random: the same debuff produced opposite turns in two pulls, on hands-off characters. With it, all
/// eight player-walks across the two recordings fit and nothing is left over. 3715 is no turn, 3716 is
/// half, 3717 a quarter one way and 3718 a quarter the other; 493 subtracts a quarter and 494 adds one.
/// It is the same trick this boss plays on Targeted Light, where a rotation is layered over a direction
/// handed out earlier.</para>
///
/// <para>So it is solvable, and the way to solve it is to choose the facing rather than to predict the
/// walk: the heading is your facing plus a total this knows ten seconds in advance, so pointing you the
/// right way decides where you end up. That is what this does -- it looks for a heading whose twelve
/// yalms neither leave the floor nor cross a burned row, subtracts the turn to get the facing that
/// produces it, and forbids every other facing so the same machinery that turns you out of a gaze turns
/// you into this. It needs Face on, and it does nothing once the walk has started, because by then the
/// game owns both your feet and your facing.</para>
/// </summary>
sealed class ForcedMarch : Components.GenericForcedMarch
{
    /// <summary>Seconds of walking, from the recorded characters own traces.</summary>
    private const float Walk = 3.1f;

    /// <summary>Yalms covered, rounded up from the longest measured (11.6).</summary>
    private const float Reach = 12f;

    /// <summary>Arrow to walk: measured 11.91s and 11.96s across the two pulls.</summary>
    private const double Lead = 11.9d;

    private readonly Dictionary<ulong, Angle> arrows = [];
    private readonly Dictionary<ulong, Angle> turns = [];
    private DateTime walks;

    public ForcedMarch(ModuleBase module)
        : base(module, stopAtWall: true) => this.MovementSpeed = 3.9f;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (Arrow(status.ID) is { } arrow)
        {
            this.arrows[actor.InstanceID] = arrow;
            this.turns.Remove(actor.InstanceID);   // the rotate icon has not landed yet
            this.walks = World.FutureTime(Lead);
        }
        else if (status.ID == (uint)SID.ForcedMarch)
        {
            this.turns.Remove(actor.InstanceID);
            this.arrows.Remove(actor.InstanceID);
            this.ActivateForcedMovement(actor, status.ExpireAt);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ForcedMarch)
            this.DeactivateForcedMovement(actor);
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        var rotate = iconID switch
        {
            (uint)IconID.RotateCW => -90f.Degrees(),
            (uint)IconID.RotateCCW => 90f.Degrees(),
            _ => default(Angle?),
        };

        if (rotate is { } turn && this.arrows.TryGetValue(actor.InstanceID, out var arrow))
            this.turns[actor.InstanceID] = arrow + turn;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!this.turns.TryGetValue(actor.InstanceID, out var turn) || World.CurrentTime > this.walks)
            return;

        if (this.Facing(actor, turn) is not { } facing)
            return;

        // leave only the quarter around the facing that aims the walk somewhere survivable, the same way
        // an inverted gaze leaves only the quarter that faces it
        hints.ForbiddenDirections.Add((facing + 180f.Degrees(), 135f.Degrees(), this.walks));
    }

    /// <summary>The facing that sends the walk somewhere worth ending up, or null if nothing does.</summary>
    private Angle? Facing(Actor actor, Angle turn)
    {
        var array = Module.FindComponent<ArcaneArray>();
        Angle? best = null;
        var bestSwing = float.MaxValue;

        for (var step = 0; step < 36; ++step)
        {
            var heading = (step * 10f).Degrees();
            if (!this.Survivable(actor, heading, array))
                continue;

            // among the headings that work, the one asking for the smallest turn, so the dodge is not
            // spun across the arena for a tie
            var swing = MathF.Abs((heading - turn - actor.Rotation).Normalized().Rad);
            if (swing < bestSwing)
            {
                bestSwing = swing;
                best = heading - turn;
            }
        }

        return best;
    }

    /// <summary>Does the whole walk stay on the floor and out of the burned rows?</summary>
    private bool Survivable(Actor actor, Angle heading, ArcaneArray? array)
    {
        var dir = heading.ToDirection();
        for (var d = 1f; d <= Reach; d += 1f)
        {
            var at = actor.Position + (d * dir);
            if (!Module.InBounds(at))
                return false;

            if (array == null)
                continue;

            foreach (ref readonly var aoe in array.ActiveAOEs(0, actor))
                if (aoe.Check(at))
                    return false;
        }

        return true;
    }

    /// <summary>The arrow you are given, before the rotate icon turns it.</summary>
    private static Angle? Arrow(uint status) => status switch
    {
        (uint)SID.ForwardMarch => default(Angle),
        (uint)SID.AboutFace => 180f.Degrees(),
        (uint)SID.LeftFace => 90f.Degrees(),
        (uint)SID.RightFace => -90f.Degrees(),
        _ => null,
    };
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
            .ActivateOnEnter<RollingSpout>()
            .ActivateOnEnter<ForcedMarch>()
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
/// <para>Left uncovered, and each of them a wind-up rather than damage: Teleport, the three Arcane Blight
/// visuals (the helper's cast carries the real angle and is drawn), both Arcane Plot visuals (the arrows
/// they create are drawn), Analysis, Calculated Trajectory, and the two summons -- Floral Figure, whose
/// flowers cast the Rolling Spout that is drawn, and Faunal Figure, whose golems are the real gap. Their
/// Aero is a 50-by-8 line in the criterion version and in neither recording did a golem live long enough
/// to cast, so the variant's id for it is still unknown.</para>
///
/// <para>Which adds turn up depends on the route: the 20:29 pull had Rodiaki and its raidwide, the 21:08
/// pull had the flowers instead. Both are covered; a route that sends the golems is the one still
/// missing.</para>
/// </summary>
[ModuleInfo(CFCID = 961u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva, from a recording")]
public sealed class V3Lala(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, (primary.Position + (5.79f * primary.Rotation.ToDirection())).Quantized(), new ArenaBoundsSquare(20f));
