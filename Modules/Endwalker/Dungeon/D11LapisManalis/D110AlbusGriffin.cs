// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D11LapisManalis.D110AlbusGriffin;

public enum OID : uint
{
    CaladriusMaturus = 0x3D56, //R=3.96
    Caladrius = 0x3CE2, //R=1.8
    AlbusGriffin = 0x3E9F, //R=4.6
}

public enum AID : uint
{
    AutoAttack1 = 872, // Caladrius/CaladriusMaturus->player, no cast, single-target
    AutoAttack2 = 870, // AlbusGriffin->player, no cast, single-target

    TransonicBlast = 32535, // Caladrius->self, 4.0s cast, range 9 90-degree cone
    WindsOfWinter = 32785, // AlbusGriffin->self, 5.0s cast, range 40 circle
    Freefall = 32786, // AlbusGriffin->location, 3.5s cast, range 8 circle
    GoldenTalons = 32787, // AlbusGriffin->self, 4.5s cast, range 8 90-degree cone
}

sealed class TransonicBlast(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TransonicBlast, new AOEShapeCone(9f, 45f.Degrees()));
sealed class WindsOfWinter(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.WindsOfWinter);
sealed class WindsOfWinterStunHint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.WindsOfWinter, false, true);
sealed class GoldenTalons(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GoldenTalons, new AOEShapeCone(8f, 45f.Degrees()));
sealed class Freefall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Freefall, 8f);

sealed class D110AlbusGriffinStates : StateMachineBuilder
{
    public D110AlbusGriffinStates(D110AlbusGriffin module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TransonicBlast>()
            .Raw.Update = () => AllDeadOrDestroyed(D110AlbusGriffin.TrashP1);
        TrivialPhase(1u)
            .ActivateOnEnter<Freefall>()
            .ActivateOnEnter<WindsOfWinter>()
            .ActivateOnEnter<WindsOfWinterStunHint>()
            .ActivateOnEnter<GoldenTalons>()
            .Raw.Update = () => AllDestroyed(D110AlbusGriffin.TrashP1) && (module.BossAlbusGriffin?.IsDeadOrDestroyed ?? true);
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 896u, CFCID = 896u, NameID = 12245u, PrimaryActorOID = (uint)OID.CaladriusMaturus, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class D110AlbusGriffin(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(47f, -570.5f), new ArenaBoundsRect(8.5f, 11.5f))
{
    public static readonly uint[] TrashP1 = [(uint)OID.CaladriusMaturus, (uint)OID.Caladrius];
    public Actor? BossAlbusGriffin;

    protected override void UpdateModule()
    {
        BossAlbusGriffin ??= GetActor((uint)OID.AlbusGriffin);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Caladrius));
        Arena.Actors(Enemies((uint)OID.AlbusGriffin));
    }
}
