// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un4Zurvan;

class P2SoarTwinSpirit(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<(Actor caster, AOEInstance aoe)> _pending = [];

    private readonly AOEShapeRect _shape = new(50f, 5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _pending.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            aoes[i] = _pending[i].aoe;
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TwinSpiritFirst)
        {
            _pending.Add((caster, new(_shape, spell.LocXZ, Angle.FromDirection(spell.LocXZ - caster.Position), Module.CastFinishAt(spell))));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.TwinSpiritFirst:
                var index = _pending.FindIndex(p => p.caster == caster);
                if (index >= 0)
                    _pending[index] = (caster, new(_shape, spell.LocXZ, Angle.FromDirection(Center - spell.LocXZ), World.FutureTime(9.2d)));
                break;
            case (uint)AID.TwinSpiritSecond:
                _pending.RemoveAll(p => p.caster == caster);
                ++NumCasts;
                break;
        }
    }
}

class P2SoarFlamingHalberd(ModuleBase module) : Components.UniformStackSpread(module, default, 12f)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.FlamingHalberd)
            AddSpread(actor, World.FutureTime(5.1d));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FlamingHalberd)
            Spreads.Clear(); // don't bother finding proper target, they all happen at the same time
    }
}

class P2SoarFlamingHalberdVoidzone(ModuleBase module) : Components.Voidzone(module, 8f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.FlamingHalberdVoidzone);
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

class P2SoarDemonicDiveCoolFlame(ModuleBase module) : Components.UniformStackSpread(module, 7f, 8f, 7)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        switch (iconID)
        {
            case (uint)IconID.DemonicDive:
                AddStack(actor, World.FutureTime(5.1d));
                break;
            case (uint)IconID.CoolFlame:
                AddSpread(actor, World.FutureTime(5.1d));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.DemonicDive:
                Stacks.Clear();
                break;
            case (uint)AID.CoolFlame:
                Spreads.Clear();
                break;
        }
    }
}
