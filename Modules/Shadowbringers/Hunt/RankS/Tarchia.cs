// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankS.Tarchia;

public enum OID : uint
{
    Boss = 0x2873, // R=9.86
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    WakeUp = 18103, // Boss->self, no cast, single-target, visual for waking up from sleep
    WildHorn = 18026, // Boss->self, 3.0s cast, range 17 120-degree cone
    BafflementBulb = 18029, // Boss->self, 3.0s cast, range 40 circle, pull 50 between hitboxes, temporary misdirection
    ForestFire = 18030, // Boss->self, 5.0s cast, range 40 circle, damage fall off AOE, hard to tell optimal distance because logs are polluted by vuln stacks, guessing about 15
    MightySpin = 18028, // Boss->self, 3.0s cast, range 14 circle
    MightySpin2 = 18093, // Boss->self, no cast, range 14 circle, after 1s after boss wakes up and 4s after every Groundstorm
    Trounce = 18027, // Boss->self, 4.0s cast, range 40 60-degree cone
    MetamorphicBlast = 18031, // Boss->self, 4.0s cast, range 40 circle
    Groundstorm = 18023 // Boss->self, 5.0s cast, range 5-40 donut
}

class WildHorn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WildHorn, new AOEShapeCone(17f, 60f.Degrees()));
class Trounce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Trounce, new AOEShapeCone(40f, 30f.Degrees()));
class Groundstorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Groundstorm, new AOEShapeDonut(5f, 40f));
class MightySpin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MightySpin, 14f);
class ForestFire(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForestFire, 15f);
class BafflementBulb(ModuleBase module) : Components.TemporaryMisdirection(module, (uint)AID.BafflementBulb, "Pull + Temporary Misdirection -> Donut -> Out");
class MetamorphicBlast(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MetamorphicBlast);

class MightySpin2(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(14f);
    private AOEInstance[] _aoe = [new(circle, module.PrimaryActor.Position.Quantized())];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Groundstorm)
        {
            _aoe = [new(circle, spell.LocXZ, default, Module.CastFinishAt(spell, 4d))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID != (uint)AID.Groundstorm)
        {
            _aoe = [];
        }
    }
}

class TarchiaStates : StateMachineBuilder
{
    public TarchiaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MetamorphicBlast>()
            .ActivateOnEnter<MightySpin>()
            .ActivateOnEnter<WildHorn>()
            .ActivateOnEnter<Trounce>()
            .ActivateOnEnter<BafflementBulb>()
            .ActivateOnEnter<Groundstorm>()
            .ActivateOnEnter<ForestFire>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8900u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Tarchia : SimpleBossModule
{
    public Tarchia(WorldState ws, Actor primary) : base(ws, primary)
    {
        ActivateComponent<MightySpin2>();
    }
}
