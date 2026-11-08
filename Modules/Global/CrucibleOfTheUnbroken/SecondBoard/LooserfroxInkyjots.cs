// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondBoard.LooserfroxInkyjots;

public enum OID : uint
{
    LoosefroxInkyjots = 0x4C65,
    ChewchumPopoto = 0x4C66, // R6.105, x1
    Quicksand = 0x1EC026, // R0.500, x6, EventObj type
    GobbieBombBig = 0x4C67, // R1.200, x0 (spawn during fight)
    GobbieBombSmall = 0x4C69, // R0.600, x0 (spawn during fight)
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 50784, // LoosefroxInkyjots/4C66->player, no cast, single-target

    Burrow = 48224, // 4C66->self, no cast, single-target
    SandPillar = 48225, // Helper->location, no cast, range 4 circle
    Teleport = 50451, // Helper->location, no cast, single-target
    SandBreathVisual = 48226, // 4C66->self, 7.8+1.2s cast, single-target
    SandBreath = 48227, // Helper->self, 9.0s cast, range 60 90.000-degree cone
    GobspinHeadlopsVisual = 48228, // LoosefroxInkyjots->self, 5.3+0.7s cast, single-target
    GobspinHeadlops = 48229, // Helper->self, 6.0s cast, range 8 circle
    Kinborrow = 48242, // LoosefroxInkyjots->self, 4.0s cast, single-target
    GobbieboomBarrage1 = 50446, // LoosefroxInkyjots->self, 3.0s cast, single-target
    GobbieboomBarrage2 = 48234, // LoosefroxInkyjots->self, 3.0s cast, single-target
    PitCatch = 50452, // Helper->location, no cast, range 6 circle: each pit's check for a bomb rolled into it (BombPush)
    PitSink = 48238, // Helper at a pit, after a catch
    Explosion = 48235, // 4C69->self, 2.0s cast, range 12 circle
    BombTossVisual = 48518, // LoosefroxInkyjots->location, 3.0+1.0s cast, single-target
    BombToss = 48519, // Helper->location, 4.0s cast, range 6 circle

    GobspinHeadlopsDonutVisual = 48230, // LoosefroxInkyjots->self, 5.3+0.7s cast, single-target
    GobspinHeadlopsDonut = 48231, // Helper->self, 6.0s cast, range 4-40 donut
    ExplosionCross = 48236, // 4C67->self, 2.0s cast, range 40 width 8 cross
    GoblinHammerVisual = 48232, // LoosefroxInkyjots->self, 3.0s cast, single-target
    GoblinHammer = 48233, // Helper->self, 5.0s cast, range 8 circle
    Earthquake = 48239, // ChewchumPopoto->self, 5.0s cast, range 60 circle
    EarthquakeRest = 48240, // ChewchumPopoto->self, no cast, range 60 circle
}

public enum SID : uint
{
    Blind = 5381, // Helper->player, extra=0x0
    PopotoSkin = 5423, // LoosefroxInkyjots->LoosefroxInkyjots, extra=0x0
}

public enum IconID : uint
{
    Exclamation_8s_x = 569, // Helper->self
}

