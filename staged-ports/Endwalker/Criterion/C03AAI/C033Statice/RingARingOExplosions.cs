// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C033Statice;

class RingARingOExplosions(ModuleBase module) : Components.GenericAOEs(module)
{
    public List<Actor> ActiveBombs = [];
    private readonly List<Actor> _bombs = [];
    private DateTime _activation;

    private static readonly AOEShapeCircle _shape = new(12f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = ActiveBombs.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            aoes[i] = new(_shape, ActiveBombs[i].Position, default, _activation);
        }
        return aoes;
    }

    public override void Update()
    {
        var count = _bombs.Count;
        if (count == 6 && ActiveBombs.Count == 0)
        {
            Actor? glowingBomb = null;
            for (var i = 0; i < count; ++i)
            {
                var bomb = _bombs[i];
                if (bomb.ModelState.AnimState1 == 1)
                {
                    glowingBomb = bomb;
                    break;
                }
            }
            if (glowingBomb != null)
            {
                var cur = glowingBomb;
                do
                {
                    ActiveBombs.Add(cur);
                    cur = World.Actors.Find(cur.Tether.Target);
                } while (cur != null && cur != glowingBomb);
                _activation = World.FutureTime(17.4d);
            }
        }
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID is (uint)OID.NBomb or (uint)OID.SBomb)
            _bombs.Add(actor);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NBombBurst or (uint)AID.SBombBurst)
        {
            ++NumCasts;
            ActiveBombs.Remove(caster);
        }
    }
}
