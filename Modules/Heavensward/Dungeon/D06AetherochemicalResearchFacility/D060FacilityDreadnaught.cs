// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Dungeon.D06AetherochemicalResearchFacility.D060FacilityDreadnaught;

public enum OID : uint
{
    Boss = 0xF54, // R3.0
    MonitoringDrone = 0xF55 // R2.4
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    AutoCannons = 4825, // MonitoringDrone->self, 3.0s cast, range 40+R width 5 rect
    Rotoswipe = 4556, // Boss->self, 3.0s cast, range 8+R 120-degree cone
    WreckingBall = 4557 // Boss->location, 4.0s cast, range 8 circle
}

class Rotoswipe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rotoswipe, new AOEShapeCone(11f, 60f.Degrees()));
class AutoCannons(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AutoCannons, new AOEShapeRect(42.4f, 2.5f));
class WreckingBall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WreckingBall, 8f);

class D060FacilityDreadnaughtStates : StateMachineBuilder
{
    public D060FacilityDreadnaughtStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Rotoswipe>()
            .ActivateOnEnter<AutoCannons>()
            .ActivateOnEnter<WreckingBall>()
            .Raw.Update = () => AllDeadOrDestroyed(D060FacilityDreadnaught.Trash);
    }
}

[ModuleInfo(CFCID = 38u, NameID = 3836u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D060FacilityDreadnaught(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-360f, -250f), 9f, 6)]);
    public static readonly uint[] Trash = [(uint)OID.Boss, (uint)OID.MonitoringDrone];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, Trash);
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
