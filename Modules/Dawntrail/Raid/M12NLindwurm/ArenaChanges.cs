// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M12NLindwurm;

[SkipLocalsInit]
sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    private Rectangle[] _rects = [];
    private readonly List<Polygon> _polygons = [];

    public override void OnMapEffect(byte index, uint state)
    {
        if (index != 0x00)
        {
            return;
        }

        switch (state)
        {
            // arena break after Bring Down The House
            case 0x00020001u:
                SetBrokenArena([new(85f, 107.5f), new(95f, 92.5f), new(105f, 107.5f), new(115f, 92.5f)]);
                break;

            // other set of tiles break
            case 0x02000100u:
                SetBrokenArena([new(85f, 92.5f), new(95f, 107.5f), new(105f, 92.5f), new(115f, 107.5f)]);
                break;

            // arena partial fix through middle
            case 0x00200010u:
            case 0x08000400u:
                Array.Resize(ref _rects, 5);
                _rects[4] = new Rectangle(Center, 20f, 5f);
                UpdateArena();
                break;

            // arena reset
            case 0x10000004u:
            case 0x00080004u:
            case 0x00800004u:
            case 0x80000004u:
                Bounds = new ArenaBoundsRect(20f, 15f);
                break;
        }

        void SetBrokenArena(WPos[] positions)
        {
            _rects = new Rectangle[4];
            _polygons.Clear();
            for (var i = 0; i < 4; ++i)
            {
                _rects[i] = new(positions[i], 5f, 7.5f);
            }
            UpdateArena();
        }

        void UpdateArena() => Bounds = new ArenaBoundsCustom(_rects, Offset: -1f);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.GrandEntrance1 or (uint)AID.GrandEntrance2)
        {
            _polygons.Add(new(caster.Position, 2f, 16));
            Bounds = new ArenaBoundsCustom([new Rectangle(Center, 20f, 15f)], [.. _polygons]);
        }
    }
}
