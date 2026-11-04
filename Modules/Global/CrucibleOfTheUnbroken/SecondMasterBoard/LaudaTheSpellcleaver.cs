// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondMasterBoard.LaudaTheSpellcleaver;

public enum OID : uint {
    LaudaTheSpellcleaver = 0x4D1E,
    Helper = 0x233C,
    ThanatosIdlePiece = 0x4E5E, // R1.400, x0 (spawn during fight)
    ThanatosPiece = 0x4D24, // R2.000, x0 (spawn during fight)
    EphemeralBlade = 0x4D21, // R1.500, x14
    PossessiveBlade = 0x4D23, // R1.000, x2
    CombustingBlade = 0x4D1F, // R1.000, x6
    DeadlyDemesneGreen = 0x1EC0C4, // R0.500, x0 (spawn during fight), EventObj type
    DeadlyDemesneYellow = 0x1EC0DD, // R0.500, x0 (spawn during fight), EventObj type
    MoltenBlade = 0x4D20, // R1.000, x0 (spawn during fight)
    MagickedBlade = 0x4EB8, // R1.500, x0 (spawn during fight)
    IndefatigableBlade = 0x4D22, // R1.500, x0 (spawn during fight)
}

public enum AID : uint {
    AutoAttack = 50860, // LaudaTheSpellcleaver->players, no cast, range 9 ?-degree cone
    Teleport = 49436, // LaudaTheSpellcleaver->location, no cast, single-target
    Thunderbolt = 49470, // LaudaTheSpellcleaver->self/player, 5.0s cast, range 50 width 6 rect
    VanishingDaggers = 49481, // LaudaTheSpellcleaver->self, 3.0s cast, single-target
    RushVisual = 49482, // 4D21->self, 0.7+0.3s cast, single-target
    Rush = 49483, // Helper->self, 1.0s cast, range 50 width 5 rect
    BeastlyAuraBoss = 49452, // LaudaTheSpellcleaver->self, 6.0s cast, single-target
    BeastlyAura = 49453, // Helper->self, 7.0s cast, range 80 width 80 rect
    GyrocleaveBoss = 49450, // LaudaTheSpellcleaver->self, 6.0s cast, single-target
    Gyrocleave = 49451, // Helper->self, 7.0s cast, range 80 width 20 rect
    GluttonousGuttingBoss = 49445, // LaudaTheSpellcleaver->self, 8.0+0.6s cast, single-target
    GluttonousGutting = 49446, // Helper->self, 13.6s cast, range 50 width 40 rect
    GluttonousGoringBoss = 49448, // LaudaTheSpellcleaver->self, 8.0+0.6s cast, single-target
    GluttonousGoring = 49449, // Helper->self, 13.6s cast, range 40 circle

    PointMaker = 49462, // LaudaTheSpellcleaver->self, 3.0s cast, single-target
    OverpoweringPointBoss = 50846, // LaudaTheSpellcleaver->self, 4.9+2.0s cast, single-target
    OverpoweringPointActor = 49463, // 4D23->self, 1.7+0.3s cast, single-target
    OverpoweringPoint1 = 50847, // LaudaTheSpellcleaver->self, no cast, single-target
    OverpoweringPoint2 = 50848, // Helper->self, 2.0s cast, range 60 width 6 rect
    OverpoweringPoint3 = 49464, // Helper->self, 2.0s cast, range 60 width 6 rect
    MoltenMetalBoss = 49465, // LaudaTheSpellcleaver->self, 6.2+2.1s cast, single-target
    MoltenMetalBoss1 = 49466, // LaudaTheSpellcleaver->self, no cast, single-target
    MoltenMetalBait = 49467, // 4D20->LaudaTheSpellcleaver, 2.5s cast, single-target
    MoltenMetalBaitCircle = 49468, // Helper->self, 3.0s cast, range 6 circle
    BeastlyFlare = 49469, // 4D20->self, 11.0s cast, range 80 circle
    CombustingBlades = 49444, // LaudaTheSpellcleaver->self, no cast, single-target
    CombustingBlades1 = 49447, // LaudaTheSpellcleaver->self, no cast, single-target
    CombustingBladesSpawn1 = 49438, // 4D1F->LaudaTheSpellcleaver, no cast, single-target
    CombustingBladesSpawn2 = 49439, // 4D1F->LaudaTheSpellcleaver, no cast, single-target
    CombustingBladesSpawn3 = 49437, // 4D1F->LaudaTheSpellcleaver, no cast, single-target
    CombustingBladesTeleport = 49440, // Helper->self, 0.5s cast, range 2 circle
    CombustingBladesTeleport1 = 49441, // Helper->self, 0.7s cast, range 2 circle
    CombustingBladesTeleport2 = 49442, // Helper->self, 0.9s cast, range 2 circle
    MagicalCombustion = 49443, // 4D1F->self, 5.0s cast, range 8 circle

