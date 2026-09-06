// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

// component for third lightwave (safe zone)
class Lightwave3(ModuleBase module) : LightwaveCommon(module)
{
    public override void Update()
    {
        // try to find two helpers with Z=70 before first cast
        if (Waves.Count == 0)
        {
            foreach (var wave in Module.Enemies((uint)OID.Helper).Where(IsInitialLightwave))
            {
                Waves.Add(wave);
            }
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Waves.Count == 0)
            return;

        if (Waves.Any(w => WaveAOE.Check(actor.Position, w)))
            hints.Add("GTFO from wave!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (Waves.Count == 0)
            return;

        foreach (var wave in Waves)
            WaveAOE.Draw(Arena, wave);
    }

    private bool IsInitialLightwave(Actor a)
    {
        var pos = a.Position;
        return Math.Abs(pos.X - 70) < 1 || Math.Abs(pos.X - 130) < 1 || Math.Abs(pos.Z - 70) < 1 || Math.Abs(pos.Z - 130) < 1;
    }
}
