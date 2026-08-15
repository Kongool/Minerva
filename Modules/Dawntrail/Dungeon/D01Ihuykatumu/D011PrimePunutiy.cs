using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D01Ihuykatumu.D011PrimePunutiy;

// Minerva's first encounter module. IDs are real (ported from BossmodReborn) so it activates and
// draws in the live duty. It covers the straightforward cast-bar mechanics with the components
// built in Phase 3; tether-bait / spread / concentric mechanics wait on those components (TODO).

public enum OID : uint
{
    Boss = 0x4190,
    ProdigiousPunutiy = 0x4191,
    Punutiy = 0x4192,
    PetitPunutiy = 0x4193,
    IhuykatumuFlytrap = 0x4194,
    Helper = 0x233C,
}

public enum AID : uint
{
    PunutiyPress = 36492, // Boss->self, 5.0s cast, range 60 circle (raidwide)
    Hydrowave = 36493,    // Boss->self, 4.0s cast, range 60 30-degree cone
    Resurface = 36494,    // Boss->self, 5.0s cast, range 100 60-degree cone

    Bury1 = 36497, // Helper->self, range 12 circle
    Bury2 = 36500, // Helper->self, range 35 width 10 rect
    Bury3 = 36498, // Helper->self, range 8 circle
    Bury4 = 36501, // Helper->self, range 4 circle
    Bury5 = 36499, // Helper->self, range 25 width 6 rect
    Bury6 = 36502, // Helper->self, range 6 circle
    Bury7 = 36503, // Helper->self, range 25 width 6 rect
    Bury8 = 36504, // Helper->self, range 35 width 10 rect
    Decay = 36505, // IhuykatumuFlytrap->self, range 6-40 donut
}

sealed class PunutiyPress(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.PunutiyPress, "Raidwide (mitigate)");
sealed class Hydrowave(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hydrowave, new AOEShapeCone(60f, 15f.Degrees()));
sealed class Resurface(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Resurface, new AOEShapeCone(100f, 30f.Degrees()));

/// <summary>Bury/Decay: several helper casts sharing a family of shapes, keyed by action id.</summary>
sealed class BuryDecay(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        AOEShape? shape = cast.Action.ID switch
        {
            (uint)AID.Bury1 => new AOEShapeCircle(12f),
            (uint)AID.Bury3 => new AOEShapeCircle(8f),
            (uint)AID.Bury4 => new AOEShapeCircle(4f),
            (uint)AID.Bury6 => new AOEShapeCircle(6f),
            (uint)AID.Bury2 or (uint)AID.Bury8 => new AOEShapeRect(35f, 5f),
            (uint)AID.Bury5 or (uint)AID.Bury7 => new AOEShapeRect(25f, 3f),
            (uint)AID.Decay => new AOEShapeDonut(6f, 40f),
            _ => null,
        };
        if (shape != null)
            this.aoes.Add(new AOEInstance(shape, cast.LocXZ, cast.Rotation, this.Module.CastFinishAt(cast), actorID: caster.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (this.aoes.Count > 0)
            this.aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
    }
}

sealed class D011PrimePunutiyStates : StateMachineBuilder
{
    public D011PrimePunutiyStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<PunutiyPress>()
            .ActivateOnEnter<Hydrowave>()
            .ActivateOnEnter<Resurface>()
            .ActivateOnEnter<BuryDecay>();
    }
}

[ModuleInfo(CFCID = 826u, NameID = 12723u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (ids from BMR)")]
public sealed class D011PrimePunutiy(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(35f, -95f), new ArenaBoundsSquare(19.5f))
{
    private static readonly uint[] Adds = [(uint)OID.ProdigiousPunutiy, (uint)OID.Punutiy, (uint)OID.PetitPunutiy];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        if (!this.PrimaryActor.IsDeadOrDestroyed)
            this.Arena.ActorMarker(this.PrimaryActor.Position, this.PrimaryActor.Rotation, this.PrimaryActor.HitboxRadius, Colors.Enemy);

        foreach (var oid in Adds)
            foreach (var add in this.Enemies(oid))
                if (!add.IsDeadOrDestroyed)
                    this.Arena.ActorMarker(add.Position, add.Rotation, add.HitboxRadius, Colors.Object);
    }
}
