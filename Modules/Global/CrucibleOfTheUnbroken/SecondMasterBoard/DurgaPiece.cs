// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondMasterBoard.DurgaPiece;

public enum OID : uint {
    DurgaPiece = 0x4CF6,
    Helper = 0x233C,
    ElectricPillar = 0x4EA8, // R1.000, x4
    Missile = 0x4CF7, // R1.300, x8
    SpinnerRookPiece = 0x4CF8, // R0.750, x0 (spawn during fight)
}

public enum AID : uint {
    AutoAttack = 49270, // DurgaPiece->player, no cast, single-target
    ElectricAbsorption = 49263, // DurgaPiece->self, 5.0s cast, single-target
    Summon = 49269, // DurgaPiece->self, 3.0s cast, single-target
    GroundingJoltBoss = 49264, // DurgaPiece->self, 4.5+0.7s cast, single-target
    GroundingJoltBig = 49265, // Helper->self, 5.2s cast, range 20 circle
    GroundingJoltSmall = 49266, // Helper->self, 5.2s cast, range 6 circle
    AtomicRay = 49272, // DurgaPiece->self, 13.0s cast, range 60 circle
    DiffusionRayBoss = 49275, // DurgaPiece->self, 4.0+1.0s cast, single-target
    DiffusionRay = 49276, // Helper->self, 5.0s cast, range 30 120.000-degree cone
    Missile = 49267, // DurgaPiece->self, 3.0s cast, single-target
    Missile1 = 49512, // DurgaPiece->self, no cast, single-target
    VoyageActor = 49268, // 4CF7->self, 1.0s cast, single-target
    Voyage = 50688, // Helper->self, 1.5s cast, range 100 width 4 rect
    ThermobaricChargeBoss = 49273, // DurgaPiece->self, 4.2+0.8s cast, single-target
    ThermobaricCharge = 49274, // Helper->self, 2.0s cast, range 55 circle

    // SpinnerRookPiece
    AetherCharge = 49271, // 4CF8->DurgaPiece, 3.0s cast, single-target
}

public enum SID : uint {
    Gen = 2056, // DurgaPiece->4CF7/DurgaPiece, extra=0x476/0x23E/0xD1
    Gen1 = 2193, // DurgaPiece->DurgaPiece, extra=0x48C
    Bleeding = 3077, // none->player, extra=0x0
    Bleeding1 = 3078, // none->player, extra=0x0
}

public enum IconID : uint {
    ThermobaricChargeLockOn = 23, // player->self
}

public enum TetherID : uint {
    ElectricPillarNETether = 416, // 4EA8->DurgaPiece
    ElectricPillarSETether = 417, // 4EA8->DurgaPiece
    ElectricPillarNWTether = 418, // 4EA8->DurgaPiece
    ElectricPillarSWTether = 419, // 4EA8->DurgaPiece
}

sealed class AtomicRay(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AtomicRay, "Raidwide - Pet cover does not work");
sealed class DiffusionRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DiffusionRay, new AOEShapeCone(30.0f, 60.0f.Degrees()));

// Due to the boss rotating as he starts these casts, it impossible to tell any earlier than just when the cast is started
sealed class GroundingJoltBig(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GroundingJoltBig, 20.0f);
sealed class GroundingJoltSmall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GroundingJoltSmall, 6.0f);

sealed class Missile(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Voyage, new AOEShapeRect(100.0f, 2.0f));
sealed class MissileBait(ModuleBase module) : Components.GenericBaitProximity(module) {
    private readonly AOEShapeRect shape = new(100.0f, 2.0f);

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id) {
        if (actor.OID == (uint)OID.Missile && id == 4565) {
            CurrentBaits.Add(new(actor, shape));
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.Voyage) {
            if (CurrentBaits.Count > 0) {
                CurrentBaits.RemoveAt(0);
            }
        }
    }
}

