// Medusa Piece, Second Master Board (piece 7). Not in BossmodReborn yet, so written from Korha's 2026-11-01 19:55 pull
// (minerva-20261101-195542-Medusa Piece.log) and the Action sheet. Every shape below was held against who that pull hit
// and who it missed (cone_fit.py): the Petrifying cones reach 22.5 degrees each side plus the target's hitbox, Shockwave
// hit Korha 7-9 degrees off its line and missed her at 35, the Ringing Blade donut hit her 7 yalms out.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondMasterBoard.MedusaPiece;

public enum OID : uint
{
    MedusaPiece = 0x4CF9, // R1.800
    Helper = 0x233C,
    LamiaPiece = 0x4CFA, // R1.400, summoned in ones and twos
    CyclopsPiece = 0x4CFB, // R2.800, x4 at the corners (108/132, +-12), no cast
}

public enum AID : uint
{
    AutoAttack = 50935, // MedusaPiece->player/pet, no cast, single-target
    Summon = 49283, // MedusaPiece->self, 3.0s cast, single-target (Lamias appear 0.9s after)

    PetrifyingRegardVisual = 49277, // MedusaPiece->self, 3.0s cast, single-target
    PetrifyingRegard = 49278, // Helper->self, 3.0s cast, range 60 45-degree cone, aimed at the tethered player
    PetrifyingPassionVisual = 49279, // MedusaPiece->self, 3.0s cast, single-target
    PetrifyingPassionVisualRepeat = 49280, // MedusaPiece->self, no cast, single-target
    PetrifyingPassion = 49281, // Helper->self, 3.0s cast, range 60 45-degree cone, aimed at the tethered player
    PetrifyingPassionRepeat = 49282, // Helper->self, no cast, range 60 45-degree cone, x4, 30 degrees on each time
    Impassion = 49298, // MedusaPiece->self, 8.0s cast, single-target (buffs herself)

    RipplingEvisceration = 49288, // MedusaPiece->self, 6.5s cast, single-target: circle, then donut
    CirclingBladeFirst = 49289, // Helper->self, 7.0s cast, range 6 circle
    RingingBladeVisual = 49290, // MedusaPiece->self, no cast, single-target
    RingingBladeSecond = 49291, // Helper->self, 9.0s cast, range 5-60 donut
    ConstrictingEvisceration = 49292, // MedusaPiece->self, 6.5s cast, single-target: donut, then circle
    RingingBladeFirst = 49293, // Helper->self, 7.0s cast, range 5-60 donut
    CirclingBladeVisual = 49294, // MedusaPiece->self, no cast, single-target
    CirclingBladeSecond = 49295, // Helper->self, 9.0s cast, range 6 circle
    Shockwave = 49296, // Helper->self, 7.0s cast, range 60 30-degree cone, x4 then x4 two seconds later

    LamiaFire = 48622, // LamiaPiece->player, no cast, single-target
    CircleOfFlames = 49284, // LamiaPiece->location, 4.0s cast, range 5 circle
    LamiaJump = 49285, // LamiaPiece->location, no cast: leaps onto the player, then casts Terrorizing Miasma there
    TerrorizingMiasma = 49286, // LamiaPiece->self, 4.0s cast, range 6 circle
    Raise = 49287, // LamiaPiece->LamiaPiece (dead), 10.0s cast, single-target

    CyclopsAutoAttack = 50937, // CyclopsPiece->player/pet, no cast, single-target
    ThousandTonzeSwing = 49297, // CyclopsPiece->self, 10.0s cast, range 20 circle
    Glower = 49370, // CyclopsPiece->self, 4.0s cast, range 40 width 3 rect
}

public enum SID : uint
{
    Petrification = 437, // Helper->CyclopsPiece/player: the Cyclops drops its cast and soon shatters
    DamageUp = 2550, // Helper->LamiaPiece: what a Petrifying cone does to a Lamia instead, stacking
    VulnerabilityUp = 1789, // Helper->player, from Shockwave
}

