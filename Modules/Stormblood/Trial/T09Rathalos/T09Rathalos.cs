// Ported from Veyn's bossmod (awgil/ffxiv_bossmod, BSD-3; see THIRD-PARTY-NOTICES.txt). BossmodReborn has no
// module for this fight, so this one comes from the original project; numbered T09 because Minerva's Stormblood
// trials follow BossmodReborn's numbering, where T05 is already Yojimbo.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T09Rathalos;

public enum OID : uint
{
    Boss = 0x2129, // R5.460, x?
    Helper = 0x18D6, // R0.500, x?, mixed types
    WyvernsTail = 0x23C0, // R3.900, x?, Part type
    SteppeSheep = 0x212B, // R0.700, x?
    SteppeYamaa = 0x212C, // R1.920, x?
    SteppeYamaa1 = 0x212D, // R1.920, x?
    SteppeCoeurl = 0x212E, // R3.150, x?
    Garula = 0x212A, // R4.000, x?
    Fireball = 0x1E9927
}

public enum AID : uint
{
    Roar = 11460, // Boss->self, no cast, range 50+R circle
    MangleVisual = 10346, // Boss->self, 2.5s cast, range 10 120-degree cone
    Mangle = 11449, // Helper->self, no cast, range 10 120-degree cone
    TailSmash = 10347, // Helper->self, no cast, range 11 ?-degree cone
    RushVisual1 = 10349, // Boss->location, 2.0s cast, width 9 rect charge
    Rush1 = 11452, // Helper->location, no cast, width 9 rect charge
    TailSwingVisual = 10348, // Boss->self, no cast, range 11 180-degree cone
    TailSwing = 11451, // Helper->self, no cast, range 11 180-degree cone
    Roar2 = 10356, // Boss->self, no cast, range 50+R circle
    KingOfTheSkiesVisual = 10357, // Boss->location, no cast, range 50 circle
    KingOfTheSkies = 11546, // Helper->location, no cast, range 50 circle
    SweepingFlamesVisual = 10361, // Boss->self, no cast, range 11 120-degree cone
    SweepingFlames = 11457, // Helper->self, no cast, range 11 120-degree cone
    Mangle2Visual = 10362, // Boss->self, 1.0s cast, range 9 90-degree cone
    Mangle2 = 11458, // Helper->self, no cast, range 9 90-degree cone
    RushVisual2 = 10360, // Boss->location, 1.5s cast, width 10 rect charge
    Rush2 = 11456, // Helper->location, no cast, width 9 rect charge
    FireballBossFirst = 10358, // Boss->player, 5.0s cast, range 5 circle
    FireballFirst = 11450, // Helper->player, no cast, range 5 circle
    VeniVidiVici = 21847, // Boss->location, no cast, width 10 rect charge

    GarulaRush = 10367, // Garula->Boss, 2.0s cast, width 8 rect charge, stuns boss
    CoeurlAuto = 870, // SteppeCoeurl->player/Boss, no cast, single-target
    MobAutos = 872, // SteppeSheep/SteppeYamaa/SteppeYamaa1/Garula->player/Boss, no cast, single-target
    Lullaby = 10363, // SteppeSheep->self, 3.0s cast, range 3+R circle
}

/// <summary>One predicted AOE at a time: the boss's visual announces it, the helper's real cast resolves it.</summary>
abstract class NextAOE(ModuleBase module, uint aid = default) : Components.GenericAOEs(module, aid)
{
    protected AOEInstance? Next;
    private readonly AOEInstance[] one = new AOEInstance[1];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (this.Next is not { } aoe)
            return [];
        this.one[0] = aoe;
        return this.one;
    }
}

class Mangle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MangleVisual, new AOEShapeCone(10f, 60.Degrees()));
class GarulaRush(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.GarulaRush, 4f);
class Lullaby(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Lullaby, new AOEShapeCircle(3.7f));

