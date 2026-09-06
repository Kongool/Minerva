// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.TreasureHunt.ShiftingGymnasionAgonon.LampasChrysine;

public enum OID : uint
{
    Boss = 0x3D40, //R=6
    GymnasiouLampas = 0x3D4D, //R=2.001
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 32287, // Boss->player, no cast, single-target

    AetherialLightVisual = 32293, // Boss->self, 1.3s cast, single-target
    AetherialLight = 32294, // Helper->self, 3.0s cast, range 40 60-degree cone
    unknown = 32236, // Boss->self, no cast, single-target, seems to be connected to Aetherial Light
    LightburstVisual = 32289, // Boss->self, 3.3s cast, single-target
    Lightburst = 32290, // Helper->player, 5.0s cast, single-target
    ShineVisual = 32291, // Boss->self, 1.3s cast, single-target
    Shine = 32292, // Helper->location, 3.0s cast, range 5 circle
    Summon = 32288, // Boss->self, 1.3s cast, single-target, spawns bonus loot adds
    Telega = 9630 // GymnasiouLampas->self, no cast, single-target, bonus loot add despawn
}

class Shine(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shine, 5f);

class AetherialLight : Components.SimpleAOEs
{
    public AetherialLight(ModuleBase module) : base(module, (uint)AID.AetherialLight, new AOEShapeCone(40f, 30f.Degrees()), 4) { MaxDangerColor = 2; }
}

class Lightburst(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Lightburst);
class Summon(ModuleBase module) : Components.CastHint(module, (uint)AID.Summon, "Calls bonus adds");

class LampasChrysineStates : StateMachineBuilder
{
    public LampasChrysineStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Shine>()
            .ActivateOnEnter<AetherialLight>()
            .ActivateOnEnter<Lightburst>()
            .ActivateOnEnter<Summon>()
            .Raw.Update = () => AllDeadOrDestroyed(LampasChrysine.All);
    }
}

[ModuleInfo(CFCID = 909u, NameID = 12021u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class LampasChrysine(WorldState ws, Actor primary) : THTemplate(ws, primary)
{
    public static readonly uint[] All = [(uint)OID.Boss, (uint)OID.GymnasiouLampas];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.GymnasiouLampas), Colors.Vulnerable);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.GymnasiouLampas => 1,
                _ => 0
            };
        }
    }
}