sealed class BurrowSandPillar(ModuleBase module) : Components.CastCounter(module, (uint)AID.Burrow)
{
    // Burrow at current position
    // 1st SandPillar happens near but not exactly at sandpit, 4-5f away
    // Kinsburrow = 4f dist, Regular/Burrow = 5f?
    // can bounce between sandpits before stopping for breath, anywhere from 6-14 SandPillar so far
    internal static readonly WPos[] Sandpits = [
        new(533f, -434f),
        new(538f, -421f),
        new(529.65f, -407f),
        new(510.35f, -407f),
        new(504f, -423f),
        new(516f, -439f)
        ];
    private readonly WPos[] _sandpits = Sandpits;
    private readonly float _radius = 6f;

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        var count = _sandpits.Length;
        for (var i = 0; i < count; i++)
        {
            Arena.ZoneCircle(_sandpits[i], _radius, default);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = _sandpits.Length;
        for (var i = 0; i < count; i++)
        {
            var sd = new SDCircle(_sandpits[i], _radius);
            hints.AddForbiddenZone(sd);
        }
    }
}
sealed class SandBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SandBreath, new AOEShapeCone(60f, 45f.Degrees()));
sealed class GobspinHeadlops(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GobspinHeadlops, 8f);
sealed class BombToss(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BombToss, 6f);
sealed class GobspinHeadlopsDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GobspinHeadlopsDonut, new AOEShapeDonut(4f, 40f));
sealed class GoblinHammer(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GoblinHammer, 8f);
sealed class GobbieBombs(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.GobbieBombBig, (uint)OID.GobbieBombSmall], AIHints.Enemy.PriorityUndesirable);
sealed class Earthquake(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Earthquake);
sealed class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, 12f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell) { }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor.OID == (uint)OID.GobbieBombSmall && animState1 == 1)
        {
            var origin = actor.Position;
            var rotation = actor.Rotation;
            var layer = ResolveArenaProjectionLayer(actor.PosRot.Y);
            var activation = World.CurrentTime.AddSeconds(7d);
            Casters.Add(new(Shape, origin, rotation, activation, actorID: actor.InstanceID, shapeDistance: Shape.Distance(origin, rotation),
                arenaProjectionLayer: layer, restrictToArenaProjectionLayer: RestrictToArenaProjectionLayer));
        }
    }

    // The bombs roll when hit (see BombPush), and blow where they stop. The circle follows its bomb until it goes off.
    public override void Update()
    {
        base.Update();
        var aoes = CollectionsMarshal.AsSpan(Casters);
        for (var i = 0; i < aoes.Length; ++i)
            if (World.Actors.Find(aoes[i].ActorID) is { } bomb)
                aoes[i].Origin = bomb.Position;
    }

    // A bomb a pit catches never explodes: it sinks, and its blast would otherwise stay drawn, then go overdue as live
    // ground round the pit (10-28 01:21: caught at 70.5s, the actor kept until 77.3s).
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == (uint)AID.PitCatch)
            foreach (var t in spell.Targets)
                Casters.RemoveAll(c => c.ActorID == t.ID);
    }
}

// Knock a small bomb into the pit Chewchum surfaces in, and it comes up stunned and vulnerable (TPLemont's Second Board D3
// guide; the JP guides say the same). Measured from Korha's 86 pulls (2026-11-07):
// - Every hit on a small bomb rolls it exactly 4 yalms straight away from the hitter's position, ~0.5s later. About 50
//   pushes are recorded, nearly all of them the familiar's AoEs scattering the bombs at random.
// - A helper at each of the six pits casts PitCatch every ~0.1s while the bombs live. A bomb whose centre comes within
//   ~6.6 yalms of a pit centre is caught and pulled in (the one recorded, at 6.07). In an empty pit it is just lost.
// - The worm's pit is known only when its Sand Breath starts, 13.0s after the bombs spawn (11.7-14.4). The bombs go
//   untargetable at +19.7s (17.8-19.9), so the window is 5.3-8.0s, and Sand Breath's cone covers the side to push from.
// So while the worm's pit is known, the player is steered behind the bomb that needs the fewest pushes, on the line from
// the pit through it, and the rotation is told to hit that bomb only while lined up. Off the line, every small bomb is
// forbidden, so a stray hit cannot waste one. Daedalus reads a small bomb at the head of the priority list as "hit it
// with the single-target combo"; it sends the familiar away itself, before the bombs exist.
sealed class BombPush(ModuleBase module) : ModuleComponent(module)
{
    private const float PushLength = 4f;
    private const float CatchPlanned = 6f;      // planned short of the 6.6 measured
    private const float Standoff = 2.4f;        // player to bomb, centre to centre, when lined up
    private const float SpotRadius = 0.8f;      // the solver's 1y grid always has a cell inside 0.71
    private const float Reach = 3.8f;          // a hit landed from 4.2 (10-28 01:21); kept inside it
    private const float AimToleranceDeg = 20f;
    // How long the worm is ranked first after a catch: a guess until a catch is recorded (TPLemont: no enrage for ~20s).
    private const double KnockUpHold = 20d;

