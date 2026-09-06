// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V22Moko;

sealed class YamaKagura(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.YamaKagura, 33f, shape: new AOEShapeRect(40f, 2.5f), kind: Kind.DirForward)
{
    private readonly GhastlyGrasp _aoe = module.FindComponent<GhastlyGrasp>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = Casters.Count;
        if (count != 0)
        {
            var casters = CollectionsMarshal.AsSpan(Casters);
            var forbidden = new ShapeDistance[count];
            for (var i = 0; i < count; ++i)
            {
                ref readonly var c = ref casters[i];
                forbidden[i] = new SDRect(c.Origin, c.Direction, 40f, Distance - 40f, 2.5f);
            }
            hints.AddForbiddenZone(new SDUnion(forbidden), Casters.Ref(0).Activation);
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }
}
