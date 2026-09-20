// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondMasterBoard.MindFlayerPiece;

public enum OID : uint {
    MindflayerPiece = 0x4CDD,
    Helper = 0x233C,
    MyconidPiece = 0x4CDE, // R0.600, x0 (spawn during fight)
    WaterPuddle = 0x1E9998, // R0.500, x0 (spawn during fight), EventObj type
    ShroombedPuddle = 0x4CDF, // R6.000, x0 (spawn during fight)
    ArcaneSphere = 0x4CE1, // R1.000-1.860, x0 (spawn during fight)
}

public enum AID : uint {
    AutoAttackThunder = 48623, // MindflayerPiece->player, no cast, single-target
    VoidWaterIIIBoss = 49199, // MindflayerPiece->self, 5.0s cast, single-target
    VoidWaterIII = 49200, // Helper->players, no cast, range 8 circle
    VoidThunderIIIBoss = 49205, // MindflayerPiece->self, 5.0s cast, single-target
    VoidThunderIIITB = 49206, // Helper->player, no cast, range 6 circle
    VoidThunderIIICross = 49207, // Helper->self, 4.0s cast, range 50 width 10 cross
    VoidThunderIIICross1 = 50939, // Helper->self, 4.0s cast, range 50 width 10 cross
    ArcaneUtterance = 49201, // MindflayerPiece->self, 5.0s cast, single-target
    ArcaneEnhancement = 49202, // MindflayerPiece->self, 5.0s cast, single-target
    DarkCurrentSmall = 49203, // 4CE1->self, 2.0s cast, range 100 width 4 rect
    DarkCurrentBig = 49204, // 4CE1->self, 2.0s cast, range 100 width 10 rect
    VoidParalyzeIII = 49208, // MindflayerPiece->self, 7.0s cast, range 60 circle

    // MyconidPiece
    AutoAttackMyconidPiece = 49682, // 4CDE->player, no cast, single-target
    SporeSpill = 49196, // Helper->self, 1.0s cast, range 6 circle

    Unknown = 49197, // 4CDF->self, no cast, range 6 circle - most likely spawning the puddle
}

public enum SID : uint {
    WaterResistanceDown = 5021, // Helper->player, extra=0x1/0x2
    LightningResistanceDownII = 4456, // none->player, extra=0x0
    SustainedDamage = 3795, // none->4CE1, extra=0x1
    Paralysis = 5382, // MindflayerPiece->player, extra=0x0
}

public enum IconID : uint {
    VoidWaterIIIIcon = 135, // player/4A04/4A15->self
    VoidThunderIIITankBuster = 344, // player->self
}

public enum TetherID : uint {
    ArcaneEnhancementTether = 426, // 4CE1->MindflayerPiece
}

sealed class VoidThunderIII(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.VoidThunderIIITankBuster, (uint)AID.VoidThunderIIITB, 6.0f, 5.1f);
sealed class VoidThunderIIICross(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidThunderIIICross, new AOEShapeCross(50.0f, 5.0f));
sealed class VoidParalyzeIII(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.VoidParalyzeIII, "Raidwide + Applies Paralysis");

sealed class VoidWaterIII(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.VoidWaterIIIIcon, (uint)AID.VoidWaterIII, 8.0f, 5.1f) {
    // TODO consider fixing the actual problem, since the other spread is a pet its not include in the Raid.WithSlot
    //  Function is copied for now, until fixed - could also just remove most of the code since we only need to consider spreads - no stacks/damage hints
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (!EnableHints) {
            return;
        }

        var spreads = CollectionsMarshal.AsSpan(ActiveSpreads);
        var stacks = CollectionsMarshal.AsSpan(ActiveStacks);
        var lenSpreads = spreads.Length;
        var lenStacks = stacks.Length;

        // nothing to do
        if (lenStacks == 0 && lenSpreads == 0) {
            return;
        }
        var isSpreadTarget = false;

        var partyWOS = Raid.WithSlot(includeDead: IncludeDeadTargets);
        var lenPWOS = partyWOS.Length;

