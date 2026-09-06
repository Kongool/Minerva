// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS8Queen;

// TODO: show reflect hints, show stay under dome hints
sealed class MaelstromsBolt(ModuleBase module) : Components.CastCounter(module, (uint)AID.MaelstromsBoltAOE)
{
    private readonly List<Actor> _ballLightnings = module.Enemies((uint)OID.BallLightning);
    private readonly List<Actor> _domes = module.Enemies((uint)OID.ProtectiveDome);

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var b in _ballLightnings.Where(b => !b.IsDead))
        {
            Arena.Actor(b, Colors.Object, true);
            Arena.ZoneCircleOutline(b.Position, 8, Colors.Object);
        }
        for (var i = 0; i < _domes.Count; ++i)
            Arena.ZoneCircleOutline(_domes[i].Position, 8, Colors.Safe);
    }
}
