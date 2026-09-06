// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M04NWickedThunder;

sealed class StampedingThunder(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect rect = new(40f, 15f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var id = spell.Action.ID;
        if (id == (uint)AID.StampedingThunderVisualWest)
        {
            AddAOE(new(95f, 80f));
        }
        else if (id == (uint)AID.StampedingThunderVisualEast)
        {
            AddAOE(new(105f, 80f));
        }
        void AddAOE(WPos position) => _aoe = [new(rect, position.Quantized(), caster.Rotation, World.FutureTime(9.4d))];
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x00 && state is 0x00200010u or 0x00020001u)
        {
            _aoe = [];
        }
    }
}

sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    public static readonly ArenaBoundsSquare DefaultBounds = new(20f);
    public static readonly WPos DefaultCenter = new(100f, 100f);
    public static readonly WPos WestRemovedCenter = new(115f, 100f);
    public static readonly WPos EastremovedCenter = new(85f, 100f);
    private static readonly ArenaBoundsRect damagedPlatform = new(5f, 20f);

    public override void OnMapEffect(byte index, uint state)
    {
        // index 0x00
        // 0x00200010 - west 3/4 disappears
        // 0x00020001 - east 3/4 disappears
        if (index == 0x00)
        {
            switch (state)
            {
                case 0x00020001u:
                    Bounds = damagedPlatform;
                    Center = EastremovedCenter;
                    break;
                case 0x00200010u:
                    Bounds = damagedPlatform;
                    Center = WestRemovedCenter;
                    break;
                case 0x00400004u:
                case 0x00800004u:
                    Bounds = DefaultBounds;
                    Center = DefaultCenter;
                    break;
            }
        }
    }
}
