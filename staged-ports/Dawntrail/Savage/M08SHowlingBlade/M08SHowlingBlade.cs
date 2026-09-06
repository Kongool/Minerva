// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M08SHowlingBlade;

sealed class ExtraplanarPursuit(ModuleBase module) : Components.CastCounter(module, (uint)AID.ExtraplanarPursuit);
sealed class TitanicPursuit(ModuleBase module) : Components.CastCounter(module, (uint)AID.TitanicPursuit);
sealed class HowlingHavoc(ModuleBase module) : Components.CastCounter(module, (uint)AID.HowlingHavoc);
sealed class GreatDivide(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.GreatDivide, new AOEShapeRect(60f, 3f));
sealed class RavenousSaber(ModuleBase module) : Components.CastCounterMulti(module, [(uint)AID.RavenousSaber1,
(uint)AID.RavenousSaber2, (uint)AID.RavenousSaber3, (uint)AID.RavenousSaber4, (uint)AID.RavenousSaber5]);
sealed class Mooncleaver1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Mooncleaver1, 8f);
sealed class ProwlingGaleP2(ModuleBase module) : Components.CastTowers(module, (uint)AID.ProwlingGaleP2, 2f, 2, 2);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1026u, CFCID = 1026u, NameID = 13843u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M08SHowlingBlade(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, StartingArena)
{
    private Actor? _bossP2;
    public Actor? BossP2() => _bossP2;

    protected override void UpdateModule()
    {
        if (StateMachine.ActivePhaseIndex == 1)
        {
            _bossP2 ??= GetActor((uint)OID.BossP2);
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_bossP2);
    }

    public static readonly WPos ArenaCenter = new(100f, 100f);
    public static readonly Polygon[] StartingArenaPolygon = [new(ArenaCenter, 12f, 40)];
    public static readonly ArenaBoundsCustom StartingArena = new(StartingArenaPolygon, MapResolution: 0.25f);
    public static readonly ArenaBoundsCustom DonutArena = new(StartingArenaPolygon, [new Polygon(ArenaCenter, 8f, 40)]);
}