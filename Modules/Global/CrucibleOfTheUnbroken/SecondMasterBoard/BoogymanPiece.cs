// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondMasterBoard.BoogymanPiece;

public enum OID : uint {
    BoogymanPiece = 0x4CE2,
    Helper = 0x233C,
    BombPiece = 0x4CE4, // R0.900, x0 (spawn during fight)
    DeepeyePiece = 0x4CE3, // R1.200, x0 (spawn during fight)
    LightSprite = 0x4CE5, // R1.200, x0 (spawn during fight)
}

public enum AID : uint {
    AutoAttack = 49217, // BoogymanPiece->player, no cast, single-target
    AutoAttack2 = 49218, // BoogymanPiece->player, no cast, single-target
    Teleport = 49229, // BoogymanPiece->location, no cast, single-target
    VeilOfDarkness = 49209, // BoogymanPiece->self, 4.0s cast, range 50 circle
    FeralLeap = 49230, // BoogymanPiece->player, no cast, single-target
    SwingRoundKnockbackBoss = 49225, // BoogymanPiece->self, 4.0+1.0s cast, single-target
    SwingRoundKnockback = 49226, // Helper->self, 5.0s cast, range 40 circle
    SwingRoundCircleBoss = 49227, // BoogymanPiece->self, 4.0+1.0s cast, single-target
    SwingRoundCircle = 49228, // Helper->self, 5.0s cast, range 12 circle
    RipplesOfGloom = 49231, // BoogymanPiece->self, 5.0s cast, range 50 circle
    ProvisionClub = 49216, // BoogymanPiece->self, 3.0s cast, single-target
    SwoopClub = 49219, // BoogymanPiece->location, 3.0s cast, width 4 rect charge
    SpinningStrikeBoos = 49221, // BoogymanPiece->self, 1.0+1.0s cast, single-target
    SpinningStrike = 49222, // Helper->self, 2.0s cast, range 30 circle
    ProvisionSword = 49215, // BoogymanPiece->self, 3.0s cast, single-target
    SwoopSword = 49220, // BoogymanPiece->location, 3.0s cast, width 4 rect charge
    ClearoutBoss = 49223, // BoogymanPiece->self, 1.0+0.5s cast, single-target
    Clearout = 49224, // Helper->self, 1.5s cast, range 40 120.000-degree cone

    // BombPiece
    Explosion = 49210, // 4CE4->self, 5.0s cast, range 9 circle
    SelfDestruct = 49211, // 4CE4->self, 8.0s cast, range 50 circle

    // DeepeyePiece
    AutoAttackDeepeyePiece = 49682, // 4CE3->player, no cast, single-target
    Oogle = 49214, // 4CE3->self, 10.0s cast, range 40 circle

    // LightSprite
    AutoAttackBanish = 49213, // 4CE5->player, 1.5s cast, single-target
    DiffuseLight = 49212, // 4CE5->self, 1.5s cast, range 20 45.000-degree cone
}

public enum SID : uint {
    SlashingResistanceDown = 3130, // BoogymanPiece->player, extra=0x0
    BluntResistanceDown = 3132, // BoogymanPiece->player, extra=0x0
    Concealed = 1621, // none->BoogymanPiece, extra=0x3
    Concealed1 = 3997, // none->4CE4/4CE3, extra=0xA
    Invisible = 616, // BoogymanPiece->BoogymanPiece, extra=0x0
    Blind = 5381, // BoogymanPiece->player, extra=0x0
    Bleeding = 3077, // none->player, extra=0x0
    Gen = 2659, // BoogymanPiece->BoogymanPiece, extra=0xBC5
}

sealed class RipplesOfGloom(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RipplesOfGloom, "Raidwide + Applies blind");
sealed class SwingRound(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SwingRoundCircle, 12.0f);
sealed class SwingRoundKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.SwingRoundKnockback, 20.0f) {
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (Casters.Count == 0) {
            return;
        }

        var knockback = Casters[0];
        var activation = Casters[0].Activation;

