// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex4Zelenia;

sealed class ThornyVine(ModuleBase module) : Components.Chains(module, (uint)TetherID.ThornyVine, default, 25f)
{
    private readonly Emblazon _emblazon = module.FindComponent<Emblazon>()!;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (!TethersAssigned && _emblazon.WedgeCenterDirection != default)
        {
            Arena.ZoneCircleOutline(Center - 5f * _emblazon.WedgeCenterDirection, 3f, Colors.Safe);
        }
        base.DrawArenaForeground(pcSlot, pc);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!TethersAssigned && _emblazon.WedgeCenterDirection != default)
        {
            hints.Add("Meet at marked spot for chains!");
        }
        else
        {
            base.AddHints(slot, actor, hints);
        }
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        base.OnUntethered(source, tether);
        if (tether.ID == (uint)TetherID.ThornyVine)
        {
            ++NumCasts;
        }
    }
}
