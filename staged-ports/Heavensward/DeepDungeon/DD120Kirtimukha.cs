// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.DD120Kirtimukha;

public enum OID : uint
{
    Boss = 0x1819, // R3.600, x1
    DeepPalaceHornet = 0x1905 // R0.400, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target
    AutoAttackAdds = 6498, // DeepPalaceHornet->player, no cast, single-target

    AcidMist = 7134, // Boss->self, 3.0s cast, range 6+R circle
    BloodyCaress = 7133, // Boss->self, no cast, range 8+R 120-degree cone
    FinalSting = 919, // DeepPalaceHornet->player, 3.0s cast, single-target
    GoldDust = 7135, // Boss->location, 3.0s cast, range 8 circle
    Leafstorm = 7136, // Boss->self, 3.0s cast, range 50 circle
    RottenStench = 7137 // Boss->self, 3.0s cast, range 45+R width 12 rect
}

class AcidMist(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AcidMist, 9.6f);
class BossAdds(ModuleBase module) : Components.Adds(module, (uint)OID.DeepPalaceHornet)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            switch (e.Actor.OID)
            {
                case (uint)OID.DeepPalaceHornet:
                    e.Priority = 2;
                    e.ForbidDOTs = true;
                    break;
                case (uint)OID.Boss:
                    e.Priority = 1;
                    break;
            }
        }
    }
}
class BloodyCaress(ModuleBase module) : Components.Cleave(module, (uint)AID.BloodyCaress, new AOEShapeCone(11.6f, 60f.Degrees()), activeWhileCasting: false);
class FinalSting(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.FinalSting, "Final sting is being cast! \nKill the add or take 98% of your hp!");
class GoldDust(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GoldDust, 8f);
class Leafstorm(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Leafstorm);
class RottenStench(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RottenStench, new AOEShapeRect(48.6f, 6f));

class DD120KirtimukhaStates : StateMachineBuilder
{
    public DD120KirtimukhaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AcidMist>()
            .ActivateOnEnter<BossAdds>()
            .ActivateOnEnter<BloodyCaress>()
            .ActivateOnEnter<FinalSting>()
            .ActivateOnEnter<GoldDust>()
            .ActivateOnEnter<Leafstorm>()
            .ActivateOnEnter<RottenStench>();
    }
}

[ModuleInfo(CFCID = 210u, NameID = 5384u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD120Kirtimukha(WorldState ws, Actor primary) : ModuleBase(ws, primary, SharedBounds.ArenaBounds120130.Center, SharedBounds.ArenaBounds120130);