    private readonly List<Actor> bombs = module.Enemies((uint)OID.GobbieBombSmall);
    private Actor? breath;                      // the helper casting Sand Breath from the worm's pit
    private WPos pit;
    private ulong chosen;
    private DateTime caughtAt;
    private bool wormUpSinceCatch;

    public bool KnockedUp => caughtAt != default && World.CurrentTime < caughtAt.AddSeconds(KnockUpHold);

    private bool PushOn => breath != null && caughtAt == default;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID != (uint)AID.SandBreath)
            return;
        breath = caster;
        var p = caster.Position;
        pit = BurrowSandPillar.Sandpits.MinBy(s => (s - p).LengthSq());
        chosen = 0;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (caster == breath && spell.Action.ID == (uint)AID.SandBreath)
            breath = null;
    }

    public override void OnActorCreated(Actor actor)
    {
        // A new set of bombs starts a new try.
        if (actor.OID == (uint)OID.GobbieBombSmall && !KnockedUp)
            caughtAt = default;
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID != (uint)AID.PitCatch || breath == null || caughtAt != default || (caster.Position - pit).LengthSq() > 4f)
            return;
        foreach (var t in spell.Targets)
        {
            if (World.Actors.Find(t.ID) is { OID: (uint)OID.GobbieBombSmall })
            {
                caughtAt = World.CurrentTime;
                wormUpSinceCatch = false;
                return;
            }
        }
    }

    public override void OnActorTargetable(Actor actor)
    {
        if (actor.OID == (uint)OID.ChewchumPopoto && KnockedUp)
            wormUpSinceCatch = true;
    }

    public override void OnActorUntargetable(Actor actor)
    {
        // The worm going back under ends the window early.
        if (actor.OID == (uint)OID.ChewchumPopoto && wormUpSinceCatch && KnockedUp)
            caughtAt = World.CurrentTime.AddSeconds(-KnockUpHold);
    }

    private static int PushesNeeded(WPos bomb, WPos pit) => Math.Max(1, (int)MathF.Ceiling(((pit - bomb).Length() - CatchPlanned) / PushLength));

    private static WPos SpotBehind(WPos bomb, WPos pit) => bomb + Standoff * (bomb - pit).Normalized();

    private Actor? Choose(Actor player)
    {
        Actor? best = null;
        var bestPushes = int.MaxValue;
        var bestWalk = float.MaxValue;
        Actor? current = null;
        foreach (var b in bombs)
        {
            if (!b.IsTargetable || b.IsDeadOrDestroyed)
                continue;
            var pushes = PushesNeeded(b.Position, pit);
            var walk = (SpotBehind(b.Position, pit) - player.Position).LengthSq();
            if (b.InstanceID == chosen)
                current = b;
            if (pushes < bestPushes || pushes == bestPushes && walk < bestWalk)
            {
                best = b;
                bestPushes = pushes;
                bestWalk = walk;
            }
        }
        // Stay with the bomb already being pushed unless another needs fewer pushes.
        if (current != null && PushesNeeded(current.Position, pit) <= bestPushes)
            best = current;
        chosen = best?.InstanceID ?? 0;
        return best;
    }

    private bool LinedUp(Actor player, Actor bomb)
    {
        var toBomb = bomb.Position - player.Position;
        var toPit = pit - bomb.Position;
        if (toBomb.Length() > Reach)
            return false;
        var cos = toBomb.Normalized().Dot(toPit.Normalized());
        return cos >= MathF.Cos(AimToleranceDeg * MathF.PI / 180f);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var target = PushOn ? Choose(actor) : null;
        var hitNow = target != null && LinedUp(actor, target);
        foreach (var b in bombs)
        {
            if (hints.FindEnemy(b) is not { } e)
                continue;
            e.Priority = hitNow && b == target ? 10 : AIHints.Enemy.PriorityForbidden;
        }

        if (target != null)
        {
            var spot = SpotBehind(target.Position, pit);
            if (InBounds(spot))
            {
                hints.AddForbiddenZone(new SDInvertedCircle(spot, SpotRadius), DateTime.MaxValue);
                // stay behind the bomb until the last moment the walk out of Sand Breath and the blasts still makes it
                hints.HoldPositioningLate = true;
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (!PushOn || World.Actors.Find(chosen) is not { } bomb)
            return;
        Arena.AddLine(bomb.Position, pit, Colors.Safe, 2f);
        Arena.AddCircle(SpotBehind(bomb.Position, pit), SpotRadius, Colors.Safe, 2f);
    }
}

sealed class ExplosionCross(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ExplosionCross, new AOEShapeCross(40f, 4f))
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell) { }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor.OID == (uint)OID.GobbieBombBig && animState1 == 1)
        {
            var origin = actor.Position;
            var rotation = actor.Rotation;
            var layer = ResolveArenaProjectionLayer(actor.PosRot.Y);
            var activation = World.CurrentTime.AddSeconds(6d);
            Casters.Add(new(Shape, origin, rotation, activation, actorID: actor.InstanceID, shapeDistance: Shape.Distance(origin, rotation),
                arenaProjectionLayer: layer, restrictToArenaProjectionLayer: RestrictToArenaProjectionLayer));
        }
    }

    // The bombs roll after they arm, and blow where they stop. Saar, 2026-10-25: two rolled 3.6-4y at 69.3s, two seconds
    // after arming and five before the blast. The circle follows its bomb until it goes off.
    public override void Update()
    {
        base.Update();
        var aoes = CollectionsMarshal.AsSpan(Casters);
        for (var i = 0; i < aoes.Length; ++i)
            if (World.Actors.Find(aoes[i].ActorID) is { } bomb)
                aoes[i].Origin = bomb.Position;
    }
}

