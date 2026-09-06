// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P7SAgdistis;

// TODO: improve!
class ForbiddenFruit5(ModuleBase module) : ForbiddenFruitCommon(module, (uint)AID.Burst)
{
    private readonly List<Actor> _towers = module.Enemies((uint)OID.Tower);

    private const float _towerRadius = 5;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var tetherSource = TetherSources[pcSlot];
        if (tetherSource != null)
            Arena.AddLine(tetherSource.Position, pc.Position, TetherColor(tetherSource));

        for (var i = 0; i < _towers.Count; ++i)
            Arena.ZoneCircleOutline(_towers[i].Position, _towerRadius, tetherSource == null ? Colors.Safe : 0);
    }
}
