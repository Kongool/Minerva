// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class P4UltimateAnnihilation(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _orbs = module.Enemies((uint)OID.Aetheroplasm);

    private const float _radius = 6f;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var orb in _orbs.Where(o => !o.IsDead))
        {
            Arena.Actor(orb, Colors.Object, true);
            Arena.ZoneCircleOutline(orb.Position, _radius, Colors.Object);
        }
    }
}
