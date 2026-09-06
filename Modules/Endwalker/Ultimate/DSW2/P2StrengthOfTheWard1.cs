// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

// spreads
sealed class P2StrengthOfTheWard1LightningStorm : Components.UniformStackSpread
{
    public P2StrengthOfTheWard1LightningStorm(ModuleBase module) : base(module, default, 5f)
    {
        AddSpreads(Raid.WithoutSlot(true, true, true));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.LightningStormAOE)
            Spreads.Clear();
    }
}

// charges
sealed class P2StrengthOfTheWard1SpiralThrust(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.SpiralThrust, "GTFO from charge aoe!")
{
    private readonly List<Actor> _knights = [];

    private static readonly AOEShapeRect _shape = new(52f, 8f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _knights.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var k = _knights[i];
            aoes[i] = new(_shape, k.Position, k.Rotation); // TODO: activation
        }
        return aoes;
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x1E43 && actor.OID is (uint)OID.SerVellguine or (uint)OID.SerPaulecrain or (uint)OID.SerIgnasse)
            _knights.Add(actor);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _knights.Remove(caster);
            ++NumCasts;
        }
    }
}

// rings
sealed class P2StrengthOfTheWard1HeavyImpact(ModuleBase module) : HeavyImpact(module, 8.2d);
