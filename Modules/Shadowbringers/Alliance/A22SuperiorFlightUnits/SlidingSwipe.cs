// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A22SuperiorFlightUnits;

sealed class SlidingSwipe(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(30f, 110f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var max = count > 2 ? 2 : count;
        ref var aoe0 = ref aoes[0];
        aoe0.Risky = true;
        if (count > 1)
        {
            aoe0.Color = Colors.Danger;
        }
        return aoes[..max];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        // two additional AIDs exist in the sheets, but never seen in 12+ logs. I assume they are cut content unless proven otherwise
        var offset = spell.Action.ID switch
        {
            (uint)AID.SlidingSwipeAlphaR or (uint)AID.SlidingSwipeChiR => -1f,
            (uint)AID.SlidingSwipeAlphaL or (uint)AID.SlidingSwipeBetaL => 1f,
            _ => default
        };
        if (offset != default)
        {
            _aoes.Add(new(rect, spell.LocXZ, spell.Rotation + offset * 90f.Degrees(), Module.CastFinishAt(spell, 0.5d), risky: false));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID is (uint)AID.SlidingSwipe1 or (uint)AID.SlidingSwipe2)
        {
            _aoes.RemoveAt(0);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (_aoes.Count < 2)
        {
            return;
        }
        // make ai stay close to boss to ensure successfully dodging the combo
        hints.AddForbiddenZone(new SDInvertedRect(Center, new WDir(1f, default), 2f, 2f, 40f), _aoes.Ref(0).Activation);
    }
}
