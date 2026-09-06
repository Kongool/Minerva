// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex6Byakko;

class StormPulseRepeat(ModuleBase module) : Components.CastCounter(module, (uint)AID.StormPulseRepeat);
class HeavenlyStrike(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.HeavenlyStrike, 3f);
class FireAndLightningBoss(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireAndLightningBoss, new AOEShapeRect(54.3f, 10f));
class FireAndLightningAdd(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FireAndLightningAdd, new AOEShapeRect(54.75f, 10f));
class SteelClaw(ModuleBase module) : Components.Cleave(module, (uint)AID.SteelClaw, new AOEShapeCone(17.75f, 60f.Degrees()), [(uint)OID.Hakutei]);
class WhiteHerald(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.WhiteHerald, (uint)AID.WhiteHerald, 15f, 5.1f); // TODO: verify falloff
class DistantClap(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DistantClap, new AOEShapeDonut(4f, 25f));
class SweepTheLegBoss(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SweepTheLegBoss, new AOEShapeCone(28.3f, 135f.Degrees()));

[ModuleInfo(CFCID = 291u, NameID = 7092u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Ex6Byakko(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, NormalBounds)
{
    public static readonly ArenaBoundsCustom NormalBounds = new([new Polygon(default, 19.5f, 48)]);
    public static readonly ArenaBoundsCustom IntermissionBounds = new([new Polygon(default, 15, 48)]);

    private Actor? _hakutei;
    public Actor? Boss() => PrimaryActor;
    public Actor? Hakutei() => _hakutei;

    protected override void UpdateModule()
    {
        _hakutei ??= GetActor((uint)OID.Hakutei);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_hakutei);
    }
}
