// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Quest.MSQ.Endwalker;

sealed class SilveredEdge(ModuleBase module) : Components.GenericAOEs(module)
{
    private DateTime _activation;
    private bool active;
    private bool casting;
    private Angle _rotation;

    private static readonly AOEShapeRect rect = new(40f, 3f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (active)
        {
            var pos = Module.PrimaryActor.Position;
            Span<AOEInstance> aoes = new AOEInstance[3];
            var angle = casting ? Angle.FromDirection(actor.Position - pos) : _rotation;
            for (var i = 0; i < 3; ++i)
            {
                aoes[i] = new(rect, pos, angle + i * 120f.Degrees(), _activation);
            }
            return aoes;
        }
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SilveredEdge)
        {
            active = true;
            casting = true;
            _activation = Module.CastFinishAt(spell, 1.4f);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SilveredEdge)
        {
            casting = false;
            _rotation = Angle.FromDirection(Raid.Player()!.Position - caster.Position);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SilveredEdgeVisual)
        {
            if (++NumCasts == 3)
            {
                NumCasts = 0;
                active = false;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!casting)
            base.AddAIHints(slot, actor, assignment, hints);
    }
}
