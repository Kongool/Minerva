// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Quest.WarringTriad.ABloodyReunion;

public enum OID : uint
{
    Boss = 0x161E,
    MagitekTurretI = 0x161F, // R0.6
    MagitekTurretII = 0x1620, // R0.6
    TerminusEst = 0x1621, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    MagitekSlug = 6026, // Boss->self, 2.5s cast, range 60+R width 4 rect
    AetherochemicalGrenado = 6031, // MagitekTurretII->location, 3.0s cast, range 8 circle
    SelfDetonate = 6032, // MagitekTurretI/MagitekTurretII->self, 5.0s cast, range 40+R circle
    MagitekSpread = 6027, // Boss->self, 3.0s cast, range 20+R 240-degree cone
}

class MagitekSlug(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekSlug, new AOEShapeRect(60f, 2f));
class AetherochemicalGrenado(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AetherochemicalGrenado, 8f);
class SelfDetonate(ModuleBase module) : Components.CastHint(module, (uint)AID.SelfDetonate, "Kill turret before detonation!", true)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = hints.PotentialTargets[i];
            if (h.Actor.CastInfo?.Action.ID == WatchedAction)
                h.Priority = 5;
        }
    }
}
class MagitekSpread(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekSpread, new AOEShapeCone(20.55f, 120f.Degrees()));

class RegulaVanHydrusStates : StateMachineBuilder
{
    public RegulaVanHydrusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MagitekSlug>()
            .ActivateOnEnter<AetherochemicalGrenado>()
            .ActivateOnEnter<SelfDetonate>()
            .ActivateOnEnter<MagitekSpread>();
    }
}

[ModuleInfo(CFCID = 173u, NameID = 3818u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class RegulaVanHydrus(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(230f, 79f), 20.256f, 24)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, [(uint)OID.MagitekTurretI, (uint)OID.MagitekTurretII]);
        Arena.Actor(PrimaryActor);
    }
}
