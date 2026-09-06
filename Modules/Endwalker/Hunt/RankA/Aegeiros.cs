// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankA.Aegeiros;

public enum OID : uint
{
    Boss = 0x3671 // R7.500, x1
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    Leafstorm = 27708, // Boss->self, 6.0s cast, range 10 circle
    Rimestorm = 27709, // Boss->self, 1.0s cast, range 40 180-degree cone
    Snowball = 27710, // Boss->location, 3.0s cast, range 8 circle
    Canopy = 27711, // Boss->players, no cast, range 12 120-degree cone cleave
    BackhandBlow = 27712 // Boss->self, 3.0s cast, range 12 120-degree cone
}

class LeafstormRimestorm(ModuleBase module) : Components.GenericAOEs(module)
{
    private DateTime _rimestormExpected;
    private static readonly AOEShapeCircle _leafstorm = new(10f);
    private static readonly AOEShapeCone _rimestorm = new(40f, 90f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var aoes = new List<AOEInstance>(2);
        if (Module.PrimaryActor.CastInfo?.IsSpell(AID.Leafstorm) ?? false)
            aoes.Add(new(_leafstorm, Module.PrimaryActor.Position, Module.PrimaryActor.CastInfo!.Rotation, Module.CastFinishAt(Module.PrimaryActor.CastInfo)));
        if (Module.PrimaryActor.CastInfo?.IsSpell(AID.Rimestorm) ?? false)
            aoes.Add(new(_rimestorm, Module.PrimaryActor.Position, Module.PrimaryActor.CastInfo!.Rotation, Module.CastFinishAt(Module.PrimaryActor.CastInfo)));
        else if (_rimestormExpected != default)
            aoes.Add(new(_rimestorm, Module.PrimaryActor.Position, Module.PrimaryActor.CastInfo?.Rotation ?? Module.PrimaryActor.Rotation, _rimestormExpected));
        return CollectionsMarshal.AsSpan(aoes);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Leafstorm)
            _rimestormExpected = World.FutureTime(9.6d);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Rimestorm)
            _rimestormExpected = default;
    }
}

class Snowball(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Snowball, 8f);
class Canopy(ModuleBase module) : Components.Cleave(module, (uint)AID.Canopy, new AOEShapeCone(12f, 60f.Degrees()), activeWhileCasting: false);
class BackhandBlow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BackhandBlow, new AOEShapeCone(12f, 60f.Degrees()));

class AegeirosStates : StateMachineBuilder
{
    public AegeirosStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<LeafstormRimestorm>()
            .ActivateOnEnter<Snowball>()
            .ActivateOnEnter<Canopy>()
            .ActivateOnEnter<BackhandBlow>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10628u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Aegeiros(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
