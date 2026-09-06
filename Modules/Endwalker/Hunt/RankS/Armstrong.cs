// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankS.Armstrong;

public enum OID : uint
{
    Boss = 0x35BE, // R7.800, x1
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    RotateCW = 27470, // Boss->self, no cast, single-target
    RotateCCW = 27471, // Boss->self, no cast, single-target
    MagitekCompressorFirst = 27472, // Boss->self, 6.0s cast, range 50 width 7 cross
    MagitekCompressorReverse = 27473, // Boss->self, 2.0s cast, range 50 width 7 cross
    MagitekCompressorNext = 27474, // Boss->self, 0.5s cast, range 50 width 7 cross
    AcceleratedLanding = 27475, // Boss->location, 4.0s cast, range 6 circle
    CalculatedCombustion = 27476, // Boss->self, 5.0s cast, range 35 circle
    Pummel = 27477, // Boss->player, 5.0s cast, single-target
    SoporificGas = 27478, // Boss->self, 6.0s cast, range 12 circle
}

class MagitekCompressor(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private Angle _increment;

    private static readonly AOEShapeCross _shape = new(50, 3.5f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.MagitekCompressorFirst)
        {
            NumCasts = 0;
            Sequences.Add(new(_shape, spell.LocXZ, spell.Rotation, _increment, Module.CastFinishAt(spell), 2.1f, 10));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.RotateCW:
                _increment = -30f.Degrees();
                break;
            case (uint)AID.RotateCCW:
                _increment = 30f.Degrees();
                break;
            case (uint)AID.MagitekCompressorFirst:
            case (uint)AID.MagitekCompressorReverse:
            case (uint)AID.MagitekCompressorNext:
                if (Sequences.Count != 0)
                {
                    AdvanceSequence(0, World.CurrentTime);
                    if (NumCasts == 5)
                    {
                        ref var s = ref Sequences.Ref(0);
                        s.Increment = -s.Increment;
                        s.NextActivation = World.FutureTime(3.6d);
                    }
                }
                break;
        }
    }
}

class AcceleratedLanding(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AcceleratedLanding, 6f);
class CalculatedCombustion(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.CalculatedCombustion);
class Pummel(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Pummel);
class SoporificGas(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SoporificGas, 12f);

class ArmstrongStates : StateMachineBuilder
{
    public ArmstrongStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MagitekCompressor>()
            .ActivateOnEnter<AcceleratedLanding>()
            .ActivateOnEnter<CalculatedCombustion>()
            .ActivateOnEnter<Pummel>()
            .ActivateOnEnter<SoporificGas>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10619u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Armstrong(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
