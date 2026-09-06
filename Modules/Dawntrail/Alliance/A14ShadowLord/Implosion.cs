// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A14ShadowLord;

sealed class Implosion(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private readonly AOEShapeCone _shapeSmall = new(12f, 90f.Degrees()), _shapeLarge = new(90f, 90f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => Module.FindComponent<GigaSlash>()?.AOEs.Count == 0 ? CollectionsMarshal.AsSpan(_aoes) : [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var shape = spell.Action.ID switch
        {
            (uint)AID.ImplosionLargeL or (uint)AID.ImplosionLargeR => _shapeLarge,
            (uint)AID.ImplosionSmallL or (uint)AID.ImplosionSmallR => _shapeSmall,
            _ => null
        };
        if (shape != null)
        {
            _aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell), actorID: caster.InstanceID));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.ImplosionLargeL or (uint)AID.ImplosionSmallR or (uint)AID.ImplosionLargeR or (uint)AID.ImplosionSmallL)
        {
            ++NumCasts;
            var count = _aoes.Count;
            var aoes = CollectionsMarshal.AsSpan(_aoes);
            var id = caster.InstanceID;
            for (var i = 0; i < count; ++i)
            {
                ref var aoe = ref aoes[i];
                if (aoe.ActorID == id)
                {
                    _aoes.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
