// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex2Lakshmi;

class DivineDenial(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DivineDenial);
class ThePallOfLight(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.ThePallOfLight, 7, 8, 8);
class InnerDemonsGaze(ModuleBase module) : Components.CastGaze(module, (uint)AID.InnerDemons);
class InnerDemonsAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InnerDemons, 7);

[ModuleInfo(CFCID = 264u, NameID = 6385u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class Ex2Lakshmi : ModuleBase
{
    public readonly List<Actor> DreamingKshatriya;

    public Ex2Lakshmi(WorldState ws, Actor primary) : base(ws, primary, new(0, 0), new ArenaBoundsCircle(20))
    {
        DreamingKshatriya = Enemies((uint)OID.DreamingKshatriya);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(DreamingKshatriya);
    }
}
