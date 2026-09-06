// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A24Ealdnarche;

sealed class Sleepga(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeRect rect = new(70f, 35f);
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.WarpVisual1 && Module.PrimaryActor is var primary && !primary.Position.AlmostEqual(caster.Position, 0.1f) && spell.Rotation != primary.Rotation)
        {
            _aoe = [new(rect, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell, 6.5d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Sleepga)
        {
            _aoe = [];
        }
    }
}

sealed class Sleep(ModuleBase module) : Components.CleansableDebuff(module, (uint)SID.Sleep, "Sleep", "put to sleep");
