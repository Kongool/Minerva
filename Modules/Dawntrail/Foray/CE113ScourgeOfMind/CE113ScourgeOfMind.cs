using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.CE113ScourgeOfMind;

// Occult Crescent Critical Engagement "Scourge of Mind". IDs/mechanics referenced from BossmodReborn's
// CE113ScourgeOfMind. NOTE: the boss is the Mysterious Mindflayer (0x46B5, R3.25) — NOT the Tentacle add
// (0x46B7, R7.2+), which the extractor picks because it's bigger AND casts more. Only BMR settles it.

public enum OID : uint
{
    Boss = 0x46B5,             // 'Mysterious Mindflayer' R3.25 — the real CE primary
    Tentacle = 0x46B7,         // R7.2-12, add — Wallop rects
    JestingJackanapes = 0x46B6, // imps — Fire/Blizzard traps
    Helper = 0x233C,
}

public enum AID : uint
{
    DarkII = 41170,        // Boss->self, 6s, range 65 90-degree cone
    VoidThunderIII = 41172, // Boss->player, 5s, tankbuster

    // raidwides: boss self-cast is the visual, Helper cast is the real damage
    MindBlastVisual = 41167, MindBlast = 41166,
    ArcaneBlastVisual = 41171, ArcaneBlast = 41174,

    Wallop1 = 41257, // Tentacle->self, range 60 width 20 rect
    Wallop2 = 41314, // Tentacle->self, range 60 width 10 rect
    Wallop3 = 41256, // Tentacle->self, range 60 width 10 rect

    FireTrap = 41250,      // JestingJackanapes->self, 4s, range 8 circle
    BlizzardTrap = 41251,  // JestingJackanapes->self, 4s, range 8 circle
    SurpriseAttack = 41254, // Helper->location, 12s, width 6 rect charge
}

sealed class DarkII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkII, new AOEShapeCone(65f, 45f.Degrees()));
sealed class WallopWide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Wallop1, new AOEShapeRect(60f, 10f));
sealed class WallopNarrow2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Wallop2, new AOEShapeRect(60f, 5f));
sealed class WallopNarrow3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Wallop3, new AOEShapeRect(60f, 5f));
sealed class FireTrap(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireTrap, new AOEShapeCircle(8f));
sealed class BlizzardTrap(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlizzardTrap, new AOEShapeCircle(8f));
sealed class VoidThunderIII(ModuleBase module) : Components.CastHint(module, (uint)AID.VoidThunderIII, "Void Thunder III: tankbuster");

sealed class Rages(ModuleBase module) : Components.RaidwideCasts(module,
    [(uint)AID.MindBlastVisual, (uint)AID.MindBlast, (uint)AID.ArcaneBlastVisual, (uint)AID.ArcaneBlast], "Raidwide (mitigate)");

/// <summary>Surprise Attack: a charge from the caster to a marked location — a rect covering the path.</summary>
sealed class SurpriseAttack(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID != (uint)AID.SurpriseAttack)
            return;
        var to = cast.LocXZ != default ? cast.LocXZ : caster.Position;
        var delta = to - caster.Position;
        var len = delta.Length();
        if (len > 0.1f)
            this.aoes.Add(new AOEInstance(new AOEShapeRect(len, 3f), caster.Position, Angle.FromDirection(delta.Normalized()), this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == (uint)AID.SurpriseAttack)
            this.aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
    }
}

sealed class CE113ScourgeOfMindStates : StateMachineBuilder
{
    public CE113ScourgeOfMindStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<DarkII>()
            .ActivateOnEnter<Rages>()
            .ActivateOnEnter<VoidThunderIII>()
            .ActivateOnEnter<WallopWide>()
            .ActivateOnEnter<WallopNarrow2>()
            .ActivateOnEnter<WallopNarrow3>()
            .ActivateOnEnter<FireTrap>()
            .ActivateOnEnter<BlizzardTrap>()
            .ActivateOnEnter<SurpriseAttack>();
    }
}

[ModuleInfo(CFCID = 1018u, NameID = 33u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (ids from BMR CE113)")]
public sealed class CE113ScourgeOfMind(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(300f, 730f), new ArenaBoundsCircle(29.5f));
