// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL3SaunionDawon;

sealed class HighPoweredMagitekRay(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.HighPoweredMagitekRay, 5f, tankbuster: true)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class ToothAndTalon(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ToothAndTalon);
sealed class Pentagust(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pentagust, new AOEShapeCone(50f, 10f.Degrees()));
sealed class SurfaceMissile(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SurfaceMissile, 6f)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class SwoopingFrenzy(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SwoopingFrenzy, 12f);
sealed class MissileSalvo(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.MissileSalvo, 6f)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class MagitekCrossray(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekCrossray, MobileHaloCrossray.Cross);
sealed class MagitekHalo(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekHalo, MobileHaloCrossray.Donut);
sealed class FrigidPulse(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FrigidPulse, Obey.Donut);
sealed class AntiPersonnelMissile(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.AntiPersonnelMissile, 6f)
{
    public override bool KeepOnPhaseChange => true;
}
sealed class Touchdown(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Touchdown1, 30f, stopAtWall: true);
sealed class WildfireWinds(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.WildfireWinds1);

[ModuleInfo(Group = ModuleGroup.TheDalriada, CFCID = 778u, NameID = 10192u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DAL3SaunionDawon(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(650f, -659f), new ArenaBoundsSquare(26.5f))
{
    public Actor? BossDawon;

    protected override void UpdateModule()
    {
        BossDawon ??= GetActor((uint)OID.Dawon);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(BossDawon);
        Arena.Actor(PrimaryActor);
    }
}
