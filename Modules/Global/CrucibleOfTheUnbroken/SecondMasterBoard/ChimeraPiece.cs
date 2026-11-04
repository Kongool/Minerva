// Chimera Piece, Second Master Board (elite). Not in BossmodReborn, so written from Korha's 2026-11-03 16:55 pull
// (minerva-20261103-165544-Chimera Piece.log), the Action sheet and the Crucible research notes (wiki and Icy Veins).
// With no module the first Ram's Roar left the guessed shapes no safe spot: Korha stood 4y from the boss inside the 12y
// circle and her Apkallu took it for her and died. The orb and the ice puddle had nothing drawn at all.
//
// The heads: each breath is a 240-degree cone (omen x6d3_b1_fan240). The helper's cast rotation is the cone's own
// direction (the resolve line only repeats the boss's facing): the Ram's is centred 120 degrees to the boss's right, the
// Dragon's 120 to its left, the Lion's straight ahead. Each Roar comes with a breath: the Ram's a 12y circle, the
// Dragon's an 8-40 donut (omen gl_sircle_4008ah1, which BossmodReborn draws 8-40 for eight other actions).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondMasterBoard.ChimeraPiece;

public enum OID : uint
{
    ChimeraPiece = 0x4CFC, // R3.700
    Helper = 0x233C,
    BallLightning = 0x4CFD, // R1.000, from Cacophony: chases one target and explodes on it
    RamsKeeperIce = 0x1EC102, // EventObj: the ice puddle the Ram's Keeper leaves where it was dropped
}

public enum AID : uint
{
    AutoAttack = 49680, // ChimeraPiece->player/pet, no cast, single-target
    ToxicTantrum = 49332, // ChimeraPiece->self, 5.0s cast, raidwide plus a cleansable poison

    RamsRoarVisual = 49309, // ChimeraPiece->self, 6.0s cast, single-target (with the Ram's breath)
    RamsRoarVisual2 = 49310, // ChimeraPiece->self, 6.0s cast, single-target (not seen)
    RamsRoarVisual3 = 49311, // ChimeraPiece->self, 6.0s cast, single-target (not seen)
    DragonsRoarVisual = 49312, // ChimeraPiece->self, 6.0s cast, single-target (with the Ram's breath)
    DragonsRoarVisual2 = 49313, // ChimeraPiece->self, 6.0s cast, single-target (with the Dragon's breath)
    DragonsRoarVisual3 = 49314, // ChimeraPiece->self, 6.0s cast, single-target (not seen)
    RamsRoar = 49315, // Helper->self, 7.0s cast, range 12 circle
    DragonsRoar = 49316, // Helper->self, 7.0s cast, range 8-40 donut
    RamsBreath = 49300, // Helper->self, 7.0s cast, range 60 240-degree cone, centred at the boss's facing -120
    DragonsBreath = 49302, // Helper->self, 7.0s cast, range 60 240-degree cone, centred at facing +120
    LionsBreath = 49304, // Helper->self, 7.0s cast, range 60 240-degree cone, centred at facing (not seen)

    RushingRoarRam = 49323, // ChimeraPiece->self, 5.9s cast, single-target: dash, then the Ram's breath (not seen)
    RushingRoarDragon = 49324, // ChimeraPiece->self, 5.9s cast, single-target: dash, then the Dragon's breath
    RushingRoarLion = 49325, // ChimeraPiece->self, 5.9s cast, single-target: dash, then the Lion's breath
    RushingRoarDash = 49322, // ChimeraPiece->tethered player, no cast, single-target; the boss then faces them
    RamsBreathRushVisual = 49326, // ChimeraPiece->self, 0.5s cast, single-target
    RamsBreathRush = 49327, // Helper->self, 1.5s cast, range 60 240-degree cone (not seen)
    DragonsBreathRushVisual = 49328, // ChimeraPiece->self, 0.5s cast, single-target
    DragonsBreathRush = 49329, // Helper->self, 1.5s cast, range 60 240-degree cone
    LionsBreathRushVisual = 49330, // ChimeraPiece->self, 0.5s cast, single-target
    LionsBreathRush = 49331, // Helper->self, 1.5s cast, range 60 240-degree cone

    Cacophony = 49317, // ChimeraPiece->self, 3.0s cast, single-target: a Ball Lightning appears 0.9s later
    ChaoticChorus = 49318, // BallLightning->self, no cast, range 6 circle, on reaching its target

    RamsKeeperVisual = 49319, // ChimeraPiece->location, 4.0s cast, single-target
    RamsKeeper = 49320, // Helper->location, 4.5s cast, range 6 circle, where the marked player stood
}

public enum SID : uint
{
    DeepFreeze = 3519, // from the Ram's Keeper ice: 10s frozen in place
}

public enum IconID : uint
{
    RamsKeeper = 669, // player: the Ram's Keeper is dropped where they stand 4.5-4.8s later
}

public enum TetherID : uint
{
    RushingRoar = 57, // ChimeraPiece->player, until the dash
}

