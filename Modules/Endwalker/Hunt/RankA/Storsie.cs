// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankA.Storsie;

public enum OID : uint
{
    Boss = 0x35DE, // R5.290, x1
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    AspectEarth = 27354, // Boss->self, 3.0s cast, single-target
    AspectWind = 27355, // Boss->self, 3.0s cast, single-target
    AspectLightning = 27356, // Boss->self, 3.0s cast, single-target
    Whorlstorm = 27358, // Boss->self, 1.0s cast, range 10-40 donut
    Defibrillate = 27359, // Boss->self, 1.0s cast, range 22 circle
    EarthenAugur = 27360, // Boss->self, 1.0s cast, range 30 270-degree cone
    FangsEnd = 27361, // Boss->player, 5.0s cast, single-target
    AspectEarthApply = 27870, // Boss->self, no cast, single-target
    AspectWindApply = 27871, // Boss->self, no cast, single-target
    AspectLightningApply = 27872, // Boss->self, no cast, single-target
}

class Aspect(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEShape? _imminentAOE;
    private DateTime _activation;
    private static readonly AOEShapeCone cone = new(30f, 135f.Degrees());
    private static readonly AOEShapeDonut donut = new(10f, 40f);
    private static readonly AOEShapeCircle circle = new(22f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_imminentAOE != null)
            return new AOEInstance[1] { new(_imminentAOE, Module.PrimaryActor.Position, Module.PrimaryActor.CastInfo?.Rotation ?? Module.PrimaryActor.Rotation, _activation) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.AspectEarth => cone,
            (uint)AID.AspectWind => donut,
            (uint)AID.AspectLightning => circle,
            _ => null
        };
        if (shape != null)
        {
            _imminentAOE = shape;
            _activation = World.FutureTime(10.4d);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.Whorlstorm or (uint)AID.Defibrillate or (uint)AID.EarthenAugur)
            _imminentAOE = null;
    }
}

class FangsEnd(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.FangsEnd);

class StorsieStates : StateMachineBuilder
{
    public StorsieStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Aspect>()
            .ActivateOnEnter<FangsEnd>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10623u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Storsie(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