sealed class LoosefroxInkyjotsStates : StateMachineBuilder
{
    public LoosefroxInkyjotsStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BurrowSandPillar>()
            .ActivateOnEnter<SandBreath>()
            .ActivateOnEnter<GobspinHeadlops>()
            .ActivateOnEnter<BombToss>()
            .ActivateOnEnter<GobspinHeadlopsDonut>()
            .ActivateOnEnter<GoblinHammer>()
            .ActivateOnEnter<GobbieBombs>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<ExplosionCross>()
            .ActivateOnEnter<Earthquake>()
            .ActivateOnEnter<BombPush>()
            .Raw.Update = () => AllDeadOrDestroyed(LoosefroxInkyjots.Bosses);
    }
}

[ModuleInfo(CFCID = 1089u, NameID = 14561u, PrimaryActorOID = (uint)OID.LoosefroxInkyjots, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "gynorhino (ported from BMR)")]
public sealed class LoosefroxInkyjots(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(520f, -420f), new ArenaBoundsCircle(22f))
{
    public static readonly uint[] Bosses = [(uint)OID.LoosefroxInkyjots, (uint)OID.ChewchumPopoto];
    private Actor? _loosefrox;
    private Actor? _chewchum;

    public Actor? Loosefrom() => _loosefrox;
    public Actor? Chewchum() => _chewchum;

    protected override void UpdatePreModuleActivation()
    {
        _loosefrox ??= GetActor((uint)OID.LoosefroxInkyjots);
        _chewchum ??= GetActor((uint)OID.ChewchumPopoto);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(_loosefrox);
        Arena.Actor(_chewchum);
    }

    private BombPush? _push;

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // The quicksand pits never clear: the walk back to the boss goes round them, not stands behind them (BombPush's
        // escape puts the player beside one).
        hints.WalkBackAroundLiveGround = true;
        var knockedUp = (_push ??= FindComponent<BombPush>())?.KnockedUp ?? false;
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            if (e.Actor.OID == (uint)OID.ChewchumPopoto)
            {
                // A bomb in its pit: the worm first, which the rotation reads as the window to burst in.
                if (knockedUp)
                {
                    e.Priority = 2;
                }
                else if (!_loosefrox?.IsDead ?? false)
                {
                    e.Priority = AIHints.Enemy.PriorityUndesirable;
                }
                break;
            }
        }
    }
}

