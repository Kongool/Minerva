// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A12Fafnir;

sealed class DarkMatterBlast(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DarkMatterBlast)
{
    public override bool KeepOnPhaseChange => true;
}

sealed class HurricaneWingRW(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.HurricaneWingRaidwide, (uint)AID.HurricaneWingRaidwideAOE1, 2.7d, "Raidwide x9")
{
    public override bool KeepOnPhaseChange => true;
}

sealed class PestilentSphere(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.PestilentSphere)
{
    public override bool KeepOnPhaseChange => true;
}

sealed class ShudderingEarth(ModuleBase module) : Components.CastCounter(module, (uint)AID.ShudderingEarth);

sealed class Darter(ModuleBase module) : Components.Adds(module, (uint)OID.Darter, 1)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class Venom(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Venom, new AOEShapeCone(30f, 60f.Degrees()))
{
    public override bool KeepOnPhaseChange => true;
}

sealed class AbsoluteTerror(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbsoluteTerrorAOE, new AOEShapeRect(70f, 10f))
{
    public override bool KeepOnPhaseChange => true;
}

sealed class WingedTerror(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WingedTerrorAOE, new AOEShapeRect(70f, 12.5f))
{
    public override bool KeepOnPhaseChange => true;
}

sealed class BalefulBreath(ModuleBase module) : Components.LineStack(module, (uint)IconID.BalefulBreath, (uint)AID.BalefulBreathAOERest, 8.2d, 70f, 3f, PartyState.MaxAllianceSize, PartyState.MaxAllianceSize, 3, false)
{
    public override bool KeepOnPhaseChange => true;
}

sealed class SharpSpike(ModuleBase module) : Components.BaitAwayIcon(module, 4f, (uint)IconID.SharpSpike, (uint)AID.SharpSpikeAOE, 6.2d)
{
    public override bool KeepOnPhaseChange => true;
}

[ModuleInfo(CFCID = 1015u, NameID = 13662u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A12Fafnir(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsCircle(34.5f))
{
    public static readonly WPos ArenaCenter = new(-500f, 600f);
    public static readonly ArenaBoundsCircle DefaultBounds = new(30f);
    public static readonly ArenaBoundsCircle FireArena = new(16f);
}