public enum IconID : uint
{
    PetrifyingRegard = 23, // player: Medusa tethers and turns Regard on them
    PetrifyingPassion = 244, // player: the same for Passion
    PassionClockwise = 235, // MedusaPiece: the repeats step -30 degrees (127-134s)
    PassionAnticlockwise = 236, // MedusaPiece: +30 degrees (180-187s)
}

sealed class PetrifyingRegard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PetrifyingRegard, new AOEShapeCone(60f, 22.5f.Degrees()));
sealed class Shockwave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shockwave, new AOEShapeCone(60f, 15f.Degrees()));
// Ringing Blade's omen (gl_sircle_6005at1) is the one BossmodReborn draws as 5-60 for Wind Sickle, Brutal Crown and Winding Wildwinds.
sealed class RingingBlade(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RingingBladeFirst, (uint)AID.RingingBladeSecond], new AOEShapeDonut(5f, 60f));
sealed class CirclingBlade(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.CirclingBladeFirst, (uint)AID.CirclingBladeSecond], 6f);
sealed class CircleOfFlames(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CircleOfFlames, 5f);
sealed class ThousandTonzeSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThousandTonzeSwing, 20f);
sealed class Glower(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Glower, new AOEShapeRect(40f, 1.5f));

/// <summary>
/// Petrifying Passion: a 45-degree cone at the tethered player with a cast bar, then four more with none, each 30
/// degrees on round from the last. The icon on Medusa says which way. Korha died to the repeats twice in one pull
/// (132.27s and 181.76s): with nothing drawing them, the dodge stepped out of the first cone the way the sweep went,
/// and the walk back for uptime did the rest. All five are drawn from the moment the cast starts, so the way out is
/// against the sweep or behind her.
/// </summary>
sealed class PetrifyingPassion(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCone cone = new(60f, 22.5f.Degrees());
    private static readonly Angle stepAngle = 30f.Degrees();
    private const int Repeats = 4;
    // Measured 1.68-1.87s between cones, 1.77 on average. Predicting a little early only means leaving a little early.
    private const double StepSeconds = 1.7d;

    private readonly List<AOEInstance> aoes = [];
    private int direction;  // -1 clockwise, +1 anticlockwise, 0 not shown: draw both ways until the first repeat lands
    private Angle start;
    private int landed;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (actor.OID != (uint)OID.MedusaPiece)
            return;
        if (iconID == (uint)IconID.PassionClockwise)
            direction = -1;
        else if (iconID == (uint)IconID.PassionAnticlockwise)
            direction = 1;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID != (uint)AID.PetrifyingPassion)
            return;
        aoes.Clear();
        landed = 0;
        start = spell.Rotation;
        var first = Module.CastFinishAt(spell);
        aoes.Add(new(cone, caster.Position, start, first, actorID: caster.InstanceID));
        for (var k = 1; k <= Repeats; ++k)
        {
            var at = first.AddSeconds(k * StepSeconds);
            if (direction >= 0)
                aoes.Add(new(cone, caster.Position, start + k * stepAngle, at, actorID: caster.InstanceID));
            if (direction <= 0)
                aoes.Add(new(cone, caster.Position, start - k * stepAngle, at, actorID: caster.InstanceID));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is not ((uint)AID.PetrifyingPassion or (uint)AID.PetrifyingPassionRepeat) || aoes.Count == 0)
            return;
        ++landed;
        if (direction == 0 && spell.Action.ID == (uint)AID.PetrifyingPassionRepeat)
        {
            // the first repeat shows the way: keep only that side
            direction = (spell.Rotation - start).Normalized().Rad > 0f ? 1 : -1;
            var keep = start;
            aoes.RemoveAll(a => !a.Rotation.AlmostEqual(keep, 0.01f) && (a.Rotation - keep).Normalized().Rad * direction < 0f);
        }
        // take off the cone that just landed, and time the rest from it
        var hit = aoes.FindIndex(a => a.Rotation.AlmostEqual(spell.Rotation, 5f * MathF.PI / 180f));
        if (hit >= 0)
            aoes.RemoveAt(hit);
        for (var i = 0; i < aoes.Count; ++i)
        {
            var a = aoes[i];
            var steps = MathF.Round(MathF.Abs((a.Rotation - spell.Rotation).Normalized().Deg) / 30f);
            a.Activation = World.FutureTime(Math.Max(1f, steps) * StepSeconds);
            aoes[i] = a;
        }
        if (landed > Repeats || aoes.Count == 0)
        {
            aoes.Clear();
            direction = 0;
        }
    }

    // a cast cut short, or the boss dying mid-sweep, must not leave cones behind
    public override void Update()
    {
        if (aoes.Count != 0 && aoes.TrueForAll(a => a.Activation < World.FutureTime(-3d)))
        {
            aoes.Clear();
            direction = 0;
        }
    }
}

