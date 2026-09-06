// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL4DiabloArmament;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeDonut donut = new(17f, 30f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DiabolicGateVisual)
        {
            _aoe = [new(donut, Center.Quantized(), default, Module.CastFinishAt(spell, 9.2d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x33)
        {
            if (state == 0x00020001u)
            {
                Bounds = DAL4DiabloArmament.SmallArena;
                Center = Center.Quantized();
                _aoe = [];
            }
            else if (state == 0x00080004u)
            {
                Bounds = DAL4DiabloArmament.DefaultArena;
                Center = DAL4DiabloArmament.ArenaCenter;
            }
        }
    }
}