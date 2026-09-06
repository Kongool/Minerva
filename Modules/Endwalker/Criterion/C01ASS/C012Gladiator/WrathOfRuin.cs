// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C012Gladiator;

sealed class GoldenSilverFlame(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _goldenFlames = [];
    private readonly List<Actor> _silverFlames = [];
    private readonly int[] _debuffs = new int[PartyState.MaxPartySize]; // silver << 16 | gold

    public bool Active => _goldenFlames.Count + _silverFlames.Count > 0;

    private static readonly AOEShapeRect _shape = new(60f, 5f);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (DebuffsAtPosition(actor.Position) != _debuffs[slot])
            hints.Add("Go to correct cell!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // TODO: implement
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (Active)
        {
            var color = Colors.SafeFromAOE;
            foreach (var c in SafeCenters(_debuffs[pcSlot]))
                Arena.ZoneRect(c, new WDir(1f, 0f), _shape.HalfWidth, _shape.HalfWidth, _shape.HalfWidth, color);
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var debuff = status.ID switch
        {
            (uint)SID.GildedFate => status.Extra,
            (uint)SID.SilveredFate => status.Extra << 16,
            _ => 0
        };

        if (debuff == 0)
            return;
        var slot = Raid.FindSlot(actor.InstanceID);
        if (slot >= 0)
            _debuffs[slot] |= debuff;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        CasterList(spell)?.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        CasterList(spell)?.Remove(caster);
    }

    private List<Actor>? CasterList(ActorCastInfo spell) => spell.Action.ID switch
    {
        (uint)AID.NGoldenFlame or (uint)AID.SGoldenFlame => _goldenFlames,
        (uint)AID.NSilverFlame or (uint)AID.SSilverFlame => _silverFlames,
        _ => null
    };

    private int CastersHittingPosition(List<Actor> casters, WPos pos) => casters.Count(a => _shape.Check(pos, a.Position, a.CastInfo!.Rotation));
    private int DebuffsAtPosition(WPos pos) => CastersHittingPosition(_silverFlames, pos) | (CastersHittingPosition(_goldenFlames, pos) << 16);

    private IEnumerable<WPos> SafeCenters(int debuff)
    {
        var limit = Center + new WDir(20f, 20f);
        var first = Center - new WDir(15f, 15f);
        for (var x = first.X; x < limit.X; x += 10f)
            for (var z = first.Z; z < limit.Z; z += 10f)
                if (DebuffsAtPosition(new WPos(x, z)) == debuff)
                    yield return new(x, z);
    }
}

// note: actual spell targets location, but it seems to be incorrect...
// note: we can predict cast start during Regret actor spawn...
abstract class RackAndRuin(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(40f, 2.5f), 8);
sealed class NRackAndRuin(ModuleBase module) : RackAndRuin(module, (uint)AID.NRackAndRuin);
sealed class SRackAndRuin(ModuleBase module) : RackAndRuin(module, (uint)AID.SRackAndRuin);

abstract class NothingBesideRemains(ModuleBase module, uint aid) : Components.SpreadFromCastTargets(module, aid, 8f);
sealed class NNothingBesideRemains(ModuleBase module) : NothingBesideRemains(module, (uint)AID.NNothingBesideRemainsAOE);
sealed class SNothingBesideRemains(ModuleBase module) : NothingBesideRemains(module, (uint)AID.SNothingBesideRemainsAOE);