        for (var i = 0; i < lenSpreads; ++i) {
            ref var s = ref spreads[i];
            if (!SpreadAppliesToArenaProjectionLayer(actor, s)) {
                continue;
            }
            var t = s.Target;
            if (t != actor || !SpreadParticipantAppliesToArenaProjectionLayer(actor, s)) {
                hints.AddForbiddenZone(new SDCircle(t.Position.Quantized(), s.Radius + ExtraAISpreadThreshold), s.Activation,
                    arenaProjectionLayer: ArenaProjectionLayerForAI(s.ResolveArenaProjectionLayer(Module), s.RestrictToArenaProjectionLayer));
            } else {
                isSpreadTarget = true;

                var radius = s.Radius;
                var act = s.Activation;
                for (var j = 0; j < lenPWOS; ++j) {
                    var p = partyWOS[j].Item2;
                    if (!SpreadParticipantAppliesToArenaProjectionLayer(p, s)) {
                        continue;
                    }

                    for (var k = 0; k < lenSpreads; ++k) {
                        if (SpreadAppliesToArenaProjectionLayer(actor, spreads[k]) && spreads[k].Target == p
                            && SpreadParticipantAppliesToArenaProjectionLayer(p, spreads[k])) {
                            goto done; // no need to add avoid hints for players who are also spread targets
                        }
                    }

                    hints.AddForbiddenZone(new SDCircle(p.Position.Quantized(), radius + ExtraAISpreadThreshold), act,
                        arenaProjectionLayer: ArenaProjectionLayerForAI(s.ResolveArenaProjectionLayer(Module), s.RestrictToArenaProjectionLayer));
                done:
                    ;
                }
            }
        }

        var isStackTarget = false;

        for (var i = 0; i < lenStacks; ++i) {
            ref var s = ref stacks[i];
            if (!StackParticipantAppliesToArenaProjectionLayer(actor, s)) {
                continue;
            }
            var t = s.Target;
            if (s.Target == actor) {
                isStackTarget = true;

                var stacksIFzTarget = new List<ShapeDistance>(lenPWOS - 1);
                var radius = s.Radius;

                for (var j = 0; j < lenPWOS; ++j) { // if player got stackmarker we should try finding a good candidate to stack with
                    ref var p = ref partyWOS[j];
                    var a = p.Item2;
                    if (t != a && StackParticipantAppliesToArenaProjectionLayer(a, s)) {
                        if (s.ForbiddenPlayers[p.Item1]) { // party member is forbidden from stacking
                            continue;
                        }

                        for (var k = 0; k < lenSpreads; ++k) {
                            if (SpreadAppliesToArenaProjectionLayer(actor, spreads[k]) && spreads[k].Target == a
                                && SpreadParticipantAppliesToArenaProjectionLayer(a, spreads[k])) {
                                goto skip; // player got a spread marker
                            }
                        }

                        for (var k = 0; k < lenStacks; ++k) {
                            if (StackAppliesToArenaProjectionLayer(actor, stacks[k]) && stacks[k].Target == a
                                && StackParticipantAppliesToArenaProjectionLayer(a, stacks[k])) {
                                goto skip; // player got a stack marker and we don't want to stack stacks
                            }
                        }
                        // buddy is not target of stacks or spreads, so a good candidate
                        stacksIFzTarget.Add(new SDInvertedCircle(a.Position, radius * 0.5f));
                    skip:
                        ;
                    }
                }

                if (stacksIFzTarget.Count > 0) {
                    hints.AddForbiddenZone(new SDIntersection([.. stacksIFzTarget]), s.Activation,
                        arenaProjectionLayer: ArenaProjectionLayerForAI(s.ResolveArenaProjectionLayer(Module), s.RestrictToArenaProjectionLayer));
                }
            }
        }

