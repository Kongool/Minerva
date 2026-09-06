// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D05Totorak.D053Graffias;

public enum OID : uint
{
    Boss = 0x103, // x1
    FleshyPod = 0x23C, // spawn during fight
    Comesmite = 0x104, // spawn during fight
    GraffiasTail = 0x10A, // spawn during fight
    PollenZone = 0x1E8614 // spawn during fight
}

public enum AID : uint
{
    AutoAttack = 870, // Boss/Comesmite->player, no cast
    RealmShaker = 697, // Boss->self, no cast, range 9 raidwide around boss
    Silkscreen = 701, // Boss->self, 2.5s cast, range 18 width 4 rect aoe
    StickyWeb = 698, // Boss->player, 1.5s cast, visual (spawns fleshy pod at target)
    PodBurst = 730, // FleshyPod->self, 3.0s cast, range 7.050 aoe
    TailMolt = 704, // Boss->self, no cast, visual (spawns tail)
    DeadlyThrust = 702 // Boss->self, 2.0s cast, visual (spawns pollen zone)
}

class Silkscreen(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Silkscreen, new AOEShapeRect(18, 2));
class StickyWeb(ModuleBase module) : Components.CastHint(module, (uint)AID.StickyWeb, "Delayed AOE at target");
class PodBurst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PodBurst, 7.05f);
class DeadlyThrust(ModuleBase module) : Components.CastHint(module, (uint)AID.DeadlyThrust, "Persistent voidzone at target");
class PollenZone(ModuleBase module) : Components.Voidzone(module, 10, m => m.Enemies((uint)OID.PollenZone));

class D053GraffiasStates : StateMachineBuilder
{
    public D053GraffiasStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Silkscreen>()
            .ActivateOnEnter<StickyWeb>()
            .ActivateOnEnter<PodBurst>()
            .ActivateOnEnter<DeadlyThrust>()
            .ActivateOnEnter<PollenZone>();
    }
}

[ModuleInfo(CFCID = 1u, NameID = 444u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class D053Graffias(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(215, -145), new ArenaBoundsCircle(20))
{
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var haveTail = Enemies((uint)OID.GraffiasTail).Count > 0;
        foreach (var e in hints.PotentialTargets)
        {
            e.Priority = (OID)e.Actor.OID switch
            {
                OID.GraffiasTail => 2,
                OID.Comesmite => haveTail ? 2 : 1,
                OID.Boss => 1,
                _ => 0,
            };
        }
    }
}
