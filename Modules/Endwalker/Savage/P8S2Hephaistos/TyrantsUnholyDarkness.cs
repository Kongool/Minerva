// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S2;

class TyrantsUnholyDarkness(ModuleBase module) : Components.CastCounter(module, (uint)AID.TyrantsUnholyDarknessAOE)
{
    private const float _radius = 6;

    // TODO: we need some sort of a threat info in worldstate to determine targets properly...
    public bool IsTarget(Actor actor) => actor.Role == Role.Tank;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (IsTarget(actor))
        {
            hints.Add("GTFO from raid!", Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, _radius).Any());
        }
        else if (Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, _radius).Any(IsTarget))
        {
            hints.Add("GTFO from tanks!");
        }
    }

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor) => IsTarget(player) ? PlayerPriority.Danger : PlayerPriority.Irrelevant;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var target in Raid.WithoutSlot(false, true, true).Where(IsTarget))
            Arena.ZoneCircleOutline(target.Position, _radius, Colors.Danger);
    }
}