    DeadlyDemesne = 49454, // LaudaTheSpellcleaver->self, 3.0s cast, single-target
    FettersYellow = 49458, // Helper->self, no cast, range 5 width 5 rect
    FettersGreen = 49455, // Helper->self, no cast, range 10 width 10 rect
    CageOfMoltenMetal = 49460, // Helper->self, 3.0s cast, range 12 circle
    LifeClaim = 49457, // Helper->self, 3.0s cast, range 15 width 10 cross
    UnseenForce = 49484, // LaudaTheSpellcleaver->self, 3.0s cast, single-target
    UnseenForce1 = 49485, // LaudaTheSpellcleaver->self, no cast, single-target
    UnseenForce2 = 49486, // Helper->player, no cast, single-target
    Shockwave = 49489, // Helper->player, no cast, single-target
    GreaterStrengthBoss = 49472, // LaudaTheSpellcleaver->self, 3.0s cast, single-target
    SearingAxeBoss = 49477, // LaudaTheSpellcleaver->self, 27.1+0.9s cast, single-target
    SearingAxeFinish = 49480, // Helper->self, 2.0s cast, range 80 circle
    SearingAxe = 49478, // Helper->self, no cast, range 80 circle
    SearingAxe1 = 49479, // Helper->self, no cast, range 80 circle
    GuttlerGlutterBoss = 49473, // LaudaTheSpellcleaver->self, 27.1+0.9s cast, single-target
    GuttlerGlutterFinish = 49476, // Helper->self, 2.0s cast, range 80 circle
    GuttlerGlutter = 49474, // Helper->self, no cast, range 80 circle
    GuttlerGlutter1 = 49475, // Helper->self, no cast, range 80 circle

    // ThanatosPiece
    AutoAttackThanatos = 49680, // 4D24->player, no cast, single-target
    InfernalPain = 50541, // 4D24->self, 8.0s cast, range 40 circle
}

public enum SID : uint {
    HighWire = 5562, // LaudaTheSpellcleaver->LaudaTheSpellcleaver, extra=0x1/0x2
    Bind = 5555, // none->player, extra=0x0
    UnseenForce = 5341, // LaudaTheSpellcleaver->player, extra=0x0
    Petrification = 4891, // Helper->player, extra=0x0
    Paralysis = 5388, // LaudaTheSpellcleaver->player, extra=0x0
    Burns = 3065, // none->player, extra=0x0
    Burns1 = 3066, // none->player, extra=0x0
    VulnerabilityDown = 5338, // none->LaudaTheSpellcleaver, extra=0x2/0x1
    MagicDamageUp = 5340, // none->LaudaTheSpellcleaver, extra=0x10/0xF/0xD/0xC/0xB/0xA/0x9/0x8/0x5/0x4/0x3
    Unknown = 2552, // none->LaudaTheSpellcleaver, extra=0x483/0x482/0x477
}

public enum IconID : uint {
    ThunderboltTankBuster = 471, // player->self
    Unknown = 234, // player->self
    UnseenForceCount = 276, // player->self
    MoltenMetalIcon = 669, // player->self
}

public enum TetherID : uint {
    OverpoweringPointTether = 1, // LaudaTheSpellcleaver/4D23->player
    MagickedBladeTether = 197, // 4EB8->LaudaTheSpellcleaver
    IndefatigableBladeTether = 5, // 4D22->LaudaTheSpellcleaver
}

// Both land 2.9-3.2s after an Unseen Force push, and the push is the way out of them (see UnseenForce). Until it has
// happened, where the player stands now says nothing about where they will be, so these do not steer the pushed player.
sealed class GluttonousGutting(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GluttonousGutting, new AOEShapeRect(50.0f, 20.0f)) {
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (Casters.Count == 0 || Module.FindComponent<UnseenForce>()?.PushedBefore(actor, Casters[0].Activation) != true) {
            base.AddAIHints(slot, actor, assignment, hints);
        }
    }
}
sealed class GluttonousGoring(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GluttonousGoring, 40.0f) {
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (Casters.Count == 0 || Module.FindComponent<UnseenForce>()?.PushedBefore(actor, Casters[0].Activation) != true) {
            base.AddAIHints(slot, actor, assignment, hints);
        }
    }
}

sealed class AutoAttack(ModuleBase module) : Components.Cleave(module, (uint)AID.AutoAttack, new AOEShapeCone(9.0f, 55.0f.Degrees())) {
    private readonly BeastlyAura? beastlyAura = module.FindComponent<BeastlyAura>();

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (beastlyAura == null || beastlyAura.knockbacks.Count > 0) {
            return;
        }

        base.AddHints(slot, actor, hints);
    }

    // Set the cleave aoe to be 1.5f so it doesn't overlap with really bad mechanics such as the cage - getting hitting by the cleave is fine
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (beastlyAura == null || beastlyAura.knockbacks.Count > 0 || TimedMechanicPending(hints)) {
            return;
        }

        // Not while an Unseen Force push is coming: keeping the familiar out of the swing forbids a 100y cone from Lauda
        // toward it, which covered the whole end the push has to start from (20:42, 2026-11-03).
        if (Module.FindComponent<UnseenForce>()?.PushedBefore(actor, DateTime.MaxValue) == true) {
            return;
        }

        foreach (var (origin, target, angle) in OriginsAndTargets()) {
            if (actor != target) {
                hints.AddForbiddenZone(Shape, origin.Position, angle, World.FutureTime(1.5d));
            } else {
                AddTargetSpecificHints(actor, origin, hints, World.FutureTime(1.5d));
            }
        }
    }

    // Guttler's cleave: when it swings at the beastmaster, keep the familiar out of it (the user, 2026-10-31).
    protected override bool AvoidOwnPet => true;
}

sealed class Thunderbolt(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(50.0f, 3.0f), (uint)IconID.ThunderboltTankBuster,
    (uint)AID.Thunderbolt, tankbuster: true) {
    // under Cover the familiar takes this hit twice, its own and the beastmaster's: keep it out of the line
    protected override bool AvoidOwnPet => true;

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (CurrentBaits.Count == 0) {
            return;
        }

        base.AddHints(slot, actor, hints);
        hints.Add("Applies paralysis debuff");
    }
}

