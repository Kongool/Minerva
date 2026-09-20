// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondMasterBoard.DrakePiece;

public enum OID : uint {
    DrakePiece = 0x4CF0,
    Helper = 0x233C,
    BallOfFire = 0x4CF1, // R1.500, x0 (spawn during fight)
    TwistingBlaze = 0x4CF3, // R1.000, x0 (spawn during fight)
    BarbmolePiece = 0x4CF2, // R1.750, x0 (spawn during fight)
    MorphoPiece = 0x4CF5, // R0.800, x0 (spawn during fight)
    AbaddonPiece = 0x4CF4, // R4.800, x0 (spawn during fight)
}

public enum AID : uint {
    AutoAttack = 49681, // DrakePiece->player, no cast, single-target
    Teleport = 49257, // DrakePiece->location, no cast, single-target
    BlazeSpikes = 49253, // DrakePiece->self, 3.0s cast, single-target
    ArmOfPurgatory = 49258, // 4CF1->self, 1.0s cast, range 4 circle
    BlowingRingOfFire = 49259, // 4CF3->self, 5.0s cast, range 3-50 donut
    BurningCycloneBoss = 49255, // DrakePiece->self, 4.0+1.0s cast, single-target
    BurningCyclone = 49256, // Helper->self, 5.0s cast, range 50 120.000-degree cone
    ManglingFang = 49254, // DrakePiece->player, 5.0s cast, single-target

    // BarbmolePiece
    AutoAttackBarbmolePiece = 50397, // 4CF2->player, no cast, single-target
    NeedlesOut = 49250, // 4CF2->self, 3.0s cast, single-target
    SeedingNeedles = 49252, // BarbmolePiece->self, 5.0s cast, range 20 circle

    // MorphoPiece
    UnwittingWings = 49260, // 4CF5->player, 4.0s cast, single-target
    DelusionDust = 49261, // 4CF5->self, no cast, range 50 circle

    // AbaddonPiece
    AutoAttackAbaddonPiece = 49682, // 4CF4->player, no cast, single-target
}

public enum SID : uint {
    BlazeSpikes = 5465, // DrakePiece->DrakePiece, extra=0x64
    NeedlesOut = 5145, // 4CF2->4CF2, extra=0x64
    Bleeding = 4068, // 4CF2->player, extra=0x1/0x2
    WitsEnd = 5146, // 4CF5->player, extra=0x1/0x2/0x3/0x4/0x5/0x6/0x7/0x8/0x9/0xA
    Confused = 1283, // 4CF5->player, extra=0x0
    Gen = 3572, // 4CF4->4CF5, extra=0x12
}

public enum IconID : uint {
    ManglingFangTankBuster = 218, // player->self
}

public enum TetherID : uint {
    MorphoPieceTether = 17, // 4CF5->player
}

sealed class ManglingFang(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ManglingFang);
sealed class SeedingNeedles(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SeedingNeedles, 20.0f);

sealed class BlazeSpikes(ModuleBase module) : Components.Dispel(module, (uint)SID.BlazeSpikes, (uint)AID.BlazeSpikes);
sealed class BlazeSpikesTarget(ModuleBase module) : Components.GenericInvincible(module, "Attacking boss with spikes debuff!") {
    private readonly List<Actor> avoidBosses = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.BlazeSpikes) {
            avoidBosses.Add(caster);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status) {
        if (status.ID == (uint)SID.BlazeSpikes) {
            avoidBosses.Remove(actor);
        }
    }

    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor) => CollectionsMarshal.AsSpan(avoidBosses);
}

// Uses GenericAOEs component to show it sooner
sealed class BurningCyclone(ModuleBase module) : Components.GenericAOEs(module) {
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCone shape = new(50.0f, 60.0f.Degrees());

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID == (uint)AID.Teleport) {
            var direction = Angle.FromDirection(Module.Center - spell.TargetXZ);
            aoes.Add(new(shape, spell.TargetXZ, direction, actorID: caster.InstanceID));
        }

        if (spell.Action.ID == (uint)AID.BurningCyclone) {
            if (aoes.Count > 0) {
                aoes.RemoveAt(0);
            }
        }
    }

    // If the caster dies before doing the cast, we have to clean it up manually
    public override void Update() {
        base.Update();

        if (aoes.Count == 0) {
            return;
        }

        var target = World.Actors.Find(aoes[0].ActorID);
        if (target == null || target.IsDead) {
            aoes.Clear();
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);
}

sealed class BallOfFireRing(ModuleBase module) : Components.GenericAOEs(module) {
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCircle circle = new(4.0f);
    private readonly AOEShapeDonut donut = new(3.0f, 50.0f);

