// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankA.Nuckelavee;

public enum OID : uint
{
    Boss = 0x288F, // R=3.6
}

public enum AID : uint
{
    AutoAttack = 872, // 288F->player, no cast, single-target
    Torpedo = 16964, // 288F->player, 4.0s cast, single-target, tankbuster on cast event
    BogBody = 16965, // 288F->player, 5.0s cast, range 5 circle, spread, applies bleed that can be dispelled
    Gallop = 16967, // 288F->location, 4.5s cast, rushes to target and casts Spite
    Spite = 18037, // 288F->self, no cast, range 8 circle
}

class Torpedo(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.Torpedo);
class BogBody(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.BogBody, 5f);

class Spite(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeCircle circle = new(8f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Gallop)
        {
            _aoe = [new(circle, spell.LocXZ, default, Module.CastFinishAt(spell))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Spite)
        {
            _aoe = [];
        }
    }
}

class NuckelaveeStates : StateMachineBuilder
{
    public NuckelaveeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Torpedo>()
            .ActivateOnEnter<BogBody>()
            .ActivateOnEnter<Spite>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8906u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Nuckelavee(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