sealed class BeastlyAura(ModuleBase module) : Components.GenericKnockback(module) {
    public readonly List<Knockback> knockbacks = [];
    private static readonly AOEShapeRect shape = new(80.0f, 40.0f);
    private ShapeDistance distance;
    private const float knockbackDistance = 20.0f;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.BeastlyAura) {
            var act = Module.CastFinishAt(spell);
            var pos = Module.Center;
            var rot = spell.Rotation;
            var offset = 90f.Degrees();
            var rot1 = rot + offset;
            var isAlongZAxis = rot1.AlmostEqual(default, Angle.DegToRad) || rot1.AlmostEqual(180f.Degrees(), Angle.DegToRad);
            knockbacks.Add(new(pos, knockbackDistance, act, shape, rot1, Kind.DirForward));
            knockbacks.Add(new(pos, knockbackDistance, act, shape, rot - offset, Kind.DirForward));
            distance = isAlongZAxis
                ? new SDKnockbackInAABBRectLeftRightAlongZAxis(Module.Center, knockbackDistance, 10f, 24.0f)
                : new SDKnockbackInAABBRectLeftRightAlongXAxis(Module.Center, knockbackDistance, 10f, 24.0f);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.BeastlyAura) {
            if (knockbacks.Count > 0) {
                knockbacks.Clear();
            }
        }
    }

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => CollectionsMarshal.AsSpan(knockbacks);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (knockbacks.Count == 0) {
            return;
        }

        var activation = knockbacks[0].Activation;

        if (!IsImmune(slot, activation)) {
            hints.AddForbiddenZone(distance, activation);
            // Handles the sides of the center of the map (left & right)
            hints.AddForbiddenZone(new SDRect(Module.Center + new WDir(-6.25f, 0f), 0.0f.Degrees(), 20.0f, 20.0f, 3.75f), activation);
            hints.AddForbiddenZone(new SDRect(Module.Center + new WDir(6.25f, 0f), 0.0f.Degrees(), 20.0f, 20.0f, 3.75f), activation);
        }
    }
}

sealed class Rush(ModuleBase module) : Components.GenericAOEs(module) {
    public readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeRect shape = new(50.0f, 2.5f);
    private const float TimeWindowInSeconds = 1.0f;

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id) {
        if (actor.OID == (uint)OID.EphemeralBlade && id == 4562) {
            aoes.Add(new(shape, actor.Position, actor.Rotation, World.FutureTime(15.0f)));
            SortHelpers.SortAOEByActivation(aoes);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID == (uint)AID.Rush) {
            if (aoes.Count > 0) {
                aoes.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) {
        var count = aoes.Count;
        if (count == 0) {
            return [];
        }

        var nextAOEs = CollectionsMarshal.AsSpan(aoes);
        var aoeGroups = 0; // number of aoes that are grouped together, e.g. first group has 4 aoes, second group has 3 aoes
        var lastAOE = 0; // number of aoes we want to show

        while (lastAOE < nextAOEs.Length) {
            // Groups the aoes by their activation time base on the TimeWindowInSecond
            if (lastAOE > 0 && nextAOEs[lastAOE].Activation > nextAOEs[lastAOE - 1].Activation.AddSeconds(TimeWindowInSeconds)) {
                aoeGroups++;
            }

            // Only show the first two groups of AOEs
            if (aoeGroups == 2) {
                break;
            }

            nextAOEs[lastAOE].Color = aoeGroups == 0 ? Colors.Danger : Colors.AOE;
            lastAOE++;
        }

        return nextAOEs[..lastAOE];
    }
}

// Used to set up fake aoes to get the player to stand correctly for the mechanic
sealed class OverpoweringPointFake(ModuleBase module) : Components.GenericAOEs(module) {
    public readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeRect shape = new(40.0f, 1.0f);
    private readonly DeadlyDemesne? deadlyDemesne = module.FindComponent<DeadlyDemesne>();

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID is (uint)AID.OverpoweringPoint2 or (uint)AID.OverpoweringPoint3) {
            aoes.Clear();
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.PointMaker) {
            aoes.Add(new(shape, Module.Center + new WDir(0f, 6.0f), Angle.AnglesCardinals[1], color: Colors.SafeFromAOE, risky: false));
            aoes.Add(new(shape, Module.Center + new WDir(0f, -6.0f), Angle.AnglesCardinals[2], color: Colors.SafeFromAOE, risky: false));
        }
    }

    // Draw on the front since it can overlap with other mechanics
    public override void DrawArenaForeground(int pcSlot, Actor pc) {
        if (deadlyDemesne == null || deadlyDemesne.greenAOEs.Count > 0 || deadlyDemesne.yellowAOEs.Count > 0) {
            return;
        }

        foreach (var aoe in aoes) {
            shape.Draw(Arena, aoe.Origin, aoe.Rotation, color: Colors.SafeFromAOE);
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) {
        if (deadlyDemesne == null || deadlyDemesne.greenAOEs.Count > 0 || deadlyDemesne.yellowAOEs.Count > 0) {
            return [];
        }

        return CollectionsMarshal.AsSpan(aoes);
    }
}
sealed class OverpoweringPoint(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.OverpoweringPoint2, (uint)AID.OverpoweringPoint3],
    new AOEShapeRect(60.0f, 3.0f));

