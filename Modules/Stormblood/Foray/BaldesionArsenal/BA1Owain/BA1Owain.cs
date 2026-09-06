// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA1Owain;

sealed class Thricecull(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Thricecull);
sealed class AcallamNaSenorach(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AcallamNaSenorach);
sealed class LegendaryImbas(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.LegendaryImbas); // applies dorito stacks, seems to get skipped if less than 4 people alive?
sealed class Pitfall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pitfall, 20f);

[ModuleInfo(Group = ModuleGroup.BaldesionArsenal, GroupID = 639u, CFCID = 639u, NameID = 7970u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class BA1Owain(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(128.98f, 748f), 29.5f, 64)], [new Rectangle(new(129f, 718f), 20f, 0.8f), new Rectangle(new(129f, 778f), 20f, 0.825f),
    new Polygon(new(123.5f, 778f), 1.5f, 8), new Polygon(new(134.5f, 778f), 1.5f, 8), new Polygon(new(123.5f, 718f), 1.5f, 8), new Polygon(new(134.5f, 718f), 1.5f, 8)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.IvoryPalm));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.IvoryPalm => 1,
                _ => 0
            };
        }
    }

    protected override bool CheckPull() => base.CheckPull() && (Center - Raid.Player()!.Position).LengthSq() < 1e4f;
}
