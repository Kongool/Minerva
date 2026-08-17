// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Dungeon.D04Vanguard.D040VanguardAerostat1;

public enum OID : uint
{
    Boss = 0x41DA, //R=2.3
    Aerostat2 = 0x447B //R=2.3
}

public enum AID : uint
{
    AutoAttack = 871, // Boss/Aerostat2->player, no cast, single-target

    IncendiaryRing = 38452 // Aerostat2->self, 4.8s cast, range 3-12 donut
}

sealed class IncendiaryRing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IncendiaryRing, new AOEShapeDonut(3, 12));

sealed class D040VanguardAerostat1States : StateMachineBuilder
{
    public D040VanguardAerostat1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<IncendiaryRing>()
            .Raw.Update = () => AllDeadOrDestroyed(D040VanguardAerostat1.Trash);
    }
}

[ModuleInfo(CFCID = 831u, NameID = 12780u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class D040VanguardAerostat1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-50f, -15f), new ArenaBoundsRect(7.7f, 25f))
{
    public static readonly uint[] Trash = [(uint)OID.Boss, (uint)OID.Aerostat2];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Aerostat2));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            hints.PotentialTargets[i].Priority = 0;
        }
    }
}