/// <summary>
/// Overpowering Point aims at the tethered player. Everything tethered to them (Lauda and the two Possessive Blades, or
/// the blades alone) fires a 60y line from where it stands toward where the player is when the tether drops, 5.0s
/// after it appears. The lines run on past the floor, and any idle Thanatos on the side walls one touches wakes up and
/// goes for the familiar. On 2026-11-03 four woke at 75s and killed Korha's Behemoth by 87.5s (the user: "aim things
/// not towards the thantos pieces along the sides"). Thunderbolt's line passes through them without waking any.
///
/// <para>So the tethered player is kept off every direction, seen from each tether source, that points a line at a
/// sleeping Thanatos. What is left is the north and south ends of the floor, the stretch BossmodReborn only draws.
/// This is advice: real danger still wins.</para>
/// </summary>
sealed class OverpoweringPointAim(ModuleBase module) : ModuleComponent(module) {
    // The line's half-width plus a yalm: recorded hits reached 4.07 off the line with a 1.4 hitbox, a miss was at 4.1.
    private const float LineReach = 3.0f + 1.0f;
    private const float LineLength = 60.0f;
    private readonly List<(Actor Source, ulong Target)> tethers = [];

    public override void OnTethered(Actor source, in ActorTetherInfo tether) {
        if (tether.ID == (uint)TetherID.OverpoweringPointTether) {
            tethers.Add((source, tether.Target));
        }
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether) {
        if (tether.ID == (uint)TetherID.OverpoweringPointTether) {
            tethers.RemoveAll(t => t.Source == source);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        List<ShapeDistance> aimedAtOne = [];
        foreach (var (source, target) in tethers) {
            if (target != actor.InstanceID) {
                continue;
            }

            List<(float Bearing, float Half)> cones = [];
            foreach (var thanatos in Module.Enemies((uint)OID.ThanatosIdlePiece)) {
                if (thanatos.IsDeadOrDestroyed) {
                    continue;
                }

                var toThanatos = thanatos.Position - source.Position;
                var distance = toThanatos.Length();
                var reach = LineReach + thanatos.HitboxRadius;
                if (distance <= reach || distance > LineLength + thanatos.HitboxRadius) {
                    continue;
                }

                // standing anywhere in this cone aims the source's line within reach of the Thanatos
                cones.Add((Angle.FromDirection(toThanatos).Rad, Angle.Asin(reach / distance).Rad));
            }

            if (cones.Count > 0) {
                aimedAtOne.Add(new SDSharedApexCones(source.Position, [.. cones]));
            }
        }

        if (aimedAtOne.Count == 0) {
            return;
        }

        // Far ground is forbidden too, so the union reads as "stand anywhere but here", a stand-here instruction
        // (AIHints.PositioningZones), rather than a bounded zone at MaxValue, which the dodge ignores.
        aimedAtOne.Add(new SDInvertedCircle(Module.Center, 1000.0f));
        hints.AddForbiddenZone(new SDUnion([.. aimedAtOne]), DateTime.MaxValue);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (tethers.Any(t => t.Target == actor.InstanceID)) {
            hints.Add("Aim the lines north or south, off the Thanatos on the walls");
        }
    }
}

/// <summary>
/// Cones that all start at one point, each a bearing and a half-angle, of unlimited reach. The bearing to a point is taken
/// once and compared with each, where an SDCone per Thanatos rebuilt its edges for every point: about 40 of them cost 19us
/// a point, the dodge measured the aim ~8,000 times a solve, and solves took 150ms (frame drops at 4:04 on 2026-11-03).
/// Signed distance to the nearest cone edge (from the apex once that edge is more than 90 degrees round).
/// </summary>
sealed class SDSharedApexCones(WPos origin, (float Bearing, float Half)[] cones) : ShapeDistance {
    public override float Distance(WPos p) {
        var off = p - origin;
        var d = off.Length();
        if (d < 1e-4f) {
            return 0f;
        }

        var bearing = MathF.Atan2(off.X, off.Z);   // Angle.FromDirection
        var outside = float.MaxValue;               // radians outside the nearest cone, negative inside one
        foreach (var (coneBearing, half) in cones) {
            var diff = bearing - coneBearing;
            diff -= MathF.Round(diff / Angle.TwoPI) * Angle.TwoPI;
            outside = MathF.Min(outside, MathF.Abs(diff) - half);
        }

        return outside >= MathF.PI / 2f ? d : d * MathF.Sin(outside);
    }
}

sealed class DeadlyDemesne(ModuleBase module) : Components.GenericAOEs(module) {
    private readonly AOEShapeCircle yellowCircle = new(12.0f);
    private readonly AOEShapeRect yellowRect = new(2.5f, 2.5f, 2.5f);
    private readonly AOEShapeCross greenCross = new(15.0f, 5.0f);
    private readonly AOEShapeRect greenRect = new(5.0f, 5.0f, 5.0f);
    private const float riskyWindow = 6.0f;
    private readonly Rush? rush = module.FindComponent<Rush>();

    // All AOEs are separate due to how the aoes can either spawn in from renderflag only or EAnim
    public readonly List<AOEInstance> greenAOEs = [];
    public readonly List<AOEInstance> yellowAOEs = [];
    private readonly List<AOEInstance> yellowCages = [];
    private readonly List<AOEInstance> greenCages = [];

