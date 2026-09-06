// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Extreme.FTME3Necrophobia;

[ModuleInfo(Group = ModuleGroup.TheForkedTowerMagic, CFCID = 1114u, NameID = 14503u, PrimaryActorOID = (uint)OID.Necrophobia, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = " (ported from BMR)")]
[SkipLocalsInit]
public sealed class FTME3Necrophobia(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 800f), new ArenaBoundsCircle(24f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Module.Center, 24f);
}
