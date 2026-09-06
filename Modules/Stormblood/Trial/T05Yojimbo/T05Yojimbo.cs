// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class MettaGiri(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MettaGiri);
class Yukikaze2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Yukikaze2, new AOEShapeRect(44.5f, 2f));
class TinySong(ModuleBase module) : Components.StackTogether(module, (uint)IconID.DoritoStack, 5f, 1f);
class BitterEnd2(ModuleBase module) : Components.Cleave(module, (uint)AID.BitterEnd2, new AOEShapeCone(9.8f, 45f.Degrees()));
class AmeNoMurakumo(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AmeNoMurakumo);
class Masamune(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Masamune, 4f);
class ZanmaZanmai(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ZanmaZanmai, "Raidwide drop to 1 hp");

[ModuleInfo(CFCID = 595u, NameID = 6089u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team, Chuggalo (ported from BMR)")]
public class T05Yojimbo(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f))
{
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.IronChain => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Embodiment));
    }
}
