// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.DD20Spurge;

public enum OID : uint
{
    Boss = 0x169F, // R3.6
    PalaceHornet = 0x1763 // R0.4
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target
    AutoAttackHornet = 6498, // PalaceHornet->player, no cast, single-target

    AcidMist = 6422, // Boss->self, 3.0s cast, range 6+R circle
    BloodyCaress = 6421, // Boss->self, no cast, range 8+R 120-degree cone
    GoldDust = 6423, // Boss->location, 3.0s cast, range 8 circle
    Leafstorm = 6424, // Boss->self, 3.0s cast, range 50 circle
    RottenStench = 6425 // Boss->self, 3.0s cast, range 45+R width 12 rect
}

class BossAdds(ModuleBase module) : Components.Adds(module, (uint)OID.PalaceHornet)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            switch (e.Actor.OID)
            {
                case (uint)OID.Boss:
                    e.Priority = 1;
                    break;
                case (uint)OID.PalaceHornet:
                    e.Priority = 2;
                    e.ForbidDOTs = true;
                    break;
            }
        }
    }
}
class AcidMist(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AcidMist, 9.6f);
class BloodyCaress(ModuleBase module) : Components.Cleave(module, (uint)AID.BloodyCaress, new AOEShapeCone(11.6f, 60f.Degrees()), activeWhileCasting: false);
class GoldDust(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GoldDust, 8f);
class Leafstorm(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Leafstorm);
class RottenStench(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RottenStench, new AOEShapeRect(48.6f, 6f));

class DD20SpurgeStates : StateMachineBuilder
{
    public DD20SpurgeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BossAdds>()
            .ActivateOnEnter<AcidMist>()
            .ActivateOnEnter<BloodyCaress>()
            .ActivateOnEnter<GoldDust>()
            .ActivateOnEnter<Leafstorm>()
            .ActivateOnEnter<RottenStench>();
    }
}

[ModuleInfo(CFCID = 175u, NameID = 4999u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD20Spurge(WorldState ws, Actor primary) : ModuleBase(ws, primary, SharedBounds.ArenaBounds2090110.Center, SharedBounds.ArenaBounds2090110);
