// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V13Gladiator;

sealed class SunderedRemains(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SunderedRemains, 10f, 8);
sealed class Landing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Landing, 20f);
sealed class GoldenFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GoldenFlame, new AOEShapeRect(60f, 5f));
sealed class SculptorsPassion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SculptorsPassion, new AOEShapeRect(60f, 4f));
sealed class RackAndRuin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RackAndRuin, new AOEShapeRect(40f, 2.5f), 8);
sealed class MightySmite(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.MightySmite);

sealed class FlashOfSteel(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.FlashOfSteel1, (uint)AID.FlashOfSteel2]);

sealed class ShatteringSteelMeteor(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.ShatteringSteel, 60f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction && Module.Enemies((uint)OID.WhirlwindUpdraft).Count != 0) // depending on path Shattering Steel can be combined with other mechs
        {
            base.OnCastStarted(caster, spell);
        }
    }

    public override ReadOnlySpan<Actor> BlockerActors()
    {
        var boulders = Module.Enemies((uint)OID.AntiqueBoulder);
        var count = boulders.Count;
        if (count == 0)
        {
            return [];
        }
        var actors = new List<Actor>(1);
        for (var i = 0; i < count; ++i)
        {
            var b = boulders[i];
            if (b.ModelState.AnimState2 != 1)
            {
                actors.Add(b);
                break;
            }
        }
        return CollectionsMarshal.AsSpan(actors);
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 868u, CFCID = 868u, NameID = 11387u, PrimaryActorOID = (uint)OID.Gladiator, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V13Gladiator(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-35f, -271f), new ArenaBoundsSquare(19.5f));