    private enum ColourType { NONE, GREEN, YELLOW };
    private ColourType colourFirst = ColourType.NONE;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.DeadlyDemesne) {
            colourFirst = ColourType.NONE;
        }
    }

    public override void OnActorCreated(Actor actor) {
        if (actor.OID != (uint)OID.DeadlyDemesneGreen && actor.OID != (uint)OID.DeadlyDemesneYellow) {
            return;
        }

        if (actor.OID == (uint)OID.DeadlyDemesneGreen) {
            greenAOEs.Add(new(greenCross, actor.Position, actor.Rotation, World.FutureTime(12.1f)));
            greenCages.Add(new(greenRect, actor.Position, actor.Rotation, World.FutureTime(12.0f), color: Colors.Danger));
            SortHelpers.SortAOEByActivation(greenAOEs);
        }

        if (actor.OID == (uint)OID.DeadlyDemesneYellow) {
            yellowAOEs.Add(new(yellowCircle, actor.Position, actor.Rotation, World.FutureTime(11.1f)));
            yellowCages.Add(new(yellowRect, actor.Position, actor.Rotation, World.FutureTime(11.0f), color: Colors.Danger));
            SortHelpers.SortAOEByActivation(yellowAOEs);
        }
    }

    public override void OnActorEAnim(Actor actor, uint state) {
        if (actor.OID != (uint)OID.DeadlyDemesneGreen && actor.OID != (uint)OID.DeadlyDemesneYellow) {
            return;
        }

        if (state == 65538 && colourFirst == ColourType.NONE) {
            if (actor.OID == (uint)OID.DeadlyDemesneGreen) {
                colourFirst = ColourType.GREEN;
            }

            if (actor.OID == (uint)OID.DeadlyDemesneYellow) {
                colourFirst = ColourType.YELLOW;
            }
        }

        if (state == 262152) {
            if (actor.OID == (uint)OID.DeadlyDemesneGreen) {
                greenCages.RemoveAt(0);
            }

            if (actor.OID == (uint)OID.DeadlyDemesneYellow) {
                yellowCages.RemoveAt(0);
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID == (uint)AID.LifeClaim) {
            if (greenAOEs.Count > 0) {
                greenAOEs.RemoveAt(0);
            }
        }

        if (spell.Action.ID == (uint)AID.CageOfMoltenMetal) {
            if (yellowAOEs.Count > 0) {
                yellowAOEs.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) {
        List<AOEInstance> incomingAOEs = []; // Combines the cage & standard AOEs

        // green cages AOEs
        var greenCageCount = greenCages.Count;
        if (greenCageCount != 0) {
            incomingAOEs.AddRange(greenCages);
        }

        // yellow cages AOEs
        var yellowCageCount = yellowCages.Count;
        if (yellowCageCount != 0) {
            incomingAOEs.AddRange(yellowCages);
        }

        // standard aoes
        var count = greenAOEs.Count +  yellowAOEs.Count;
        if (count == 0) { // If there are no standard aoes we can just return the cages
            return CollectionsMarshal.AsSpan(incomingAOEs);
        }

        // If rush is active then we will only show the cages since standing in a cage is worst then getting hit by a rush aoe
        if (rush == null || rush.aoes.Count > 7) {
            return CollectionsMarshal.AsSpan(incomingAOEs);
        }

        // If the total number of actors is > 4 it means it both colours and we have to wait to know which one is first
        var total = greenAOEs.Count + yellowAOEs.Count;
        if (total > 4 && colourFirst == ColourType.NONE) {
            return CollectionsMarshal.AsSpan(incomingAOEs);
        }

        List<AOEInstance> colourAOEs = [];

        // If colourFirst is not set then it means we are only dealing with one colour
        if (colourFirst == ColourType.NONE) {
            colourAOEs.AddRange(greenAOEs);
            colourAOEs.AddRange(yellowAOEs);
        }

        // Otherwie if colourFirst is set then we only add whichever type has aoes until it hits 0 then add the other type
        if (colourFirst == ColourType.GREEN) {
            colourAOEs.AddRange(greenAOEs.Count != 0 ? greenAOEs : yellowAOEs);
        }

        if (colourFirst == ColourType.YELLOW) {
            colourAOEs.AddRange(yellowAOEs.Count != 0 ? yellowAOEs : greenAOEs);
        }

        var standardAOEs = CollectionsMarshal.AsSpan(colourAOEs);
        var time = World.CurrentTime;

        for (var i = 0; i < standardAOEs.Length; i++) {
            ref var aoe = ref standardAOEs[i];
            aoe.Risky = aoe.Activation == default || aoe.Activation <= time.AddSeconds(riskyWindow);
        }

        incomingAOEs.AddRange(standardAOEs);
        return CollectionsMarshal.AsSpan(incomingAOEs);
    }
}

/// <summary>
/// Molten Metal: 5.4s after the mark, a 6y circle drops where the marked player stands and the Molten Blade teleports
/// there to cast Beastly Flare, which reaches the whole floor and falls off with distance (Korha took 913 at 9.2y,
/// 1,107 at 12y, 614-879 at 35-37y across this fight and Guttler's, 2026-10-31 and 2026-11-03). Korha dropped the
/// second one in the middle, where no spot on the 50y floor is far from it.
///
/// <para>So the marked player is sent to the end nub nearer them when the mark appears: 5.4s covers the 25y to
/// either nub from the middle but not the 45y to the far one. The far end is then 45y from the blade. Either nub is
/// clear of the Overpowering Point aim, which runs at the same time in the second Molten Metal. BossmodReborn's own
/// version of this hint is commented out; Guttler's is a hard zone at (520,-400). This is advice: real danger still
/// wins.</para>
/// </summary>
sealed class MoltenMetalBait(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCircle(6.0f), (uint)IconID.MoltenMetalIcon, centerAtTarget: true) {
    // the centres of the two end nubs in BuildArena
    internal static readonly WPos NorthEnd = new(520.0f, -442.5f);
    internal static readonly WPos SouthEnd = new(520.0f, -397.5f);
    private const float EndRadius = 2.5f;
    private WPos end;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID) {
        base.OnEventIcon(actor, iconID, targetID);
        if (iconID == IID) {
            var target = World.Actors.Find(targetID) ?? actor;
            end = (target.Position - NorthEnd).LengthSq() < (target.Position - SouthEnd).LengthSq() ? NorthEnd : SouthEnd;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.MoltenMetalBaitCircle) {
            CurrentBaits.Clear();
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (CurrentBaits.Count == 0) {
            return;
        }

        hints.Add("Bait far away on one side of the map!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (!CurrentBaits.Any(b => b.Target == actor)) {
            return;
        }

        // everything but the nub is forbidden, at MaxValue: a stand-here instruction (AIHints.PositioningZones)
        hints.AddForbiddenZone(new SDInvertedCircle(end, EndRadius), DateTime.MaxValue);
    }
}
sealed class MoltenMetalBaitAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MoltenMetalBaitCircle, 6.0f);

// It reaches the whole floor and falls off with distance (both casts hit Korha, 2026-11-03; see MoltenMetalBait): drawn
// and dodged as before, and announced as a raidwide so a rotation plugin hears it coming, as Guttler's is. The
// announcement stands while Deadly Demesne hides the circle, since the hit lands either way.
sealed class BeastlyFlare(ModuleBase module) : Components.ProximityAOEs(module, (uint)AID.BeastlyFlare, 30.0f) {
    private readonly DeadlyDemesne? deadlyDemesne = module.FindComponent<DeadlyDemesne>();

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) {
        if (deadlyDemesne == null || deadlyDemesne.greenAOEs.Count > 0 || deadlyDemesne.yellowAOEs.Count > 0) {
            return [];
        }

        return base.ActiveAOEs(slot, actor);
    }
}