// Held until the drop's own cast starts, not the boss's: the drop is placed where the bait stands up to 0.72s after the
// boss's cast ends, always before the drop's cast appears (89 drops). Let go at the boss's cast, Saar walked back toward
// the boss and dropped it 4.4y off the corner, which leaves a push only a few degrees either side of the diagonal that
// stays on the floor (2026-11-07).
sealed class ThermobaricChargeBait(ModuleBase module) : Components.BaitAwayIcon(module, 2.0f, (uint)IconID.ThermobaricChargeLockOn,
    (uint)AID.ThermobaricCharge) {

    // the drop's cast starts about 5.1s after the icon (the bait's activation); one not come by this is not coming
    private const double StaleAfterSeconds = 2.0d;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.ThermobaricCharge && CurrentBaits.Count != 0) {
            CurrentBaits.RemoveAt(0);
        }
    }

    public override void Update() {
        base.Update();
        CurrentBaits.RemoveAll(b => World.CurrentTime > b.Activation.AddSeconds(StaleAfterSeconds));
    }

    private readonly WPos[] positions = [
        new(138.0f, -18.0f), // NE
        new(138.0f, 18.0f), // SE
        new(102.0f, -18.0f), // NW
        new(102.0f, 18.0f) // SW
    ];

    public override void DrawArenaForeground(int pcSlot, Actor pc) {
        base.DrawArenaForeground(pcSlot, pc);

        if (CurrentBaits.Count == 0) {
            return;
        }

        foreach (var point in positions) {
            Arena.AddCircle(point, 1.0f, Colors.Safe);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        base.AddHints(slot, actor, hints);

        if (CurrentBaits.Count == 0) {
            return;
        }

        hints.Add("Drop the bait at a corner of the map!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        base.AddAIHints(slot, actor, assignment, hints);

        if (!IsBaitTarget(actor)) {
            return;
        }

        var bestPosition = positions[0];
        var playerPosition = actor.Position;
        var bestDistance = (playerPosition - bestPosition).LengthSq();

        foreach (var point in positions) {
            var distance = (playerPosition - point).LengthSq();
            if (distance < bestDistance) {
                bestDistance = distance;
                bestPosition = point;
            }
        }

        hints.AddForbiddenZone(new SDInvertedCircle(bestPosition, 1.0f));
    }
}
// 40y straight away from the drop. The push starts 0.50-0.56s after the cast ends (88 pushes, Saar's and Korha's,
// 2026-11-01 to 11-07), but the base list lets the caster go when the cast ends. So the guard was gone for that half
// second, and the walk back to the boss ran 2.5y down the push line. 12 of Saar's 23 pushes ended past the wall and bled
// him (2026-11-07). The guard now holds until the push starts. It is judged with the real 40y (BossmodReborn's 39 was a
// yalm short), and 1y short of the wall, since the bleed starts at the wall (0.5y past it bled him at 18:41).
sealed class ThermobaricChargeKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ThermobaricCharge, 40.0f) {
    private const double PushStartsAfterSeconds = 0.5d;
    // just past the latest push seen (0.56s): held any longer, the guard is still up mid-push and calls the landing danger
    private const double HoldAfterCastSeconds = 0.6d;
    private const float WallMargin = 1.0f;
    // on the drop itself the push has no direction to judge; Korha steps 2-3y in from her corner
    private const float OffDropRadius = 1.5f;
    private (WPos Origin, DateTime Push, DateTime Until)? held;

    public override void OnCastFinished(Actor caster, ActorCastInfo cast) {
        if (cast.Action.ID == WatchedAction) {
            held = (cast.LocXZ != default ? cast.LocXZ : caster.Position, World.FutureTime(PushStartsAfterSeconds), World.FutureTime(HoldAfterCastSeconds));
        }
        base.OnCastFinished(caster, cast);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        WPos origin;
        DateTime activation;
        if (Casters.Count != 0) {
            origin = Casters[0].Origin;
            activation = Casters[0].Activation;
        } else if (held is { } h && World.CurrentTime < h.Until) {
            origin = h.Origin;
            activation = h.Push;
        } else {
            return;
        }

        if (IsImmune(slot, activation)) {
            return;
        }

        var half = 20.0f - WallMargin;
        hints.AddForbiddenZone(new SDKnockbackInAABBRectAwayFromOrigin(Module.Center, origin, 40.0f, half, half), activation);
        hints.AddForbiddenZone(new SDCircle(origin, OffDropRadius), activation);
    }
}

sealed class DurgaPieceStates : StateMachineBuilder {
    public DurgaPieceStates(ModuleBase module) : base(module) {
        TrivialPhase()
            .ActivateOnEnter<AtomicRay>()
            .ActivateOnEnter<DiffusionRay>()
            .ActivateOnEnter<GroundingJoltBig>()
            .ActivateOnEnter<GroundingJoltSmall>()
            .ActivateOnEnter<Missile>()
            .ActivateOnEnter<MissileBait>()
            .ActivateOnEnter<ThermobaricChargeBait>()
            .ActivateOnEnter<ThermobaricChargeKnockback>();
    }
}

[ModuleInfo(CFCID = 1092u, NameID = 14657u, PrimaryActorOID = (uint)OID.DurgaPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class DurgaPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, 0f), new ArenaBoundsRect(20f, 20f)) {
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i) {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch {
                (uint)OID.SpinnerRookPiece => 2,
                (uint)OID.DurgaPiece => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc) {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.SpinnerRookPiece));
    }

    private readonly string[] _prePullHints = [
        "Atomic Ray: Heavy raidwide damage - Pet cover will not take damage for you",
    ];

    public override string[] PrePullHints => _prePullHints;
}
