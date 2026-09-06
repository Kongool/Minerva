// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C030Trash1;

class Hydroshot(ModuleBase module) : Components.GenericKnockback(module)
{
    private Actor? _caster;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_caster?.CastInfo?.TargetID == actor.InstanceID)
            return new Knockback[1] { new(_caster.Position, 10f, Module.CastFinishAt(_caster.CastInfo)) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.NHydroshot or (uint)AID.SHydroshot)
            _caster = caster;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_caster == caster)
            _caster = null;
    }
}

class C030MonkStates : StateMachineBuilder
{
    private readonly bool _savage;

    public C030MonkStates(ModuleBase module, bool savage) : base(module)
    {
        _savage = savage;
        DeathPhase(0, SinglePhase)
            .ActivateOnEnter<Hydroshot>()
            .ActivateOnEnter<Twister>();
    }

    private void SinglePhase(uint id)
    {
        Hydroshot(id, 9.1f);
        CrossAttack(id + 0x10000, 3.4f);
        SimpleState(id + 0xFF0000, 10, "???");
    }

    private void Hydroshot(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SHydroshot : (uint)AID.NHydroshot, delay, 5, "Single-target 1");
        Cast(id + 0x10, _savage ? (uint)AID.SHydroshot : (uint)AID.NHydroshot, 1.5f, 5, "Single-target 2");
        Cast(id + 0x20, _savage ? (uint)AID.SHydroshot : (uint)AID.NHydroshot, 1.5f, 5, "Single-target 3");
    }

    private void CrossAttack(uint id, float delay)
    {
        Cast(id, _savage ? (uint)AID.SCrossAttack : (uint)AID.NCrossAttack, delay, 5, "Tankbuster")
            .SetHint(StateMachine.StateHint.Tankbuster);
    }
}
class C030NMonkStates(ModuleBase module) : C030MonkStates(module, false);
class C030SMonkStates(ModuleBase module) : C030MonkStates(module, true);

[ModuleInfo(CFCID = 979u, NameID = 12631u, PrimaryActorOID = (uint)OID.NMonk, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030NMonk(WorldState ws, Actor primary) : C030Trash1(ws, primary);

[ModuleInfo(CFCID = 980u, NameID = 12631u, PrimaryActorOID = (uint)OID.SMonk, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C030SMonk(WorldState ws, Actor primary) : C030Trash1(ws, primary);
