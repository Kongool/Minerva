// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A33RedGirl;

sealed class ShockWhiteBlack(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ShockWhiteAOE, (uint)AID.ShockBlackAOE], 5f);
abstract class ShockWhiteBait(ModuleBase module, uint iconID, double delay) : Components.BaitAwayIcon(module, 5f, iconID, (uint)AID.ShockWhiteBait, delay)
{
    private readonly ArenaChanges _arena = module.FindComponent<ArenaChanges>()!;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_arena.NumWalls != 0 && IsBaitTarget(actor))
        {
            hints.Add("Intersect black walls!");
        }
    }
}
sealed class ShockWhiteBaitSlow(ModuleBase module) : ShockWhiteBait(module, (uint)IconID.ShockWhiteSlow, 10.1d);
sealed class ShockWhiteBaitFast(ModuleBase module) : ShockWhiteBait(module, (uint)IconID.ShockWhiteFast, 5.1d);

sealed class ShockBlackBait(ModuleBase module) : Components.BaitAwayIcon(module, 5f, (uint)IconID.ShockBlack, (uint)AID.ShockBlackBait, 10.1d)
{
    private readonly ArenaChanges _arena = module.FindComponent<ArenaChanges>()!;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_arena.NumWalls != 0 && IsBaitTarget(actor))
        {
            hints.Add("Intersect white walls!");
        }
    }
}
