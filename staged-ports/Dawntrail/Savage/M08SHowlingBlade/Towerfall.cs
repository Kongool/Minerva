// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M08SHowlingBlade;

sealed class Towerfall(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly AOEShapeRect rect = new(30f, 5f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.TerrestrialTitans:
                AddAOE();
                AddAOE(180f.Degrees());
                break;
            case (uint)AID.Towerfall:
                if (_aoes.Count == 4)
                {
                    _aoes.Clear();
                }
                AddAOE(delay: 0f);
                break;
        }

        void AddAOE(Angle offset = default, double delay = 14.7d)
        => _aoes.Add(new(rect, spell.LocXZ, spell.Rotation + offset, Module.CastFinishAt(spell, delay)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Towerfall)
        {
            ++NumCasts;
        }
    }
}

sealed class FangedCrossing(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly AOEShapeCross cross = new(21f, 3.5f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x11D1 && actor.OID == (uint)OID.GleamingFang1)
        {
            _aoes.Add(new(cross, actor.Position.Quantized(), actor.Rotation, World.FutureTime(6d)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FangedCrossing)
        {
            ++NumCasts;
        }
    }
}

sealed class TerrestrialTitans(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TerrestrialTitans, 5f);
