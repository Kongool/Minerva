// Ported from Veyn's bossmod (awgil/ffxiv_bossmod, BSD-3; see THIRD-PARTY-NOTICES.txt). BossmodReborn has no
// module for this fight, so this one comes from the original project; numbered Ex9 because Minerva's Stormblood
// extremes follow BossmodReborn's numbering, which ends at Ex8 Seiryu.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex9Rathalos;

public enum OID : uint
{
    Boss = 0x212F, // R5.460, x1
    Helper = 0x18D6, // R1.300, x1, mixed
    WyvernsTail = 0x23D9, // R3.900, x1, Part type
    SteppeSheep = 0x2131, // R0.700, x0 (spawn during fight)
    SteppeYamaa = 0x2132, // R1.920, x0 (spawn during fight)
    SteppeYamaa1 = 0x2133, // R1.920, x0 (spawn during fight)
    SteppeCoeurl = 0x2134, // R3.150, x0 (spawn during fight)
    Garula = 0x2130, // R4.000, x0 (spawn during fight)
    Fireball = 0x1E9927
}

public enum AID : uint
{
    Roar1 = 11459, // Boss->self, no cast, range 50+R circle
    MangleVisual = 10323, // Boss->self, 2.5s cast, range 10 120-degree cone
    Mangle = 10332, // Helper->self, no cast, range 10 120-degree cone
    TailSmash = 10324, // Helper->self, no cast, range 11 ?-degree cone, used right after Mangle
    RushVisual1 = 10326, // Boss->location, 2.0s cast, width 9 rect charge
    Rush1 = 10813, // Helper->location, no cast, width 9 rect charge
    TailSwingVisual = 10325, // Boss->self, no cast, range 11 180-degree cone
    TailSwing = 10812, // Helper->self, no cast, range 11 180-degree cone
    Roar2 = 10333, // Boss->self, no cast, range 50+R circle, applies stun
    KingOfTheSkiesVisual = 10334, // Boss->location, no cast, range 50 circle
    KingOfTheSkies = 11545, // Helper->location, no cast, range 50 circle
    SweepingFlamesVisual = 10338, // Boss->self, no cast, range 11 120-degree cone
    SweepingFlames = 11446, // Helper->self, no cast, range 11 120-degree cone
    Mangle2Visual = 10339, // Boss->self, 0.7s cast, range 9 90-degree cone
    Mangle2 = 11447, // Helper->self, no cast, range 9 90-degree cone
    FireballBossFirst = 10335, // Boss->player, 5.0s cast, range 5 circle
    FireballFirst = 10336, // Helper->player, no cast, range 5 circle
    FireballBossRest = 11530, // Boss->player, 3.0s cast, range 5 circle
    FireballRest = 11531, // Helper->player, no cast, range 5 circle
    RushVisual2 = 10337, // Boss->location, 1.0s cast, width 10 rect charge
    Rush2 = 11445, // Helper->location, no cast, width 9 rect charge
    VeniVidiVici = 21847, // Boss->location, no cast, width 10 rect charge

    GarulaRush = 10344, // Garula->Boss, 2.0s cast, width 8 rect charge, stuns boss
    CoeurlAuto = 870, // SteppeCoeurl->player/Boss, no cast, single-target
    MobAutos = 872, // SteppeYamaa1/SteppeYamaa/SteppeSheep/Garula->player/Boss, no cast, single-target
    Lanolin = 10328, // SteppeYamaa1->self, 2.5s cast, single-target
    Lullaby = 10340, // SteppeSheep->self, 3.0s cast, range 3+R circle
    HeadButt = 10341, // SteppeYamaa1->location, 2.5s cast, range 3+R width 3 rect
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
class HeadButt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeadButt, new AOEShapeRect(4.92f, 1.5f));

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
class FireballStack2(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.FireballBossRest, 5f);
// the puddles are predicted where the helper's untelegraphed hits land, then handed to the live fireball objects
class FirePuddle(ModuleBase module) : Components.VoidzoneAtCastTargetGroup(module, 5f, [(uint)AID.FireballFirst, (uint)AID.FireballRest], m => m.Enemies((uint)OID.Fireball).Where(e => e.EventState != 7), 0.5d);

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

class Ex9RathalosStates : StateMachineBuilder
{
    public Ex9RathalosStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Mangle>()
            .ActivateOnEnter<GarulaRush>()
            .ActivateOnEnter<Lullaby>()
            .ActivateOnEnter<HeadButt>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<TailSwing>()
            .ActivateOnEnter<Mangle2>()
            .ActivateOnEnter<FireballStack1>()
            .ActivateOnEnter<FireballStack2>()
            .ActivateOnEnter<FirePuddle>()
            .ActivateOnEnter<KingOfTheSkies>()
            .ActivateOnEnter<SweepingFlames>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<TargetHints>();
    }
}

[ModuleInfo(CFCID = 475u, NameID = 7221u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from Veyn's bossmod (awgil)")]
public class Ex9Rathalos(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsCircle(24.5f));
