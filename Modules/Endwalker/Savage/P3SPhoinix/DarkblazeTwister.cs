// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P3SPhoinix;

// state related to darkblaze twister mechanics
class TwisterVoidzone(ModuleBase module) : Components.Voidzone(module, 5f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.TwisterVoidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}
class BurningTwister(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BurningTwister, new AOEShapeDonut(7f, 20f));

class DarkTwister(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.DarkTwister, _knockbackRange, true)
{
    private const float _knockbackRange = 17f;
    private const float _aoeInnerRadius = 5f;
    private const float _aoeMiddleRadius = 7f;
    private const float safeOffset = _knockbackRange + (_aoeInnerRadius + _aoeMiddleRadius) / 2f;
    private const float safeRadius = (_aoeMiddleRadius - _aoeInnerRadius) / 2f;

    private TwisterVoidzone? _aoe1 = module.FindComponent<TwisterVoidzone>();
    private BurningTwister? _aoe2 = module.FindComponent<BurningTwister>();

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (Casters.Count == 0)
            return;
        ref readonly var darkTwister = ref Casters.Ref(0);
        foreach (var burningTwister in BurningTwisters())
        {
            var dir = burningTwister.Position - darkTwister.Origin;
            var len = dir.Length();
            dir /= len;
            Arena.ZoneCircleOutline(darkTwister.Origin + dir * (len - safeOffset), safeRadius, Colors.Safe);
        }

        List<Actor> BurningTwisters()
        {
            List<Actor> burningTwisters = [];
            var twisters = Module.Enemies((uint)OID.DarkblazeTwister);
            var count = twisters.Count;
            for (var i = 0; i < count; ++i)
            {
                var twister = twisters[i];
                if (twister.CastInfo != null && twister.CastInfo.IsSpell(AID.BurningTwister))
                    burningTwisters.Add(twister);
            }
            return burningTwisters;
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        _aoe1 ??= Module.FindComponent<TwisterVoidzone>();
        if (_aoe1 != null)
        {
            var aoes = _aoe1.ActiveAOEs(slot, actor);
            var len = aoes.Length;
            for (var i = 0; i < len; ++i)
            {
                ref readonly var aoe = ref aoes[i];
                if (aoe.Check(pos))
                {
                    return true;
                }
            }
        }
        _aoe2 ??= Module.FindComponent<BurningTwister>();
        if (_aoe2 != null)
        {
            var aoes = _aoe2.ActiveAOEs(slot, actor);
            var len = aoes.Length;
            for (var i = 0; i < len; ++i)
            {
                ref readonly var aoe = ref aoes[i];
                if (aoe.Check(pos))
                {
                    return true;
                }
            }
        }
        return !InBounds(pos);
    }
}
