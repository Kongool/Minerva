// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T03QueenEternal;

sealed class Besiegement(ModuleBase module) : Components.GenericAOEs(module)
{
    private const float L = 60f;
    public readonly List<AOEInstance> AOEs = [];
    private static readonly AOEShapeRect[] rects = [new(L, 2f), new(L, 4f), new(L, 5f), new(L, 6f), new(L, 9f)];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnMapEffect(byte index, uint state)
    {
        if (index != 0x03)
        {
            return;
        }

        (int, float)[] aoes = [];
        var check = NumCasts != 1;

        switch (state)
        {
            case 0x00200010u:
                aoes = check ? [(3, 114f), (1, 90f), (2, 101f), (0, 82f)] : [(1, 116f), (1, 94f), (2, 105f), (1, 84f)];
                break;

            case 0x02000100u:
                aoes = check ? [(2, 95f), (4, 111f), (1, 84f)] : [];
                break;

            case 0x08000400u:
                aoes = check ? [(1, 116f), (4, 101f), (2, 85f)] : [];
                break;

            case 0x00800040u:
                aoes = check ? [(1, 116f), (2, 105f), (1, 94f), (1, 84f)] : [(3, 114f), (2, 101f), (1, 90f), (0, 82f)];
                break;
        }

        var len = aoes.Length;
        if (len > 0)
        {
            ++NumCasts;
            var act = World.FutureTime(6.6d);
            for (var i = 0; i < len; ++i)
            {
                var aoe = aoes[i];
                AOEs.Add(new(rects[aoe.Item1], new WPos(aoe.Item2, 80f).Quantized(), Angle.AnglesCardinals[1], act));
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (AOEs.Count != 0)
        {
            switch (spell.Action.ID)
            {
                case (uint)AID.Besiegement1:
                case (uint)AID.Besiegement2:
                case (uint)AID.Besiegement3:
                case (uint)AID.Besiegement4:
                case (uint)AID.Besiegement5:
                    AOEs.Clear();
                    break;
            }
        }
    }
}
