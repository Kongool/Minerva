// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN5TrinityAvowed;

sealed class AllegiantArsenal(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeCone cone = new(70f, 135f.Degrees());
    private static readonly AOEShapeCircle circle = new(10f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.AllegiantArsenalBow or (uint)AID.AllegiantArsenalSword => cone,
            (uint)AID.AllegiantArsenalStaff => circle,
            _ => null
        };
        if (shape != null)
        {
            _aoe = [new(shape, spell.LocXZ, spell.Rotation + (spell.Action.ID == (uint)AID.AllegiantArsenalSword ? 180f.Degrees() : default), Module.CastFinishAt(spell, 8.1d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.InfernalSlash or (uint)AID.Flashvane or (uint)AID.FuryOfBozja)
        {
            _aoe = [];
        }
    }
}