    public override void OnActorCreated(Actor actor) {
        if (actor.OID == (uint)OID.BallOfFire) {
            aoes.Add(new(circle, actor.Position, actor.Rotation, actorID: actor.InstanceID));
        }

        if (actor.OID == (uint)OID.TwistingBlaze) {
            var aoeIndex = aoes.FindIndex(aoe => aoe.Origin.AlmostEqual(actor.Position, 0.5f));
            if (aoeIndex >= 0) {
                aoes.RemoveAt(aoeIndex);
            }

            aoes.Add(new(donut, actor.Position, actor.Rotation, actorID: actor.InstanceID));
        }
    }

    public override void OnActorDestroyed(Actor actor) {
        if (actor.OID == (uint)OID.BallOfFire) {
            if (aoes.Count > 0) {
                aoes.RemoveAll(aoe => aoe.ActorID == actor.InstanceID);
            }
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.BlowingRingOfFire) {
            if (aoes.Count > 0) {
                aoes.RemoveAll(aoe => aoe.ActorID == caster.InstanceID);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);
}

// TODO the player must have aggro - need a way to do this
sealed class AbaddonEat(ModuleBase module) : ModuleComponent(module) {
    private readonly AOEShapeCircle shape = new(1.0f);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        var abaddon = Module.Enemies((uint)OID.AbaddonPiece).FirstOrDefault(abaddon => !abaddon.IsDead);
        if (abaddon == null || abaddon.TargetID != actor.InstanceID) {
            return;
        }

        var morphoPieces = Module.Enemies((uint)OID.MorphoPiece).ToList();

        var spots = new List<ShapeDistance>();
        foreach (var morpho in morphoPieces) {
            spots.Add(shape.Distance(morpho.Position, default));
        }

        if (spots.Count == 0) {
            return;
        }

        hints.AddForbiddenZone(new SDInvertedUnion([.. spots]));
    }
}

sealed class DrakePieceStates : StateMachineBuilder {
    public DrakePieceStates(ModuleBase module) : base(module) {
        TrivialPhase()
            .ActivateOnEnter<BurningCyclone>()
            .ActivateOnEnter<ManglingFang>()
            .ActivateOnEnter<BallOfFireRing>()
            .ActivateOnEnter<BlazeSpikes>()
            .ActivateOnEnter<BlazeSpikesTarget>()
            .ActivateOnEnter<AbaddonEat>()
            .ActivateOnEnter<SeedingNeedles>()
            .Raw.Update = () => AllDeadOrDestroyed(DrakePiece.Bosses);
    }
}

[ModuleInfo(CFCID = 1092u, NameID = 14651u, PrimaryActorOID = (uint)OID.DrakePiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class DrakePiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(520f, 0f), new ArenaBoundsRect(20f, 15f)) {
    public static readonly uint[] Bosses = [(uint)OID.AbaddonPiece, (uint)OID.MorphoPiece, (uint)OID.BarbmolePiece, (uint)OID.DrakePiece];

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        var morphoPieceAlive = Enemies((uint)OID.MorphoPiece).Any(morpho => !morpho.IsDead);
        var AbaddonPieceAlive = Enemies((uint)OID.AbaddonPiece).Any(abaddon => !abaddon.IsDead);

        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i) {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch {
                (uint)OID.AbaddonPiece => morphoPieceAlive ? AIHints.Enemy.PriorityForbidden : 4,
                (uint)OID.MorphoPiece => AbaddonPieceAlive ? AIHints.Enemy.PriorityForbidden : 3, // This should never happen, but it could
                (uint)OID.BarbmolePiece => e.Actor.FindStatus((uint)SID.NeedlesOut) != null ? AIHints.Enemy.PriorityForbidden : 2,
                (uint)OID.DrakePiece => e.Actor.FindStatus((uint)SID.BlazeSpikes) != null ? AIHints.Enemy.PriorityForbidden : 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc) {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.AbaddonPiece));
        Arena.Actors(Enemies((uint)OID.MorphoPiece));
        Arena.Actors(Enemies((uint)OID.BarbmolePiece));
    }

    private readonly string[] _prePullHints = [
        "DrakePiece: Uses Blaze Spikes which will reflect damage when attacked - This can be dispelled",
        "BarbmolePiece: Casts Needles Out, which will make it reflect with a stacking bleed when attacked.",
        "AbaddonPiece: Eats MorphoPiece and gains regen stacks. Will not gain regen if MorphoPiece is low HP when eaten - Don't kill until all MorphoPiece are dead",
        "MorphoPiece: Applies WitsEnd upon reaching 16 stacks will cause Confused debuff. Killing MorphoPiece will cause confuse debuff as well, must be eaten by AbaddonPiece"
    ];

    public override string[] PrePullHints => _prePullHints;
}
