// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P1SErichthonios;

[ModuleInfo(CFCID = 809u, NameID = 10576u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class P1S(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsSquare(20))
{
    public const float InnerCircleRadius = 12; // this determines in/out flails and cells boundary

    protected override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (Bounds is ArenaBoundsCircle)
        {
            // cells mode
            var diag = Bounds.Radius / 1.414214f;
            Arena.ZoneCircleOutline(Center, InnerCircleRadius, Colors.Border);
            Arena.AddLine(Center + new WDir(Bounds.Radius, 0), Center - new WDir(Bounds.Radius, 0), Colors.Border);
            Arena.AddLine(Center + new WDir(0, Bounds.Radius), Center - new WDir(0, Bounds.Radius), Colors.Border);
            Arena.AddLine(Center + new WDir(diag, +diag), Center - new WDir(diag, +diag), Colors.Border);
            Arena.AddLine(Center + new WDir(diag, -diag), Center - new WDir(diag, -diag), Colors.Border);
        }
    }
}