sealed class MagicalCombustion : Components.SimpleAOEs {
    public MagicalCombustion(ModuleBase module) : base(module, (uint)AID.MagicalCombustion, 8.0f) {
        Color = Colors.Danger;
    }
}

/// <summary>
/// Unseen Force: the player is bound for 7s and turned to face the way Lauda faces, or the opposite way. When the
/// status runs out (12s the first time, 17s after), Shockwave pushes them 40y along their facing, forward if they were
/// turned Lauda's way, backward if not. That is BossmodReborn's rule, and all five Shockwaves of 2026-11-03 fit it.
///
/// <para>The floor is 50y long and 20 wide, so the push stays on it only from an end, pointed down the length. All five
/// pushes left it, and every one was followed by Burns (3065, then 3066) from the ground around the floor. The push is
/// also the way out of what follows. Gluttonous Gutting covers everything south of Lauda at the north end 2.9s later:
/// from the south end the push lands 2.5y short of safety behind her. Gluttonous Goring (40y from her) is the reverse:
/// from the north end the push lands 2.5y short of the south nub.</para>
///
/// <para>So the player is sent to whichever end nub lands them nearest to clear ground against what goes off in the
/// six seconds after the push, or the nearer end when nothing does. For the last 2.5s their facing is held down the
/// floor, through the turning used for gazes. Both are advice; real danger still wins.</para>
/// </summary>
sealed class UnseenForce(ModuleBase module) : Components.GenericKnockback(module) {
    private readonly List<(Actor player, Angle offset, DateTime expireAt)> knockbacks = [];
    private const float knockbackDistance = 40.0f;
    private const float EndRadius = 2.5f;
    // 40y pushed 10 degrees off the length drifts 7y sideways: still on the 20y floor from the middle of a nub
    private static readonly Angle FacingTolerance = 10.0f.Degrees();
    private const double TurnLeadSeconds = 2.5d;
    private const double AfterWindowSeconds = 6.0d;
    // the Shockwave lands 0.1-0.3s after the status runs out; one that has not come by this is not coming
    private const double StaleAfterSeconds = 1.0d;
    private WPos? chosenEnd;

    public override void OnStatusGain(Actor actor, ref ActorStatus status) {
        if (status.ID == (uint)SID.UnseenForce) {
            var opposed = Module.PrimaryActor.Rotation.ToDirection().Dot(actor.Rotation.ToDirection()) < 0;
            // A recording carries no status duration (ReplayParser): it ran 12.0s the first time and 17.0s after, all
            // five times on 2026-11-03.
            var expireAt = status.ExpireAt != default ? status.ExpireAt : World.FutureTime(gains == 0 ? 12.0d : 17.0d);
            ++gains;
            knockbacks.Add((actor, opposed ? 180f.Degrees() : default, expireAt));
        }
    }

