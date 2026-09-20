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

sealed class RevealMainBoss(ModuleBase module) : ModuleComponent(module) {
    private bool reveal => !Module.PrimaryActor.IsTargetable && Module.Enemies((uint)OID.LightSprite).Any(actor => !actor.IsDead);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        if (!reveal) {
            return;
        }

        var lightSprites = Module.Enemies((uint)OID.LightSprite).FirstOrDefault(sprite => !sprite.IsDead);
        if (lightSprites == null) {
            return;
        }

        var spriteToBoss = lightSprites.Position - Module.PrimaryActor.Position;
        hints.GoalZones.Add(AIHints.GoalSingleTarget(lightSprites.Position + 2.0f * spriteToBoss.Normalized(), 1.0f, 2.0f));
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
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i) {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch {
                (uint)OID.BombPiece => 4,
                (uint)OID.DeepeyePiece => 3,
                (uint)OID.LightSprite => 2,
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