        if (IsImmune(slot, activation)) {
            return;
        }

        hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(Module.Center, knockback.Origin, 20.0f, 19.0f), activation);
    }
}

sealed class BombExplosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, 9.0f);
sealed class Oogle(ModuleBase module) : Components.CastGaze(module, (uint)AID.Oogle);
sealed class DiffuseLight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DiffuseLight, new AOEShapeCone(20.0f, 22.5f.Degrees()));

sealed class SwoopCharge : Components.SimpleChargeAOEGroups {
    public SwoopCharge(ModuleBase module) : base(module, [(uint)AID.SwoopClub, (uint)AID.SwoopSword], 2.0f) {
        Color = Colors.Danger;
    }
}
sealed class Swoop(ModuleBase module) : Components.GenericAOEs(module) {
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCircle circle = new(30.0f);
    private readonly AOEShapeCone cone = new(40.0f, 60.0f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell) {
        if (spell.Action.ID == (uint)AID.SwoopClub) {
            var direction = Angle.FromDirection(Module.Center - spell.LocXZ);
            aoes.Add(new(circle, spell.LocXZ, direction));
        }

        if (spell.Action.ID == (uint)AID.SwoopSword) {
            var direction = Angle.FromDirection(Module.Center - spell.LocXZ);
            aoes.Add(new(cone, spell.LocXZ, direction));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell) {
        if (spell.Action.ID is (uint)AID.SpinningStrike or (uint)AID.Clearout) {
            if (aoes.Count > 0) {
                aoes.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(aoes);
}

// Used to track where the adds are base on if they're dead or not - They go invisible, so we have to manually draw them like this
sealed class AddTrack(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.BombPiece, (uint)OID.DeepeyePiece]) {
    private new List<Actor> ActiveActors() {
        var enemies = Module.Enemies(OIDs);
        var result = new List<Actor>(enemies.Count);
        foreach (var actor in enemies) {
            if (!actor.IsDead) {
                result.Add(actor);
            }
        }
        return result;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        foreach (var a in ActiveActors()) {
            var priority = a.OID switch {
                (uint)OID.BombPiece => 4,
                (uint)OID.DeepeyePiece => 3,
                _ => 0
            };

            if (priority > 0) {
                hints.GoalZones.Add(AIHints.GoalSingleTarget(a.Position, 3.0f, priority));
            }
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc) {
        var actors = ActiveActors();

        foreach (var actor in actors) {
            Arena.Actor(actor, Colors.Enemy);
        }
    }
}

/// <summary>
/// Veil of Darkness hides Boogyman (untargetable, Concealed) until a dying Light Sprite's Diffuse Light touches it.
/// The light is a 20y cone, 22.5 each side, along the sprite's facing when it dies, and a sprite faces its target: the
/// player, or the familiar, which holds a sprite's attention from its spawn until the player has hit it a while.
/// While hidden, the boss teleports 16y out from the centre every ~9.3s and Feral Leaps onto the player 4.1s later.
///
/// <para>On 2026-11-03 (18:59) it stayed hidden 59s, from 53.0 to 112.3: all three sprites of the first wave missed,
/// and the second wave came 37s later. Of the nine sprite deaths in both pulls, the three that faced the player with
/// the boss on that side revealed it; two faced the player from the wrong side (145 and 41 degrees off); four faced the
/// familiar. BossmodReborn's goal here sent the player to the far side of the sprite from the boss.</para>
///
/// <para>So one sprite is picked to be finished: one already facing the player if any, else the one being worked on,
/// else the nearest. It is raised above the other sprites (the module's priorities, which the rotation plugin follows),
/// and the player is kept on the boss's side of it, inside a cone narrower than the light, so its light covers the
/// boss when it dies facing them. A sprite facing the familiar still shines where the familiar stands. This is advice:
/// real danger still wins.</para>
/// </summary>
sealed class RevealMainBoss(ModuleBase module) : ModuleComponent(module) {
    // a little inside the light's 22.5, for the boss's own steps and the player's
    private static readonly Angle StandWithin = 20.0f.Degrees();
    private const float StandRange = 40.0f;
    public Actor? Focus { get; private set; }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        var boss = Module.PrimaryActor;
        Actor? nearest = null, facingPlayer = null, targeted = null;
        var keep = false;
        var lightPending = false;
        if (!boss.IsTargetable && !boss.IsDeadOrDestroyed) {
            foreach (var sprite in Module.Enemies((uint)OID.LightSprite)) {
                lightPending |= sprite.CastInfo?.Action.ID == (uint)AID.DiffuseLight;
                if (sprite.IsDead) {
                    continue;
                }

                keep |= sprite == Focus;
                if (sprite.InstanceID == actor.TargetID) {
                    targeted = sprite;
                }

                var d = (sprite.Position - actor.Position).LengthSq();
                if (nearest == null || d < (nearest.Position - actor.Position).LengthSq()) {
                    nearest = sprite;
                }

                if (sprite.TargetID == actor.InstanceID
                    && (facingPlayer == null || d < (facingPlayer.Position - actor.Position).LengthSq())) {
                    facingPlayer = sprite;
                }
            }
        }

        // the rotation plugin hard-targets the sprite raised for it, so the player's target is normally this one
        Focus = facingPlayer ?? targeted ?? (keep ? Focus : nearest);
        // A dead sprite's light is already aimed; walking to the next sprite now crosses it (dodgesim, 18:59 pull).
        if (Focus == null || lightPending) {
            return;
        }

        // everything outside the cone from the sprite toward the boss is forbidden, at MaxValue: a stand-here
        // instruction (AIHints.PositioningZones)
        hints.AddForbiddenZone(new SDInvertedCone(Focus.Position, StandRange, Angle.FromDirection(boss.Position - Focus.Position), StandWithin),
            DateTime.MaxValue);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints) {
        if (Focus != null) {
            hints.Add("Kill the marked Light Sprite from Boogyman's side, so its light points at him");
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc) {
        if (Focus != null) {
            Arena.AddCircle(Focus.Position, Focus.HitboxRadius + 0.5f, Colors.Safe);
        }
    }
}

sealed class BoogymanPieceStates : StateMachineBuilder {
    public BoogymanPieceStates(ModuleBase module) : base(module) {
        TrivialPhase()
            .ActivateOnEnter<RipplesOfGloom>()
            .ActivateOnEnter<SwingRound>()
            .ActivateOnEnter<SwingRoundKnockback>()
            .ActivateOnEnter<BombExplosion>()
            .ActivateOnEnter<Oogle>()
            .ActivateOnEnter<DiffuseLight>()
            .ActivateOnEnter<SwoopCharge>()
            .ActivateOnEnter<Swoop>()
            .ActivateOnEnter<AddTrack>()
            .ActivateOnEnter<RevealMainBoss>();
    }
}

[ModuleInfo(CFCID = 1092u, NameID = 14638u, PrimaryActorOID = (uint)OID.BoogymanPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class BoogymanPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, -420f), new ArenaBoundsCircle(20f)) {
    private RevealMainBoss? reveal;

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        // the sprite RevealMainBoss lined the player up for is finished before the other sprites
        var focus = (reveal ??= FindComponent<RevealMainBoss>())?.Focus;
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i) {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch {
                (uint)OID.BombPiece => 4,
                (uint)OID.DeepeyePiece => 3,
                (uint)OID.LightSprite => e.Actor == focus ? 3 : 2,
                (uint)OID.BoogymanPiece => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc) {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.LightSprite));
    }

    private readonly string[] _prePullHints = [
        "Fight kill priority: BombPiece -> DeepeyePiece -> LightSprite -> boss",
        "Sprite: Will cast a cone toward where it's facing when it dies, aim it toward the boss to reveal it"
    ];

    public override string[] PrePullHints => _prePullHints;
}
