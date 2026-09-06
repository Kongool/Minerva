// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T01Valigarmanda;

sealed class ChillingCataclysm(ModuleBase module) : Components.GenericAOEs(module)
{
    public readonly List<AOEInstance> AOEs = [];

    private static readonly AOEShapeCross _shape = new(40f, 2.5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.ArcaneSphere2)
        {
            var activation = World.FutureTime(7.1d);
            var pos = actor.Position.Quantized();
            AOEs.Add(new(_shape, pos, Angle.AnglesCardinals[1], activation));
            AOEs.Add(new(_shape, pos, Angle.AnglesIntercardinals[1], activation));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ChillingCataclysm)
        {
            AOEs.Clear();
        }
    }
}