sealed class ToxicTantrum(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ToxicTantrum);
sealed class RamsRoar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RamsRoar, 12f);
sealed class DragonsRoar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DragonsRoar, new AOEShapeDonut(8f, 40f));
sealed class Breaths(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RamsBreath, (uint)AID.DragonsBreath, (uint)AID.LionsBreath,
    (uint)AID.RamsBreathRush, (uint)AID.DragonsBreathRush, (uint)AID.LionsBreathRush], new AOEShapeCone(60f, 120f.Degrees()));
sealed class RamsKeeper(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RamsKeeper, 6f);

/// <summary>
/// Rushing Roar: the boss dashes at the tethered player and then breathes along the way it now faces. The breath's own
/// cast bar gives 1.4s, too little to get round the boss from in front of it. The dash already fixes the direction, and
/// which head breathes is known from the Rushing Roar cast, so the cone is drawn from the dash: 3.5s of warning (3.51
/// and 3.47s from dash to breath in Korha's pull). The breath's cast then takes over.
/// </summary>
sealed class RushingBreath(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.RushingRoarDash)
{
    private static readonly AOEShapeCone cone = new(60f, 120f.Degrees());
    private const double DashToBreath = 3.4d;
    private readonly List<AOEInstance> aoes = [];
    private Angle? offset;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.RushingRoarRam:
                offset = (-120f).Degrees();
                break;
            case (uint)AID.RushingRoarDragon:
                offset = 120f.Degrees();
                break;
            case (uint)AID.RushingRoarLion:
                offset = default(Angle);
                break;
            case (uint)AID.RamsBreathRush or (uint)AID.DragonsBreathRush or (uint)AID.LionsBreathRush:
                aoes.Clear();
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID != (uint)AID.RushingRoarDash || offset is not { } o)
            return;
        aoes.Clear();
        aoes.Add(new(cone, caster.Position, spell.Rotation + o, World.FutureTime(DashToBreath)));
        offset = null;
    }

    public override void Update() => aoes.RemoveAll(a => a.Activation < World.FutureTime(-2d));
}

/// <summary>
/// Cacophony's Ball Lightning picks a target (Korha's Mantis at 70s, Korha herself at 132s), sits still for about 5s
/// (5.0 and 4.8s), then rushes it at 4-5 yalms a second and explodes for 6y with no cast bar once within about 2y of it:
/// 6.6s and 8.5s after it appeared. Korha stood still for the second one and took it with a Vulnerability Up.
///
/// <para>BossmodReborn keeps the player off the ground ahead of the orb's facing (Eureka Orthos's floor-50 Chimera). That
/// is not where this one goes: the closed-loop replay stood beside the idle orb, out of a capsule along its facing, and
/// was caught when it rushed the Mantis on the other side. So the lane is drawn to the target instead. Chased by it
/// yourself, you are kept out of the orb's reach plus one second of its rush.</para>
/// </summary>
sealed class BallLightning(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.ChaoticChorus)
{
    private const float Blast = 6f;
    private const float RushPerSecond = 5f;
    private static readonly AOEShapeCircle circle = new(Blast);
    private readonly List<Actor> orbs = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (orbs.Count == 0)
            return [];
        var aoes = new AOEInstance[orbs.Count];
        for (var i = 0; i < orbs.Count; ++i)
            aoes[i] = new(circle, orbs[i].Position);
        return aoes;
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.BallLightning)
            orbs.Add(actor);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ChaoticChorus)
            orbs.Remove(caster);
    }

    public override void OnActorDestroyed(Actor actor) => orbs.Remove(actor);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var orb in orbs)
        {
            hints.AddForbiddenZone(new SDCircle(orb.Position, Blast));
            if (World.Actors.Find(orb.TargetID) is not { } target || target == orb)
                continue;
            var lane = target.Position - orb.Position;
            if (lane.LengthSq() < 0.01f)
                continue;
            if (target == actor)
            {
                hints.AddForbiddenZone(new SDCapsule(orb.Position, lane, Math.Min(lane.Length(), RushPerSecond), Blast));
            }
            else
            {
                hints.AddForbiddenZone(new SDCapsule(orb.Position, lane, lane.Length(), Blast));
                hints.AddForbiddenZone(new SDCircle(target.Position, Blast));
            }
        }
    }
}

/// <summary>
/// The Ram's Keeper marks a player; 4.5-4.8s later the boss drops a 6y circle where they stand, and an ice puddle stays
/// there for 25s. The research notes say to drop it at the wall. Korha dropped the first at the west wall 3y from the
/// boss, and the puddle then grew over the boss's side of the floor. While marked, the player is asked to stand within 5y
/// of the wall and 15y or more from the boss. Advice only: real danger wins, and the ask ends when the drop's cast starts.
/// </summary>
sealed class RamsKeeperBait(ModuleBase module) : ModuleComponent(module)
{
    private const float WallBand = 15f; // the floor is a 20y circle
    private const float AwayFromBoss = 15f;
    private ulong marked;
    private DateTime expires;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.RamsKeeper)
        {
            marked = actor.InstanceID;
            expires = World.FutureTime(8d);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RamsKeeper)
            marked = 0;
    }

    private bool IsMarked(Actor actor) => actor.InstanceID == marked && World.CurrentTime <= expires;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!IsMarked(actor))
            return;
        // The far ring forbids nothing reachable; it makes the union read as "anywhere but here", a stand-here instruction
        // (AIHints.PositioningZones), rather than a bounded zone at MaxValue, which the dodge ignores.
        hints.AddForbiddenZone(new SDUnion([new SDCircle(Module.Center, WallBand), new SDCircle(Module.PrimaryActor.Position, AwayFromBoss),
            new SDInvertedCircle(Module.Center, 1000f)]), DateTime.MaxValue);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (IsMarked(actor))
            hints.Add("Drop the ice at the wall, away from Chimera");
    }
}

