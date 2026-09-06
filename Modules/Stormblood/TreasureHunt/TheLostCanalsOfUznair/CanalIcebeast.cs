// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.TreasureHunt.LostCanalsOfUznair.CanalIcebeast;

public enum OID : uint
{
    Boss = 0x1F15, //R=7.5
    CanalVindthurs = 0x1F0E, // R1.56
    CanalIceHomunculus = 0x1F0F, // R1.6
    Abharamu = 0x1EBF, // R3.42
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack1 = 870, // Boss->player, no cast, single-target
    AutoAttack2 = 872, // Abharamu/CanalVindthurs->player, no cast, single-target

    FrumiousJaws = 9556, // Boss->player, no cast, single-target
    AbsoluteZero = 9558, // Boss->self, 5.0s cast, range 38+R 90-degree cone
    Blizzard = 967, // CanalIceHomunculus->player, 1.0s cast, single-target
    Eyeshine = 9557, // Boss->self, 4.0s cast, range 38+R circle, gaze
    Freezeover = 4486, // CanalVindthurs->location, 3.0s cast, range 3 circle
    PlainPound = 4487, // CanalVindthurs->self, 3.0s cast, range 3+R circle

    AbharamuActivate = 9636, // Abharamu->self, no cast, single-target
    Spin = 8599, // Abharamu->self, no cast, range 6+R 120-degree cone
    RaucousScritch = 8598, // Abharamu->self, 2.5s cast, range 5+R 120-degree cone
    Hurl = 5352, // Abharamu->location, 3.0s cast, range 6 circle
    Telega = 9630 // Abharamu->self, no cast, single-target, bonus adds disappear
}

class Eyeshine(ModuleBase module) : Components.CastGaze(module, (uint)AID.Eyeshine);
class AbsoluteZero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbsoluteZero, new AOEShapeCone(45.5f, 45f.Degrees()));
class Freezeover(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Freezeover, 6f);
class PlainPound(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PlainPound, 4.56f);
class RaucousScritch(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RaucousScritch, new AOEShapeCone(8.42f, 60f.Degrees()));
class Hurl(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Hurl, 6f);
class Spin(ModuleBase module) : Components.Cleave(module, (uint)AID.Spin, new AOEShapeCone(9.42f, 60f.Degrees()), [(uint)OID.Abharamu]);

class CanalIcebeastStates : StateMachineBuilder
{
    public CanalIcebeastStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Eyeshine>()
            .ActivateOnEnter<AbsoluteZero>()
            .ActivateOnEnter<Freezeover>()
            .ActivateOnEnter<PlainPound>()
            .ActivateOnEnter<Hurl>()
            .ActivateOnEnter<RaucousScritch>()
            .ActivateOnEnter<Spin>()
            .Raw.Update = () => AllDeadOrDestroyed(CanalIcebeast.All);
    }
}

[ModuleInfo(CFCID = 268u, NameID = 6650u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class CanalIcebeast(WorldState ws, Actor primary) : FinalRoomArena(ws, primary)
{
    private static readonly uint[] trash = [(uint)OID.CanalVindthurs, (uint)OID.CanalIceHomunculus];
    public static readonly uint[] All = [(uint)OID.Boss, (uint)OID.Abharamu, .. trash];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(this, trash);
        Arena.Actors(Enemies((uint)OID.Abharamu), Colors.Vulnerable);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.Abharamu => 2,
                (uint)OID.CanalVindthurs or (uint)OID.CanalIceHomunculus => 1,
                _ => 0
            };
        }
    }
}
