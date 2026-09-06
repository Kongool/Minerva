// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankA.Rusalka;

public enum OID : uint
{
    Boss = 0x2853 // R=3.6
}

public enum AID : uint
{
    AutoAttack = 17364, // Boss->player, no cast, single-target
    Hydrocannon = 17363, // Boss->location, 3.5s cast, range 8 circle
    AetherialSpark = 17368, // Boss->self, 2.5s cast, range 12 width 4 rect
    AetherialPull = 17366, // Boss->self, 4.0s cast, range 30 circle, pull 30 between centers
    Flood = 17369 // Boss->self, no cast, range 8 circle
}

class Hydrocannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hydrocannon, 8f);
class AetherialSpark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AetherialSpark, new AOEShapeRect(12f, 2f));

class AetherialPull(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.AetherialPull, 30f, shape: new AOEShapeCircle(30f), kind: Kind.TowardsOrigin)
{
    private readonly Flood _aoe = module.FindComponent<Flood>()!;

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        if (_aoe.AOE.Length != 0)
        {
            ref var aoe = ref _aoe.AOE[0];
            return aoe.Check(pos);
        }
        return false;
    }
}

class Flood(ModuleBase module) : Components.GenericAOEs(module)
{
    public AOEInstance[] AOE = [];
    private static readonly AOEShapeCircle circle = new(8f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => AOE;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.AetherialPull)
        {
            AOE = [new(circle, spell.LocXZ, default, Module.CastFinishAt(spell, 3.6d))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Flood)
        {
            AOE = [];
        }
    }
}

class RusalkaStates : StateMachineBuilder
{
    public RusalkaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Hydrocannon>()
            .ActivateOnEnter<AetherialSpark>()
            .ActivateOnEnter<Flood>()
            .ActivateOnEnter<AetherialPull>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8896u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Rusalka(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
