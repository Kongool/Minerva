// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A21AegisUnit;

sealed class BeamCannons(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCone coneSmall = new(40f, 15f.Degrees()), coneMed = new(40f, 30f.Degrees()), coneBig = new(40f, 45f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ManeuverBeamCannons)
        {
            var act = Module.CastFinishAt(spell, 0.3d);
            var pos = spell.LocXZ;
            var rot = spell.Rotation;
            var a144 = 144f.Degrees();
            var a72 = 72f.Degrees();
            AddAOE(coneSmall, -a144);
            AddAOE(coneMed, a72);
            AddAOE(coneMed);
            AddAOE(coneBig, -a72);
            AddAOE(coneBig, a144);
            void AddAOE(AOEShapeCone shape, Angle offset = default) => _aoes.Add(new(shape, pos, rot + offset, act));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.BeamCannons1)
        {
            _aoes.Clear();
        }
    }
}