/// <summary>
/// A Lamia leaps onto the player with no cast bar and starts Terrorizing Miasma (6y) where it lands two seconds later.
/// The leap's landing spot is the circle's centre, so it is drawn from the leap: six seconds of warning instead of four.
/// Leap to resolve, four times in Korha's pull: 6.12, 6.12, 6.04, 5.96s.
/// </summary>
sealed class TerrorizingMiasma(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.TerrorizingMiasma)
{
    private static readonly AOEShapeCircle circle = new(6f);
    private const double LeapToResolve = 5.9d;
    private readonly List<AOEInstance> aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID != (uint)AID.LamiaJump)
            return;
        aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
        aoes.Add(new(circle, spell.TargetPos.ToWPos(), default, World.FutureTime(LeapToResolve), actorID: caster.InstanceID));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID != (uint)AID.TerrorizingMiasma)
            return;
        aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
        aoes.Add(new(circle, caster.Position, default, Module.CastFinishAt(spell), actorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TerrorizingMiasma)
            aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
    }

    public override void OnActorDeath(Actor actor) => aoes.RemoveAll(a => a.ActorID == actor.InstanceID);

    public override void Update() => aoes.RemoveAll(a => a.Activation < World.FutureTime(-3d));
}

/// <summary>
/// Medusa tethers a player and turns Petrifying Regard or Passion on them when the cast starts. A Cyclops in the cone is
/// petrified: its cast stops, and three of the four petrified in Korha's pull shattered within 3.3s. Four 1000-tonze
/// Swings (20y from the corners) leave no floor clear, so a petrified Cyclops is the way out of that; at 92s it was luck
/// (Korha stood 1.4 degrees off one). While marked, the player is asked to stand in line with a Cyclops, one casting the
/// Swing first. Advice only: the dodge drops it when danger says otherwise. The ask ends when the cast starts, and the
/// cone that then appears is dodged like any other.
///
/// <para>Against the Swing the line alone is not enough. The only ground the Swings then leave is beside the petrified
/// Cyclops (over 20y from the other three), and the Regard lands 2.3s before them: the closed-loop replay lined up from
/// beside Medusa, 20 yalms off, and was caught on the way. So for a Swing the player waits on the line within
/// <see cref="SwingWait"/> of the Cyclops it aims at, the nearest one casting.</para>
/// </summary>
sealed class GazeAim(ModuleBase module) : ModuleComponent(module)
{
    // The tether goes 0.75-1.8s before the cast starts (six casts); the mark to the cast was 5.8-8.7s.
    private const double MarkLifetime = 12d;
    // Well inside the cone's 22.5: Medusa walks after her target during the mark, and the line moves with her.
    private static readonly Angle aimHalfWidth = 12f.Degrees();
    private const float SwingWait = 5f;