    private int gains;

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID == (uint)AID.Shockwave) {
            // the one it landed on, not simply the oldest: an older entry may be someone the push never reached
            if (knockbacks.RemoveAll(k => k.player.InstanceID == spell.MainTargetID) == 0 && knockbacks.Count > 0) {
                knockbacks.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) {
        var count = knockbacks.Count;
        if (count == 0) {
            return [];
        }

        var incomingKnockbacks = CollectionsMarshal.AsSpan(knockbacks);
        foreach (var (player, offset, expireAt) in incomingKnockbacks) {
            if (player != actor) {
                continue;
            }

            return new Knockback[] { new(player.Position, knockbackDistance, expireAt, direction: player.Rotation + offset, kind: Kind.DirForward) };
        }

        return [];
    }

    // Replaces the base's landing check: it counts a landing inside anything going off within 3s as a hit, and the
    // Gutting landing is 2.5y from safety with 2.9s to walk it.
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        // A push that never came is dropped. Xia died to Doom (his Demonic Helm) 1.8s before his Shockwave on 2026-11-03
        // (21:35): the entry stayed, and its facing arc, now overdue, read as a gaze landing this instant, so GazeHold kept
        // him still for the next 60s, through Gyrocleave (866) and two Magical Combustions (1,001 and 813).
        var now = World.CurrentTime;
        knockbacks.RemoveAll(k => k.player.IsDead || now > k.expireAt.AddSeconds(StaleAfterSeconds));
        foreach (var (player, offset, expireAt) in knockbacks) {
            if (player != actor) {
                continue;
            }

            var start = ChooseEnd(slot, actor, expireAt);
            // everything but the nub is forbidden, at MaxValue: a stand-here instruction (AIHints.PositioningZones)
            hints.AddForbiddenZone(new SDInvertedCircle(start, EndRadius), DateTime.MaxValue);
            // While real danger covers the nub, the instruction is set aside and the dodge went anywhere safe: 30y off
            // under the 20:42 Combusting Blades, with 3.2s to walk back once they went off. The pull keeps it close.
            hints.GoalZones.Add(AIHints.GoalProximity(start, 25.0f, 10.0f));
            // Held through the snapshot, which can come a little after the status runs out; an overdue arc holds every step,
            // so the entry is dropped StaleAfterSeconds past (above) and the hold cannot outlast that.
            if ((expireAt - now).TotalSeconds <= TurnLeadSeconds) {
                // the facing that pushes down the floor; every other heading is a forbidden arc, as for a gaze
                var facing = PushFrom(start) - offset;
                hints.ForbiddenDirections.Add((facing + 180.0f.Degrees(), 180.0f.Degrees() - FacingTolerance, expireAt));
            }

            return;
        }

        chosenEnd = null;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        base.AddHints(slot, actor, hints);
        if (chosenEnd is { } end && knockbacks.Any(k => k.player == actor)) {
            hints.Add($"Shockwave: stand at the {(end == MoltenMetalBait.NorthEnd ? "north" : "south")} end, pushed down the floor");
        }
    }

    /// <summary>Will this player be pushed before <paramref name="time"/>?</summary>
    public bool PushedBefore(Actor actor, DateTime time) => knockbacks.Any(k => k.player == actor && k.expireAt < time);

    // from the north nub the push goes south (+z), from the south nub north
    private static Angle PushFrom(WPos end) => end == MoltenMetalBait.NorthEnd ? 0.0f.Degrees() : 180.0f.Degrees();

    private WPos ChooseEnd(int slot, Actor actor, DateTime shove) {
        var north = Clearance(slot, actor, MoltenMetalBait.NorthEnd + knockbackDistance * PushFrom(MoltenMetalBait.NorthEnd).ToDirection(), shove);
        var south = Clearance(slot, actor, MoltenMetalBait.SouthEnd + knockbackDistance * PushFrom(MoltenMetalBait.SouthEnd).ToDirection(), shove);
        if (Math.Abs(north - south) > 1.0f) {
            chosenEnd = north > south ? MoltenMetalBait.NorthEnd : MoltenMetalBait.SouthEnd;
        } else {
            chosenEnd ??= (actor.Position - MoltenMetalBait.NorthEnd).LengthSq() < (actor.Position - MoltenMetalBait.SouthEnd).LengthSq()
                ? MoltenMetalBait.NorthEnd : MoltenMetalBait.SouthEnd;
        }

        return chosenEnd.Value;
    }

    // How far a landing is from leaving the worst zone going off within six seconds of the push: positive is clear,
    // negative is the walk still needed. Clear of everything when nothing goes off then.
    private float Clearance(int slot, Actor actor, WPos landing, DateTime shove) {
        var worst = float.MaxValue;
        foreach (var component in Module.Components) {
            if (component is not Components.GenericAOEs aoes) {
                continue;
            }

            foreach (ref readonly var aoe in aoes.ActiveAOEs(slot, actor)) {
                var after = (aoe.Activation - shove).TotalSeconds;
                if (aoe.Risky && after >= 0.0d && after <= AfterWindowSeconds) {
                    worst = Math.Min(worst, (aoe.ShapeDistance ?? aoe.Shape.Distance(aoe.Origin, aoe.Rotation)).Distance(landing));
                }
            }
        }

        return worst;
    }
}

