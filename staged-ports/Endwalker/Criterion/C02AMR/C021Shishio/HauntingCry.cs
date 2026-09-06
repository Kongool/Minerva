// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C021Shishio;

sealed class HauntingCrySwipes(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCone _shape = new(40f, 90f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.NRightSwipe:
            case (uint)AID.NLeftSwipe:
            case (uint)AID.SRightSwipe:
            case (uint)AID.SLeftSwipe:
                _aoes.Add(new(_shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.NRightSwipe:
            case (uint)AID.NLeftSwipe:
            case (uint)AID.SRightSwipe:
            case (uint)AID.SLeftSwipe:
                ++NumCasts;
                if (_aoes.Count != 0)
                    _aoes.RemoveAt(0);
                break;
        }
    }
}

sealed class HauntingCryReisho(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> _ghosts = [];
    private DateTime _activation;
    private DateTime _ignoreBefore;

    private static readonly AOEShapeCircle _shape = new(6f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _ghosts.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
            aoes[i] = new(_shape, _ghosts[i].Position, default, _activation);
        return aoes;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var count = _ghosts.Count;
        if (count == 0)
            return;
        for (var i = 0; i < count; ++i)
        {
            var g = _ghosts[i];
            Arena.Actor(g, Colors.Object, true);
            var target = World.Actors.Find(g.Tether.Target);
            if (target != null)
                Arena.AddLine(g.Position, target.Position, Colors.Danger);
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID is (uint)OID.NHauntingThrall or (uint)OID.SHauntingThrall)
        {
            _ghosts.Add(source);
            _activation = World.FutureTime(5.1d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NReisho or (uint)AID.SReisho && World.CurrentTime > _ignoreBefore)
        {
            ++NumCasts;
            _activation = World.FutureTime(2.1f);
            _ignoreBefore = World.FutureTime(1);
        }
    }
}

abstract class HauntingCryVermilionAura(ModuleBase module, uint aid) : Components.CastTowers(module, aid, 4f);
sealed class NHauntingCryVermilionAura(ModuleBase module) : HauntingCryVermilionAura(module, (uint)AID.NVermilionAura);
sealed class SHauntingCryVermilionAura(ModuleBase module) : HauntingCryVermilionAura(module, (uint)AID.SVermilionAura);

abstract class HauntingCryStygianAura(ModuleBase module, uint aid) : Components.SpreadFromCastTargets(module, aid, 15f);
sealed class NHauntingCryStygianAura(ModuleBase module) : HauntingCryStygianAura(module, (uint)AID.NStygianAura);
sealed class SHauntingCryStygianAura(ModuleBase module) : HauntingCryStygianAura(module, (uint)AID.SStygianAura);