/// <summary>
/// The Ram's Keeper's ice puddle grows, and touching it is Deep Freeze: 10s unable to move or act. Korha stood 12.2y from
/// the first one's centre from 72.5s and was frozen at 78.15s, 13.2s after it appeared (an animation plays at 5.8s), and
/// spent the next Roar frozen. That is the only measurement, and it fits steady growth from the drop as well as faster
/// growth from the animation, so the drawn radius takes whichever is bigger, 1.5s ahead, up to the whole floor.
/// </summary>
sealed class RamsKeeperIce(ModuleBase module) : Components.GenericAOEs(module)
{
    private const float StartRadius = 6f;
    private const float MaxRadius = 20f;
    private const double Lead = 1.5d;
    private readonly List<(Actor Ice, DateTime Spawned)> puddles = [];

    public static float RadiusAt(double secondsSinceSpawn)
    {
        var t = (float)secondsSinceSpawn;
        return Math.Min(MaxRadius, StartRadius + Math.Max(0.55f * t, 1.0f * (t - 5.8f)));
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (puddles.Count == 0)
            return [];
        var aoes = new AOEInstance[puddles.Count];
        for (var i = 0; i < puddles.Count; ++i)
        {
            var (ice, spawned) = puddles[i];
            aoes[i] = new(new AOEShapeCircle(RadiusAt((World.CurrentTime - spawned).TotalSeconds + Lead)), ice.Position);
        }
        return aoes;
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.RamsKeeperIce)
            puddles.Add((actor, World.CurrentTime));
    }

    public override void OnActorDestroyed(Actor actor) => puddles.RemoveAll(p => p.Ice == actor);

    // The growing circle alone walked the player out a step at a time as it grew: 0.25y a frame from 208.7 on
    // 2026-11-03 (19:06), until Dragon's Roar, cast by a boss 8.4y from the ice, left only ground inside it, and Korha
    // was frozen at 17.6y. The user: "move outside the frost aura zone right away rather than inch out". So the player
    // is also asked to stand clear of where it ends up, at once: 20y, which that pull bears out (frozen at 17.6y at
    // 18.4s, never at 20.5-21y). Advice only: the growing circle stays the real danger, and a Roar that needs the
    // ground still gets it.
    private const float ClearOfCap = MaxRadius + 1f;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (puddles.Count == 0)
            return;

        var shapes = new ShapeDistance[puddles.Count + 1];
        for (var i = 0; i < puddles.Count; ++i)
            shapes[i] = new SDCircle(puddles[i].Ice.Position, ClearOfCap);
        // Far ground is forbidden too, so the union reads as "stand anywhere but here", a stand-here instruction
        // (AIHints.PositioningZones), rather than a bounded zone at MaxValue, which the dodge ignores.
        shapes[^1] = new SDInvertedCircle(Module.Center, 1000f);
        hints.AddForbiddenZone(new SDUnion(shapes), DateTime.MaxValue);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var (ice, _) in puddles)
            Arena.AddCircle(ice.Position, MaxRadius, Colors.Danger);
    }
}

sealed class ChimeraPieceStates : StateMachineBuilder
{
    public ChimeraPieceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ToxicTantrum>()
            .ActivateOnEnter<RamsRoar>()
            .ActivateOnEnter<DragonsRoar>()
            .ActivateOnEnter<Breaths>()
            .ActivateOnEnter<RushingBreath>()
            .ActivateOnEnter<BallLightning>()
            .ActivateOnEnter<RamsKeeper>()
            .ActivateOnEnter<RamsKeeperBait>()
            .ActivateOnEnter<RamsKeeperIce>();
    }
}

[ModuleInfo(CFCID = 1092u, NameID = 14663u, PrimaryActorOID = (uint)OID.ChimeraPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (from recordings)")]
public sealed class ChimeraPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, -420f), new ArenaBoundsCircle(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.BallLightning));
    }

    private readonly string[] _prePullHints = [
        "Breaths cover 240 degrees: Ram to the right, Dragon to the left, Lion in front; Ram's Roar is out, Dragon's Roar is in",
        "Drop the Ram's Keeper ice at the wall away from Chimera; it grows and freezes",
        "Keep away from the Ball Lightning: it chases one target and explodes",
    ];

    public override string[] PrePullHints => _prePullHints;
}
