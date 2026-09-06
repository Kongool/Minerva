// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Trial.T01IfritN;

public enum OID : uint
{
    Boss = 0xCF, // x1
    InfernalNail = 0xD0, // spawn during fight
    Helper = 0x191
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast

    Incinerate = 453, // Boss->self, no cast, range 10+R ?-degree cone cleave
    VulcanBurst = 454, // Boss->self, no cast, range 16+R circle unavoidable aoe with knockback ?
    Eruption = 455, // Boss->self, 2.2s cast, visual
    EruptionAOE = 733, // Helper->location, 3.0s cast, range 8 aoe
    Hellfire = 458, // Boss->self, 2.0s cast, infernal nail 'enrage'
    RadiantPlume = 456, // Boss->self, 2.2s cast, visual
    RadiantPlumeAOE = 734 // Helper->location, 3.0s cast, range 8 aoe
}

class Hints(ModuleBase module) : ModuleComponent(module)
{
    private DateTime _nailSpawn;

    public override void AddGlobalHints(GlobalHints hints)
    {
        var nail = Module.Enemies((uint)OID.InfernalNail).FirstOrDefault();
        if (_nailSpawn == default && nail != null && nail.IsTargetable)
        {
            _nailSpawn = World.CurrentTime;
        }
        if (_nailSpawn != default && nail != null && nail.IsTargetable && !nail.IsDead)
        {
            hints.Add($"Nail enrage in: {Math.Max(35 - (World.CurrentTime - _nailSpawn).TotalSeconds, 0.0f):f1}s");
        }
    }
}

class Incinerate(ModuleBase module) : Components.Cleave(module, (uint)AID.Incinerate, new AOEShapeCone(16f, 60f.Degrees())); // TODO: verify angle
class Eruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EruptionAOE, 8f);
class RadiantPlume(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RadiantPlumeAOE, 8f);
class Nails(ModuleBase module) : Components.Adds(module, (uint)OID.InfernalNail, 2);

class T01IfritNStates : StateMachineBuilder
{
    public T01IfritNStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Hints>()
            .ActivateOnEnter<Incinerate>()
            .ActivateOnEnter<Eruption>()
            .ActivateOnEnter<RadiantPlume>()
            .ActivateOnEnter<Nails>();
    }
}

[ModuleInfo(CFCID = 56u, NameID = 1185u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class T01IfritN(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, IfritArena)
{
    public static readonly ArenaBoundsCustom IfritArena = new([new Polygon(default, 19.5f, 36)]);
}
