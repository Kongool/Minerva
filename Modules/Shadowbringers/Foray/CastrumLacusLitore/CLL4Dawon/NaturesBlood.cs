// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL4Dawon;

sealed class NaturesBlood(ModuleBase module) : Components.Exaflare(module, 4f)
{
    private readonly ArenaChange _arena = module.FindComponent<ArenaChange>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (!_arena.IsDawonArena)
            return base.ActiveAOEs(slot, actor);
        else
            return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.NaturesBloodFirst)
        {
            Lines.Add(new(caster.Position, 6f * spell.Rotation.ToDirection(), Module.CastFinishAt(spell), 1.1d, 7, 3));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NaturesBloodFirst or (uint)AID.NaturesBloodRest)
        {
            var count = Lines.Count;
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                var line = Lines[i];
                if (line.Next.AlmostEqual(pos, 1f))
                {
                    AdvanceLine(line, pos);
                    if (line.ExplosionsLeft == 0)
                        Lines.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