    private ulong marked;
    private DateTime expires;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID is (uint)IconID.PetrifyingRegard or (uint)IconID.PetrifyingPassion)
        {
            marked = actor.InstanceID;
            expires = World.FutureTime(MarkLifetime);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.PetrifyingRegard or (uint)AID.PetrifyingPassion)
            marked = 0;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (actor.InstanceID != marked || World.CurrentTime > expires)
            return;
        var medusa = Module.PrimaryActor;
        var mine = Angle.FromDirection(actor.Position - medusa.Position);
        Actor? best = null;
        var bestSwing = false;
        var bestCost = float.MaxValue;
        foreach (var cyclops in Module.Enemies((uint)OID.CyclopsPiece))
        {
            if (cyclops.IsDeadOrDestroyed || cyclops.FindStatus((uint)SID.Petrification) != null)
                continue;
            var swing = cyclops.CastInfo?.Action.ID == (uint)AID.ThousandTonzeSwing;
            // against a Swing the walk is to the Cyclops itself; otherwise only the turn round Medusa matters
            var cost = swing ? (cyclops.Position - actor.Position).Length()
                : MathF.Abs(mine.DistanceToAngle(Angle.FromDirection(cyclops.Position - medusa.Position)).Rad);
            if (best == null || swing && !bestSwing || swing == bestSwing && cost < bestCost)
                (best, bestSwing, bestCost) = (cyclops, swing, cost);
        }
        if (best == null)
            return;
        ShapeDistance outsideLine = new SDInvertedCone(medusa.Position, 60f, Angle.FromDirection(best.Position - medusa.Position), aimHalfWidth);
        hints.AddForbiddenZone(bestSwing ? new SDUnion([outsideLine, new SDInvertedCircle(best.Position, SwingWait)]) : outsideLine, DateTime.MaxValue);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (actor.InstanceID == marked && World.CurrentTime <= expires && Module.Enemies((uint)OID.CyclopsPiece).Any(c => !c.IsDeadOrDestroyed))
            hints.Add("Line Medusa's gaze up with a Cyclops");
    }
}

// A Lamia raises its dead partner; the second pair raises at once. Interruptible (Soul Crush) per Game8's guide 816366
// (Daedalus research, 2026-11-02). The recordings do not carry the game's interruptible flag yet.
sealed class Raise(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.Raise, showNameInHint: true);

sealed class MedusaPieceStates : StateMachineBuilder
{
    public MedusaPieceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PetrifyingRegard>()
            .ActivateOnEnter<PetrifyingPassion>()
            .ActivateOnEnter<GazeAim>()
            .ActivateOnEnter<Shockwave>()
            .ActivateOnEnter<RingingBlade>()
            .ActivateOnEnter<CirclingBlade>()
            .ActivateOnEnter<CircleOfFlames>()
            .ActivateOnEnter<TerrorizingMiasma>()
            .ActivateOnEnter<Raise>()
            .ActivateOnEnter<ThousandTonzeSwing>()
            .ActivateOnEnter<Glower>();
    }
}

[ModuleInfo(CFCID = 1092u, NameID = 14660u, PrimaryActorOID = (uint)OID.MedusaPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (from recordings)")]
public sealed class MedusaPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, 0f), new ArenaBoundsRect(20f, 20f))
{
    // Lamias before Medusa, and both Lamias alike: the rotation plugin then keeps their HP even so they die together and
    // neither is left to Raise the other (Daedalus's CombatRetargetPolicy.BalancePick). That only acts once a Lamia is its
    // target, and with no kill order here it stayed on Medusa and never balanced (the roommate's pull, 2026-11-03). Ties
    // go to the target already held, so the balance's switches stand. The Cyclopes are left unranked: the gaze shatters them.
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var e in hints.PotentialTargets)
            e.Priority = e.Actor.OID switch
            {
                // a dead one waiting to be raised, or one not summoned yet, is still in the list: not a target
                (uint)OID.LamiaPiece when e.Actor.IsTargetable && !e.Actor.IsDead && e.Actor.HPMP.CurHP > 0 => 2,
                (uint)OID.MedusaPiece => 1,
                _ => e.Priority,
            };
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.LamiaPiece));
        Arena.Actors(Enemies((uint)OID.CyclopsPiece));
    }

    private readonly string[] _prePullHints = [
        "Petrifying Passion sweeps 30 degrees at a time after its first cone: leave against the sweep",
        "Aim Medusa's gaze through a Cyclops (it shatters); Lamias it touches get stronger",
    ];

    public override string[] PrePullHints => _prePullHints;
}
