// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex3QueenEternal;

sealed class ProsecutionOfWar(ModuleBase module) : Components.TankSwap(module, (uint)AID.ProsecutionOfWar, (uint)AID.ProsecutionOfWar, (uint)AID.ProsecutionOfWarAOE, default, 3.1d);
sealed class DyingMemory(ModuleBase module) : Components.CastCounter(module, (uint)AID.DyingMemory);
sealed class DyingMemoryLast(ModuleBase module) : Components.CastCounter(module, (uint)AID.DyingMemoryLast);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1017u, CFCID = 1017u, NameID = 13029u, PrimaryActorOID = (uint)OID.BossP1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Ex3QueenEternal(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f))
{
    public static Rectangle[] GetIceRects() => [new(new(112f, 95f), 4f, 15f), new(new(88f, 95f), 4f, 15f), new(new(100f, 100f), 2f, 10f)];
    public static Rectangle[] GetAllIceRects() => [.. GetIceRects(), new(new(100f, 96f), 8f, 2f), new(new(100f, 104f), 8f, 2f)];

    private Actor? _bossP2;
    public Actor? BossP1() => PrimaryActor;
    public Actor? BossP2() => _bossP2;

    protected override void UpdateModule()
    {
        _bossP2 ??= GetActor((uint)OID.BossP2);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_bossP2);
    }
}