sealed class SearingAxe(ModuleBase module) : Components.GenericAOEs(module) {
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCircle shape = new(15.0f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.SearingAxeBoss) {
            aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID == (uint)AID.SearingAxeFinish) {
            if (aoes.Count > 0) {
                aoes.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) {
        var count = aoes.Count;
        if (count == 0) {
            return [];
        }

        if (aoes[0].Activation > World.CurrentTime.AddSeconds(5.0f)) {
            return [];
        }

        return CollectionsMarshal.AsSpan(aoes);
    }
}

sealed class GuttlerGlutter(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.GuttlerGlutterBoss, 20.0f) {
    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) {
        var count = Casters.Count;
        if (count == 0) {
            return [];
        }


        if (Casters[0].Activation > World.CurrentTime.AddSeconds(5.0f)) {
            return [];
        }

        return CollectionsMarshal.AsSpan(Casters);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.GuttlerGlutterFinish) {
            if (Casters.Count > 0) {
                Casters.RemoveAt(0);
            }
        }
    }

    // The finisher pushes 20y straight away from Lauda. Korha stood 12.9y north of her and landed at (524.2,-450.6), off
    // the floor, and the Burns finished her (20:42, 2026-11-03). From her centre a 20y push stays on the 20-by-50 floor only
    // from a strip on her north-south line: 3-4y out lands 1-2y short of an end nub's tip, and the push spreads any
    // sideways error 6-8 times, so the strip is 0.3y either side for the nub's 2.5. The user: "try for the strip".
    // Only for the last seconds, since the swords at the ends have to die first (both alive cost 3,035 on the finisher).
    private const double StripLeadSeconds = 4.5d;
    private const float StripHalfWidth = 0.3f;
    private const float LandShortOfTip = 1.5f;   // where in the nub the push should end, measured back from its tip
    private const float LandTolerance = 0.5f;    // either side of that, along the line
    private const float NubTipBeyondCentre = 2.5f;
    private WPos? stripEnd;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        base.AddAIHints(slot, actor, assignment, hints);
        stripEnd = null;
        if (Casters.Count == 0) {
            return;
        }

        var push = Casters[0];
        if ((push.Activation - World.CurrentTime).TotalSeconds > StripLeadSeconds || IsImmune(slot, push.Activation)) {
            return;
        }

        // the nearer end first, the other when Lauda stands too close to it for the strip to fit
        var nearNorth = (actor.Position - MoltenMetalBait.NorthEnd).LengthSq() < (actor.Position - MoltenMetalBait.SouthEnd).LengthSq();
        foreach (var end in nearNorth ? new[] { MoltenMetalBait.NorthEnd, MoltenMetalBait.SouthEnd } : new[] { MoltenMetalBait.SouthEnd, MoltenMetalBait.NorthEnd }) {
            var toEnd = end - push.Origin;
            var land = toEnd.Length() + NubTipBeyondCentre - LandShortOfTip;   // how far from Lauda the push should end
            var from = land - push.Distance - LandTolerance;
            if (from < 2.5f) {
                continue;   // too near her centre: the push would spread the error more than the nub can take
            }

            var dir = toEnd.Normalized();
            hints.AddForbiddenZone(new SDInvertedRect(push.Origin + (from * dir), dir, 2.0f * LandTolerance, 0.0f, StripHalfWidth), DateTime.MaxValue);
            hints.GoalZones.Add(AIHints.GoalProximity(push.Origin + ((from + LandTolerance) * dir), 15.0f, 10.0f));
            stripEnd = end;
            return;
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        base.AddHints(slot, actor, hints);
        if (stripEnd is { } end) {
            hints.Add($"Guttler Glutter: stand 3-4y {(end == MoltenMetalBait.NorthEnd ? "north" : "south")} of Lauda, on her centre line");
        }
    }
}

sealed class Gyrocleave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Gyrocleave, new AOEShapeRect(80.0f, 10.0f)) {
    private readonly OverpoweringPointFake? overpoweringPointFake = module.FindComponent<OverpoweringPointFake>();
    private readonly Rush? rush = module.FindComponent<Rush>();

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (overpoweringPointFake == null || overpoweringPointFake.aoes.Count > 0) {
            return;
        }

        base.AddHints(slot, actor, hints);
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) {
        if (rush == null || rush.aoes.Count > 7) {
            return [];
        }

        return base.ActiveAOEs(slot, actor);
    }
}

sealed class LaudaTheSpellcleaverStates : StateMachineBuilder {
    public LaudaTheSpellcleaverStates(ModuleBase module) : base(module) {
        TrivialPhase()
            .ActivateOnEnter<BeastlyAura>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<DeadlyDemesne>()
            .ActivateOnEnter<Thunderbolt>()
            .ActivateOnEnter<OverpoweringPointFake>()
            .ActivateOnEnter<OverpoweringPoint>()
            .ActivateOnEnter<OverpoweringPointAim>()
            .ActivateOnEnter<Gyrocleave>()
            .ActivateOnEnter<MoltenMetalBait>()
            .ActivateOnEnter<MoltenMetalBaitAOE>()
            .ActivateOnEnter<BeastlyFlare>()
            .ActivateOnEnter<GluttonousGutting>()
            .ActivateOnEnter<GluttonousGoring>()
            .ActivateOnEnter<MagicalCombustion>()
            .ActivateOnEnter<UnseenForce>()
            .ActivateOnEnter<SearingAxe>()
            .ActivateOnEnter<GuttlerGlutter>()
            .ActivateOnEnter<AutoAttack>();
    }
}

[ModuleInfo(CFCID = 1092u, NameID = 14693u, PrimaryActorOID = (uint)OID.LaudaTheSpellcleaver, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class LaudaTheSpellcleaver: ModuleBase {
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i) {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch {
                (uint)OID.MagickedBlade => 3,
                (uint)OID.IndefatigableBlade => 3,
                (uint)OID.ThanatosPiece => 2,
                (uint)OID.LaudaTheSpellcleaver => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc) {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.ThanatosPiece));
        Arena.Actors(Enemies((uint)OID.MagickedBlade));
        Arena.Actors(Enemies((uint)OID.IndefatigableBlade));
    }

    public LaudaTheSpellcleaver(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private LaudaTheSpellcleaver(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena() {
        var arena = new ArenaBoundsCustom([
            new Rectangle(new(520f, -420f), 10f, 20f), // Base map
            new Rectangle(new(520f, -397.5f), 2.5f, 2.5f), // Bottom of map
            new Rectangle(new(520f, -442.5f), 2.5f, 2.5f), // Bottom of map

            // Left side of map
            new Rectangle(new(508f, -412.5f), 2.5f, 2.5f),
            new Rectangle(new(508f, -422.5f), 2.5f, 2.5f),

            // Right side of map
            new Rectangle(new(532f, -417.5f), 2.5f, 2.5f),
            new Rectangle(new(532f, -427.5f), 2.5f, 2.5f),
        ]);

        return (arena.Center, arena);
    }
}
