// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A35Diabolos;

class Nightmare(ModuleBase module) : Components.CastGaze(module, (uint)AID.Nightmare);
class NightTerror(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.NightTerror, 10, 8);
class RuinousOmen1(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RuinousOmen1);
class RuinousOmen2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RuinousOmen2);
class UltimateTerror(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UltimateTerror, new AOEShapeDonut(5, 19.5f));

[ModuleInfo(CFCID = 220u, NameID = 5526u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A35Diabolos(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-350, -445), new ArenaBoundsCircle(35))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Deathgate));
        Arena.Actors(Enemies((uint)OID.Lifegate));
    }
}
