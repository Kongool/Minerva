// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Trial.T02TitanN;

public enum OID : uint
{
    Boss = 0xF6, // x1
    TitansHeart = 0x5E3, // Part type, spawn during fight
    GraniteGaol = 0x5A4, // spawn during fight
    Helper = 0x1B2
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast
    Tumult = 642, // Boss->self, no cast, raidwide
    RockBuster = 641, // Boss->self, no cast, range 11.25 ?-degree cone cleave
    Geocrush = 651, // Boss->self, 4.5s cast, range 27 aoe with ? falloff
    Landslide = 650, // Boss->self, 3.0s cast, range 40.25 width 6 rect aoe with knockback 15
    RockThrow = 645, // Boss->player, no cast, visual for granite gaol spawn
    GraniteSepulchre = 28799, // GraniteGaol->self, 15.0s cast, oneshot target if gaol not killed
    EarthenFury = 652, // Boss->self, no cast, wipe if heart not killed, otherwise just a raidwide
    WeightOfTheLand = 644, // Boss->self, 3.0s cast, visual
    WeightOfTheLandAOE = 973 // Helper->location, 3.5s cast, range 6 puddle
}

class Hints(ModuleBase module) : ModuleComponent(module)
{
    private DateTime _heartSpawn;

    public override void AddGlobalHints(GlobalHints hints)
    {
        var heartExists = ((T02TitanN)Module).ActiveHeart.Any();
        if (_heartSpawn == default && heartExists)
        {
            _heartSpawn = World.CurrentTime;
        }
        if (_heartSpawn != default && heartExists)
        {
            hints.Add($"Heart enrage in: {Math.Max(62 - (World.CurrentTime - _heartSpawn).TotalSeconds, 0.0f):f1}s");
        }
    }
}

class RockBuster(ModuleBase module) : Components.Cleave(module, (uint)AID.RockBuster, new AOEShapeCone(11.25f, 60.Degrees())); // TODO: verify angle
class Geocrush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Geocrush, 18); // TODO: verify falloff
class Landslide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Landslide, new AOEShapeRect(40, 3));
class WeightOfTheLand(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WeightOfTheLandAOE, 6);

class T02TitanNStates : StateMachineBuilder
{
    public T02TitanNStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Hints>()
            .ActivateOnEnter<RockBuster>()
            .ActivateOnEnter<Geocrush>()
            .ActivateOnEnter<Landslide>()
            .ActivateOnEnter<WeightOfTheLand>();
    }
}

[ModuleInfo(CFCID = 57u, NameID = 1801u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class T02TitanN : ModuleBase
{
    private readonly List<Actor> _heart;
    public IEnumerable<Actor> ActiveHeart => _heart.Where(h => h.IsTargetable && !h.IsDead);

    public T02TitanN(WorldState ws, Actor primary) : base(ws, primary, default, new ArenaBoundsCircle(20)) // note: initial area is size 25, but it becomes smaller at 75%
    {
        _heart = Enemies((uint)OID.TitansHeart);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var e in hints.PotentialTargets)
        {
            e.Priority = (OID)e.Actor.OID switch
            {
                OID.GraniteGaol => 3,
                OID.TitansHeart => 2,
                OID.Boss => 1,
                _ => 0
            };
        }
    }
}
