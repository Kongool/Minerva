// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T01Valigarmanda;

sealed class SlitheringStrike(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SlitheringStrike, new AOEShapeCone(24f, 90f.Degrees()));
sealed class SkyruinHailOfFeathersDisasterZoneRuinForetold(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.Skyruin1, (uint)AID.Skyruin2, (uint)AID.HailOfFeathers,
(uint)AID.DisasterZone1, (uint)AID.DisasterZone2, (uint)AID.RuinForetold]);

abstract class CalamitousCry(ModuleBase module, uint aid) : Components.LineStack(module, aidMarker: aid, (uint)AID.CalamitousCry, 5d, 60f, 3f)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == (uint)AID.LimitBreakVisual4) // not sure if line stack gets cancelled when limit break phase ends, just a safety feature
        {
            CurrentBaits.Clear();
        }
    }
}

sealed class CalamitousCry1(ModuleBase module) : CalamitousCry(module, (uint)AID.CalamitousCryMarker1);
sealed class CalamitousCry2(ModuleBase module) : CalamitousCry(module, (uint)AID.CalamitousCryMarker2);

sealed class CalamitousEcho(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CalamitousEcho, new AOEShapeCone(40f, 10f.Degrees()));

abstract class Tulidisaster(ModuleBase module, uint aid, double delay) : Components.RaidwideCastDelay(module, (uint)AID.TulidisasterVisual, aid, delay);
sealed class Tulidisaster1(ModuleBase module) : Tulidisaster(module, (uint)AID.Tulidisaster1, 3.1d);
sealed class Tulidisaster2(ModuleBase module) : Tulidisaster(module, (uint)AID.Tulidisaster2, 11.6d);
sealed class Tulidisaster3(ModuleBase module) : Tulidisaster(module, (uint)AID.Tulidisaster3, 19.6d);

sealed class Eruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Eruption, 6f);
sealed class IceTalon(ModuleBase module) : Components.BaitAwayIcon(module, 6f, (uint)IconID.Tankbuster, (uint)AID.IceTalon, 5d, tankbuster: true);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 832u, CFCID = 832u, NameID = 12854u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class T01Valigarmanda(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsRect(20f, 15f))
{
    private static readonly uint[] objects = [(uint)OID.IceBoulder, (uint)OID.FlameKissedBeacon, (uint)OID.GlacialBeacon, (uint)OID.ThunderousBeacon];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(this, objects, Colors.Object);
    }
}
