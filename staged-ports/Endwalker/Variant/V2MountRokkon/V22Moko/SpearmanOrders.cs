// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V22Moko;

sealed class SpearmanOrdersFast(ModuleBase module) : Components.Exaflare(module, new AOEShapeRect(3f, 2f, 0.474f))
{
    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.AshigaruSoheiFast && id == 0x25E9)
        {
            Lines.Add(new(actor.Position, 3.474f * actor.Rotation.ToDirection(), World.FutureTime(7.8d), 0.6d, 12, 4));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SpearpointPushFast)
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
                    {
                        Lines.RemoveAt(i);
                    }
                    return;
                }
            }
        }
    }
}

sealed class SpearmanOrdersSlow(ModuleBase module) : Components.Exaflare(module, new AOEShapeRect(2f, 2f, 0.316f))
{
    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.AshigaruSoheiSlow && id == 0x25E9)
        {
            Lines.Add(new(actor.Position, 2.316f * actor.Rotation.ToDirection(), World.FutureTime(7.8d), 0.6d, 18, 2));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SpearpointPushSlow)
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
                    {
                        Lines.RemoveAt(i);
                    }
                    return;
                }
            }
        }
    }
}
