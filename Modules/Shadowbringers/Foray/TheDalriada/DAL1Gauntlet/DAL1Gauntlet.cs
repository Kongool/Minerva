// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL1Gauntlet;

sealed class NihilitysSong(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.NihilitysSong);
sealed class SanctifiedQuakeIII(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SanctifiedQuakeIII);
sealed class BroadsideBarrage(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BroadsideBarrage, new AOEShapeRect(40f, 20f));
sealed class SurfaceMissile(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SurfaceMissile, 6f);
sealed class CeruleumExplosion(ModuleBase module) : Components.CastHint(module, (uint)AID.CeruleumExplosion, "Enrage!", true);
sealed class FlamingCyclone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlamingCyclone, 10f);
sealed class SeventyFourDegrees(ModuleBase module) : Components.DonutStack(module, (uint)AID.SeventyFourDegrees, (uint)IconID.SeventyFourDegrees, 4f, 8f, 9f);

[ModuleInfo(Group = ModuleGroup.TheDalriada, GroupID = 778u, CFCID = 778u, NameID = 10212u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class DAL1Gauntlet(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(222f, -689f), new ArenaBoundsSquare(29.5f))
{
    public Actor? BossAugur;
    public Actor? BossAlkonost;
    public Actor? BossCrow;

    protected override void UpdateModule()
    {
        BossAugur ??= GetActor((uint)OID.ForthLegionAugur1);
        if (StateMachine.ActivePhaseIndex >= 1)
        {
            BossAlkonost ??= GetActor((uint)OID.TamedAlkonost);
            BossCrow ??= GetActor((uint)OID.TamedCrow);
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        switch (StateMachine.ActivePhaseIndex)
        {
            case -1:
            case 0:
                Arena.Actor(PrimaryActor);
                Arena.Actors(Enemies((uint)OID.ForthLegionInfantry));
                break;
            case 1:
                Arena.Actor(BossAugur);
                Arena.Actors(Enemies((uint)OID.WaveborneZirnitra));
                Arena.Actors(Enemies((uint)OID.FlameborneZirnitra));
                break;
            case 2:
                Arena.Actor(BossAlkonost);
                Arena.Actor(BossCrow);
                break;
        }
    }
}