        var stacksIFz = new List<ShapeDistance>();
        var stacksIFzLayer = -2; // -2 = empty, -1 = unlayered/mixed
        var stacksIFzActivation = DateTime.MaxValue;
        for (var i = 0; i < lenStacks; ++i) {
            ref var s = ref stacks[i];
            if (!StackParticipantAppliesToArenaProjectionLayer(actor, s)) {
                continue;
            }
            var t = s.Target;
            var targetPos = t.Position.Quantized();
            var act = s.Activation;
            var radius = s.Radius;

            if (s.Target != actor) {
                if (s.ForbiddenPlayers[slot]) {
                    goto addfz;
                }
                var numInside = s.NumInside(Module);
                var isInside = s.IsInside(actor);
                var max = s.MaxSize;
                if (!isSpreadTarget && (!isInside && numInside < max || isInside && numInside <= max)) { // don't try to stack if spread target
                    stacksIFz.Add(new SDInvertedCircle(targetPos, radius));
                    var layer = ArenaProjectionLayerForAI(s.ResolveArenaProjectionLayer(Module), s.RestrictToArenaProjectionLayer) ?? -1;
                    stacksIFzLayer = stacksIFzLayer == -2 || stacksIFzLayer == layer ? layer : -1;
                    stacksIFzActivation = stacksIFzActivation < act ? stacksIFzActivation : act;
                    continue;
                }
            addfz:
                // avoid stack if forbidden or enough players inside
                // double radius if stack target to prevent standing next to other stack markers or overlapping them
                hints.AddForbiddenZone(new SDCircle(targetPos, !isStackTarget ? radius : 2f * radius), act,
                    arenaProjectionLayer: ArenaProjectionLayerForAI(s.ResolveArenaProjectionLayer(Module), s.RestrictToArenaProjectionLayer));
            }
        }

        var countIFz = stacksIFz.Count;
        if (countIFz > 0) {
            if (countIFz == 1) {
                hints.AddForbiddenZone(stacksIFz[0], stacksIFzActivation,
                    arenaProjectionLayer: stacksIFzLayer >= 0 ? stacksIFzLayer : null);
            } else {
                hints.AddForbiddenZone(new SDOutsideOfUnion([.. stacksIFz]), stacksIFzActivation,
                    arenaProjectionLayer: stacksIFzLayer >= 0 ? stacksIFzLayer : null);
            }
        }

        if (RaidwideOnResolve) {
            BitMask spreadMask = default;
            var firstSpreadActivation = DateTime.MaxValue;
            for (var i = 0; i < lenSpreads; ++i) {
                ref var s = ref spreads[i];
                if (SpreadAppliesToArenaProjectionLayer(actor, s) && SpreadParticipantAppliesToArenaProjectionLayer(s.Target, s)) {
                    spreadMask.Set(Raid.FindSlot(s.Target.InstanceID));
                    firstSpreadActivation = firstSpreadActivation < s.Activation ? firstSpreadActivation : s.Activation;
                }
            }

            if (spreadMask != default) {
                hints.AddPredictedDamage(spreadMask, firstSpreadActivation);
            }

            BitMask stackMask = default;
            var firstStackActivation = DateTime.MaxValue;
            var participants = Raid.WithSlot(includeDead: IncludeDeadTargets);
            for (var i = 0; i < lenStacks; ++i) {
                ref var s = ref stacks[i];
                if (StackAppliesToArenaProjectionLayer(actor, s)) {
                    for (var j = 0; j < participants.Length; ++j) {
                        ref var participant = ref participants[j];
                        if (!s.ForbiddenPlayers[participant.Item1] && StackParticipantAppliesToArenaProjectionLayer(participant.Item2, s)) {
                            stackMask.Set(participant.Item1);
                        }
                    }
                    firstStackActivation = firstStackActivation < s.Activation ? firstStackActivation : s.Activation;
                }
            }

            if (stackMask != default) {
                hints.AddPredictedDamage(stackMask, firstStackActivation);
            }
        }
    }
}

sealed class WaterPuddles : Components.PersistentInvertibleVoidzone {
    public WaterPuddles(ModuleBase module) : base(module, 8.0f, GetVoidzones) {
        InvertResolveAt = World.CurrentTime;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (!Sources(Module).Any()) {
            return;
        }

        base.AddHints(slot, actor, hints);
    }

