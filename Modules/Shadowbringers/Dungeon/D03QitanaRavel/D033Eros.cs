// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Dungeon.D03QitanaRavel.D033Eros;

public enum OID : uint
{
    Boss = 0x27B1, //R=7.02
    PoisonVoidzone = 0x1E972C,
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Jump = 15519, // Boss->location, no cast, single-target, visual

    Rend = 15513, // Boss->player, 4.0s cast, single-target, tankbuster
    HoundOutOfHeaven = 15514, // Boss->self, 5.0s cast, single-target
    HoundOutOfHeavenSuccess = 17079, // Boss->player, no cast, single-target, tether stretch success
    HoundOutOfHeavenFail = 17080, // Boss->player, no cast, single-target, tether stretch fail
    Glossolalia = 15515, // Boss->self, 3.0s cast, range 50 circle, raidwide
    ViperPoison = 15516, // Boss->self, 6.0s cast, single-target
    ViperPoisonPatterns = 15518, // Helper->location, 6.0s cast, range 6 circle
    ViperPoisonBait = 15517, // Helper->player, 6.0s cast, range 6 circle

    Inhale = 17168, // Boss->self, 4.0s cast, range 50 circle, attract 50 between centers

    HeavingBreathVisual = 16923, // Helper->self, 3.5s cast, range 42 width 30 rect
    HeavingBreath = 15520, // Boss->self, 3.5s cast, range 50 circle, knockback 35 forward

    ConfessionOfFaithVisual1 = 15524, // Boss->self, 5.0s cast, single-target, left/right breath
    ConfessionOfFaithVisual2 = 15521, // Boss->self, 5.0s cast, single-target, center breath
    ConfessionOfFaithLeft = 15526, // Helper->self, 5.5s cast, range 60 60-degree cone
    ConfessionOfFaithRight = 15527, // Helper->self, 5.5s cast, range 60 60-degree cone
    ConfessionOfFaithCenter = 15522, // Helper->self, 5.5s cast, range 60 60-degree cone
    ConfessionOfFaithStack = 15525, // Helper->players, 5.8s cast, range 6 circle, stack
    ConfessionOfFaithSpread = 15523 // Helper->player, 5.8s cast, range 5 circle, spread
}

class HoundOutOfHeaven(ModuleBase module) : Components.StretchTetherDuo(module, 19f, 5.2f);
class ViperPoisonVoidzone(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 6f, (uint)AID.ViperPoisonPatterns, GetVoidzones, 0.8f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.PoisonVoidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}
class ConfessionOfFaithStack(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.ConfessionOfFaithStack, 6f, 4, 4);
class ConfessionOfFaithSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.ConfessionOfFaithSpread, 5f);

abstract class Breath(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 30f.Degrees()));
class ConfessionOfFaithCenter(ModuleBase module) : Breath(module, (uint)AID.ConfessionOfFaithCenter);
class ConfessionOfFaithLeft(ModuleBase module) : Breath(module, (uint)AID.ConfessionOfFaithLeft);
class ConfessionOfFaithRight(ModuleBase module) : Breath(module, (uint)AID.ConfessionOfFaithRight);

class ViperPoisonBait(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.ViperPoisonBait, 6f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (IsBaitTarget(actor))
        {
            hints.AddForbiddenZone(new SDRect(new(17f, -518f), new(17f, -558f), 13f));
        }
    }
}

class Inhale(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Inhale, 50f, kind: Kind.TowardsOrigin)
{
    private readonly ViperPoisonVoidzone _aoe = module.FindComponent<ViperPoisonVoidzone>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            var aoes = _aoe.ActiveAOEs(slot, actor);
            var len = aoes.Length;
            var forbidden = new ShapeDistance[len];
            for (var i = 0; i < len; ++i)
            {
                ref readonly var aoe = ref aoes[0];
                forbidden[i] = new SDRect(aoe.Origin, Module.PrimaryActor.Rotation, 40f, default, 6f);
            }
            if (forbidden.Length != 0)
            {
                hints.AddForbiddenZone(new SDUnion(forbidden), Casters.Ref(0).Activation);
            }
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return false;
    }
}

class HeavingBreath(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.HeavingBreath, 35f, kind: Kind.DirForward, stopAtWall: true)
{
    private readonly ViperPoisonVoidzone _aoe = module.FindComponent<ViperPoisonVoidzone>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            var aoes = _aoe.ActiveAOEs(slot, actor);
            var len = aoes.Length;
            var forbidden = new ShapeDistance[len];
            for (var i = 0; i < len; ++i)
            {
                ref readonly var aoe = ref aoes[0];
                forbidden[i] = new SDRect(aoe.Origin, new WDir(default, 1f), 40f, 40f, 6f);
            }
            if (forbidden.Length != 0)
            {
                hints.AddForbiddenZone(new SDUnion(forbidden), Casters.Ref(0).Activation);
            }
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe.ActiveAOEs(slot, actor);
        var len1 = aoes.Length;
        for (var i = 0; i < len1; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return false;
    }
}

class Glossolalia(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Glossolalia);
class Rend(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.Rend);

class D033ErosStates : StateMachineBuilder
{
    public D033ErosStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ViperPoisonBait>()
            .ActivateOnEnter<ViperPoisonVoidzone>()
            .ActivateOnEnter<Rend>()
            .ActivateOnEnter<HoundOutOfHeaven>()
            .ActivateOnEnter<Glossolalia>()
            .ActivateOnEnter<ConfessionOfFaithCenter>()
            .ActivateOnEnter<ConfessionOfFaithRight>()
            .ActivateOnEnter<ConfessionOfFaithLeft>()
            .ActivateOnEnter<ConfessionOfFaithSpread>()
            .ActivateOnEnter<ConfessionOfFaithStack>()
            .ActivateOnEnter<HeavingBreath>()
            .ActivateOnEnter<Inhale>();
    }
}

[ModuleInfo(CFCID = 651u, NameID = 8233u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D033Eros(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(17, -538), new ArenaBoundsRect(14.5f, 19.5f));
