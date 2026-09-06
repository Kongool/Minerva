// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA2Raiden;

sealed class LancingBlowSpread(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Spreadmarker, (uint)AID.LancingBlow, 10f, 6f)
{
    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.StreakLightning)
            Spreads.Clear();
    }
}

sealed class LancingBlowAOE(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(10f);
    public readonly List<AOEInstance> AOEs = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.StreakLightning)
            AOEs.Add(new(circle, actor.Position.Quantized(), default, World.FutureTime(1d)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.LancingBlow)
        {
            ++NumCasts;
            AOEs.Clear();
        }
    }
}