    public static Actor[] GetVoidzones(ModuleBase module) {
        var enemies = module.Enemies((uint)OID.WaterPuddle);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i) {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

sealed class SporeSpill(ModuleBase module) : Components.GenericAOEs(module) {
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCircle shape = new(6.0f);

    public override void OnActorDeath(Actor actor) {
        if (actor.OID == (uint)OID.MyconidPiece) {
            aoes.Add(new(shape, actor.Position, actor.Rotation));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID == (uint)AID.SporeSpill) {
            if (aoes.Count > 0) {
                aoes.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);
}

sealed class ShroombedPuddles(ModuleBase module) : Components.Voidzone(module, 6.0f, GetVoidzones) {
    private static Actor[] GetVoidzones(ModuleBase module) {
        var enemies = module.Enemies((uint)OID.ShroombedPuddle);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i) {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

sealed class ArcaneEnhancement(ModuleBase module) : Components.GenericAOEs(module) {
    private readonly List<(Actor actor, bool isTethered)> spheres = [];
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeRect smallRect = new(100.0f, 2.0f, 100.0f);
    private readonly AOEShapeRect bigRect = new(100.0f, 5.0f, 100.0f);
    private bool active = false; // Used to wait showing the aoes until the tethers are sent out

    public override void OnActorCreated(Actor actor) {
        if (actor.OID == (uint)OID.ArcaneSphere) {
            spheres.Add((actor, false));
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether) {
        if (tether.ID == (uint)TetherID.ArcaneEnhancementTether) {
            var sphereIndex = spheres.FindIndex(sphere => sphere.actor == source);
            if (sphereIndex < 0) {
                return;
            }

            spheres[sphereIndex] = (spheres[sphereIndex].actor, true);
            active = true;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID is (uint)AID.DarkCurrentSmall or (uint)AID.DarkCurrentBig) {
            if (spheres.Count > 0) {
                var sphereIndex = spheres.FindIndex(sphere => sphere.actor == caster);
                if (sphereIndex < 0) {
                    return;
                }

                spheres.RemoveAt(sphereIndex);

                if (spheres.Count == 0) {
                    active = false;
                }
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) {
        if (!active) {
            return [];
        }

        aoes.Clear();

        var count = spheres.Count;
        if (count == 0) {
            return [];
        }

        foreach (var sphere in spheres) {
            if (sphere.isTethered) {
                aoes.Add(new(bigRect, sphere.actor.Position, sphere.actor.Rotation, actorID: sphere.actor.InstanceID));
            }

            if (!sphere.isTethered) {
                aoes.Add(new(smallRect, sphere.actor.Position, sphere.actor.Rotation, actorID: sphere.actor.InstanceID));
            }
        }

        return CollectionsMarshal.AsSpan(aoes);
    }
}

sealed class MindflayerPieceStates : StateMachineBuilder {
    public MindflayerPieceStates(ModuleBase module) : base(module) {
        TrivialPhase()
            .ActivateOnEnter<VoidWaterIII>()
            .ActivateOnEnter<VoidThunderIII>()
            .ActivateOnEnter<VoidThunderIIICross>()
            .ActivateOnEnter<VoidParalyzeIII>()
            .ActivateOnEnter<WaterPuddles>()
            .ActivateOnEnter<ShroombedPuddles>()
            .ActivateOnEnter<ArcaneEnhancement>()
            .ActivateOnEnter<SporeSpill>();
    }
}

[ModuleInfo(CFCID = 1092u, NameID = 14633u, PrimaryActorOID = (uint)OID.MindflayerPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class MindflayerPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, 0f), new ArenaBoundsRect(20f, 20f)) {
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        var puddles = WaterPuddles.GetVoidzones(this);

        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i) {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch {
                (uint)OID.MyconidPiece => insidePuddle(e.Actor, puddles) ? 2 : AIHints.Enemy.PriorityForbidden,
                (uint)OID.MindflayerPiece => 1,
                _ => 0
            };
        }
    }

    private static bool insidePuddle(Actor actor, Actor[] puddles) {
        foreach (var puddle in puddles) {
            if (actor.Position.InCircle(puddle.Position, 6.0f)) {
                return true;
            }
        }

        return false;
    }

    protected override void DrawEnemies(int pcSlot, Actor pc) {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.MyconidPiece), Colors.Vulnerable);
    }

    private readonly string[] _prePullHints = [
        "Kill the MyconidPiece inside the water puddles to solve the mechanic correctly"
    ];

    public override string[] PrePullHints => _prePullHints;
}
