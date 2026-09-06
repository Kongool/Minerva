// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex3QueenEternal;

sealed class LegitimateForce(ModuleBase module) : Components.GenericAOEs(module)
{
    public readonly List<AOEInstance> AOEs = [];
    private static readonly AOEShapeRect rect = new(20f, 40f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = AOEs.Count;
        if (count == 0)
            return [];
        var aoes = CollectionsMarshal.AsSpan(AOEs);
        if (count == 1)
        {
            aoes[0].Risky = true;
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.LegitimateForceFirstR:
                AddAOEs(-90f, 90f);
                break;
            case (uint)AID.LegitimateForceFirstL:
                AddAOEs(90f, -90f);
                break;
        }
        void AddAOEs(float first, float second)
        {
            AddAOE(first);
            AddAOE(second, 3.1d, false); // intentionally caster.Position here, since these are not the actual aoe spell casts
            void AddAOE(float offset, double delay = default, bool first = true) => AOEs.Add(new(rect, caster.Position, spell.Rotation + offset.Degrees(), Module.CastFinishAt(spell, delay), first ? Colors.Danger : default, first));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.LegitimateForceFirstL:
            case (uint)AID.LegitimateForceFirstR:
            case (uint)AID.LegitimateForceSecondL:
            case (uint)AID.LegitimateForceSecondR:
                ++NumCasts;
                if (AOEs.Count != 0)
                    AOEs.RemoveAt(0);
                break;
        }
    }
}