class Rush(ModuleBase module) : NextAOE(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID.RushVisual1 or AID.RushVisual2)
        {
            var delay = (AID)spell.Action.ID is AID.RushVisual1 ? 0.6d : 1.3d;
            var chargeDir = spell.LocXZ - caster.Position;
            this.Next = new(new AOEShapeRect(chargeDir.Length() + 6f, 4.5f), caster.Position, chargeDir.ToAngle(), this.Module.CastFinishAt(spell, delay));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.Rush1 or AID.Rush2)
            this.Next = null;
    }
}

class TailSwing(ModuleBase module) : NextAOE(module, (uint)AID.TailSwingVisual)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == this.WatchedAction)
            this.Next = new(new AOEShapeCone(11f, 90.Degrees()), caster.Position, spell.Rotation - 90.Degrees(), this.World.FutureTime(1.9f));
        if ((AID)spell.Action.ID == AID.TailSwing)
            this.Next = null;
    }
}

class SweepingFlames(ModuleBase module) : NextAOE(module, (uint)AID.SweepingFlamesVisual)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == this.WatchedAction)
            this.Next = new(new AOEShapeCone(11f, 60.Degrees()), caster.Position, spell.Rotation, this.World.FutureTime(1.5f));
        if ((AID)spell.Action.ID == AID.SweepingFlames)
            this.Next = null;
    }
}

class Mangle2(ModuleBase module) : NextAOE(module, (uint)AID.Mangle2Visual)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == this.WatchedAction)
            this.Next = new(new AOEShapeCone(9f, 45.Degrees()), caster.Position, spell.Rotation, this.Module.CastFinishAt(spell, 0.6d));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID == AID.Mangle2)
            this.Next = null;
    }
}

class FireballStack1(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.FireballBossFirst, 5f);
// the puddle is predicted where the helper's untelegraphed hit lands, then handed to the live fireball object
class FirePuddle(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 5f, (uint)AID.FireballFirst, m => m.Enemies((uint)OID.Fireball).Where(e => e.EventState != 7), 0.5d);

class KingOfTheSkies(ModuleBase module) : Components.GenericLineOfSightAOE(module, (uint)AID.KingOfTheSkiesVisual, 100f, false)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == this.WatchedAction)
            this.Modify(new WPos(100f, 82f), this.Module.Enemies((uint)OID.Garula).Select(e => (e.Position, e.HitboxRadius)), this.World.FutureTime(7f));
        if ((AID)spell.Action.ID == AID.KingOfTheSkies)
            this.Modify(null, []);
    }
}

class Adds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.SteppeYamaa, (uint)OID.SteppeYamaa1, (uint)OID.SteppeSheep, (uint)OID.SteppeCoeurl, (uint)OID.Garula]);

// while the tail is targetable it is the only thing worth hitting
class TargetHints(ModuleBase module) : ModuleComponent(module)
{
    private Actor? tail;

    public override void OnActorTargetable(Actor actor)
    {
        if (actor.OID == (uint)OID.WyvernsTail)
            this.tail = actor;
    }

    public override void OnActorUntargetable(Actor actor)
    {
        if (actor.OID == (uint)OID.WyvernsTail)
            this.tail = null;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // dropped from the port: the original queues the duty's Mega Potion below 25% HP (Minerva executes no actions)
        if (this.tail != null && !this.tail.IsDead)
        {
            hints.SetPriority(this.tail, 1);
            hints.SetPriority(this.Module.PrimaryActor, AIHints.Enemy.PriorityForbidden);
        }
    }
}

class T09RathalosStates : StateMachineBuilder
{
    public T09RathalosStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Mangle>()
            .ActivateOnEnter<GarulaRush>()
            .ActivateOnEnter<Lullaby>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<TailSwing>()
            .ActivateOnEnter<Mangle2>()
            .ActivateOnEnter<FireballStack1>()
            .ActivateOnEnter<FirePuddle>()
            .ActivateOnEnter<KingOfTheSkies>()
            .ActivateOnEnter<SweepingFlames>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<TargetHints>();
    }
}

[ModuleInfo(CFCID = 474u, NameID = 7221u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from Veyn's bossmod (awgil)")]
public class T09Rathalos(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsCircle(20f));
