// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C012Gladiator;

abstract class SunderedRemains(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 10f, 8);
sealed class NSunderedRemains(ModuleBase module) : SunderedRemains(module, (uint)AID.NSunderedRemains);
sealed class SSunderedRemains(ModuleBase module) : SunderedRemains(module, (uint)AID.SSunderedRemains);

sealed class ScreamOfTheFallen(ModuleBase module) : Components.UniformStackSpread(module, default, 15f)
{
    private BitMask _second;
    private readonly List<Actor> _towers = [];

    private const float _towerRadius = 3;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (!IsSpreadTarget(actor))
            hints.Add("Soak the tower!", !ActiveTowers(_second[slot]).Any(t => t.Position.InCircle(actor.Position, _towerRadius)));
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        base.DrawArenaForeground(pcSlot, pc);
        if (!IsSpreadTarget(pc))
            foreach (var t in ActiveTowers(_second[pcSlot]))
                Arena.ZoneCircleOutline(t.Position, _towerRadius, Colors.Safe, 2f);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.FirstInLine:
                AddSpread(actor);
                break;
            case (uint)SID.SecondInLine:
                _second.Set(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.NExplosion or (uint)AID.SExplosion)
            _towers.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.NExplosion or (uint)AID.SExplosion)
        {
            switch (++NumCasts)
            {
                case 2:
                    Spreads.Clear();
                    AddSpreads(Raid.WithSlot(false, true, true).IncludedInMask(_second).Actors());
                    break;
                case 4:
                    Spreads.Clear();
                    break;
            }
        }
    }

    private List<Actor> ActiveTowers(bool second)
    {
        List<Actor> result = [];
        var count = _towers.Count;
        if (second)
        {
            for (var i = 0; i < 2 && i < count; ++i)
                result.Add(_towers[i]);
        }
        else
        {
            for (var i = 2; i < count; ++i)
                result.Add(_towers[i]);
        }
        return result;
    }
}
