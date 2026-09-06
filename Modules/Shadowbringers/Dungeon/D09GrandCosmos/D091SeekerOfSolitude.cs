// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Dungeon.D09GrandCosmos.D091SeekerOfSolitude;

public enum OID : uint
{
    Boss = 0x2C1A, // R2.0
    MagickedBroom = 0x2C1B, // R3.125
    SweepVoidzone = 0x1EAEAE, // R0.5
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 18280, // Boss->player, no cast, single-target

    Shadowbolt = 18281, // Boss->player, 4.0s cast, single-target
    ImmortalAnathema = 18851, // Boss->self, 4.0s cast, range 60 circle

    TribulationVisual = 18283, // Boss->self, 3.0s cast, single-target
    Tribulation = 18852, // Helper->location, 3.0s cast, range 3 circle

    Sweep = 18288, // Helper->player, no cast, single-target

    DarkShockVisual = 18286, // Boss->self, 3.0s cast, single-target
    DarkShock = 18287, // Helper->location, 3.0s cast, range 6 circle
    DeepClean = 18289, // Helper->player, no cast, single-target
    DarkPulse = 18282, // Boss->players, 5.0s cast, range 6 circle, stack
    DarkWellVisual = 18284, // Boss->self, no cast, single-target
    DarkWell = 18285, // Helper->player, 5.0s cast, range 5 circle, spread
    MovementMagick = 18713 // Boss->self, 3.0s cast, single-target
}

class ImmortalAnathema(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ImmortalAnathema);
class Shadowbolt(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Shadowbolt);
class Tribulation(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 6f, (uint)AID.Tribulation, GetVoidzones, 0.1f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.SweepVoidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

class DarkPulse(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DarkPulse, 6f, 4, 4);
class DarkWell(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.DarkWell, 5f);
class DarkShock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkShock, 6f);
class MagickedBroom(ModuleBase module) : Components.Voidzone(module, 3.125f, GetVoidzones, 10f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.MagickedBroom);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.ModelState.AnimState1 == 1)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

class D091SeekerOfSolitudeStates : StateMachineBuilder
{
    public D091SeekerOfSolitudeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ImmortalAnathema>()
            .ActivateOnEnter<Shadowbolt>()
            .ActivateOnEnter<Tribulation>()
            .ActivateOnEnter<DarkPulse>()
            .ActivateOnEnter<DarkWell>()
            .ActivateOnEnter<DarkShock>()
            .ActivateOnEnter<MagickedBroom>();
    }
}

[ModuleInfo(CFCID = 692u, NameID = 9041u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D091SeekerOfSolitude(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(default, 187f), new ArenaBoundsRect(20.5f, 14.5f));
