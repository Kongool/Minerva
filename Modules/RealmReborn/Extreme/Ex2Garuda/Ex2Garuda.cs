// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex2Garuda;

class DownburstBoss(ModuleBase module) : Components.Cleave(module, (uint)AID.Downburst1, new AOEShapeCone(11.7f, 60.Degrees())); // TODO: verify angle

abstract class Downburst(ModuleBase module, uint aid, OID oid) : Components.Cleave(module, aid, new AOEShapeCone(11.36f, 60.Degrees()), [(uint)oid]); // TODO: verify angle
class DownburstSuparna(ModuleBase module) : Downburst(module, (uint)AID.Downburst1, OID.Suparna); // TODO: verify angle
class DownburstChirada(ModuleBase module) : Downburst(module, (uint)AID.Downburst2, OID.Chirada); // TODO: verify angle

class Slipstream(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Slipstream, new AOEShapeCone(11.7f, 45.Degrees()));
class FrictionAdds(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FrictionAdds, 5);
class FeatherRain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FeatherRain, 3);
class AerialBlast(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AerialBlast);
class MistralShriek(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MistralShriek);
class Gigastorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Gigastorm, 6.5f);
class GreatWhirlwind(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GreatWhirlwind, 8);

[ModuleInfo(CFCID = 65u, NameID = 1644u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex2Garuda : ModuleBase
{
    public readonly List<Actor> Monoliths;
    public readonly List<Actor> RazorPlumes;
    public readonly List<Actor> SpinyPlumes;
    public readonly List<Actor> SatinPlumes;
    public readonly List<Actor> Chirada;
    public readonly List<Actor> Suparna;

    public Ex2Garuda(WorldState ws, Actor primary) : base(ws, primary, new(0, 0), new ArenaBoundsCircle(22))
    {
        Monoliths = Enemies((uint)OID.Monolith);
        RazorPlumes = Enemies((uint)OID.RazorPlume);
        SpinyPlumes = Enemies((uint)OID.SpinyPlume);
        SatinPlumes = Enemies((uint)OID.SatinPlume);
        Chirada = Enemies((uint)OID.Chirada);
        Suparna = Enemies((uint)OID.Suparna);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Monoliths.Where(a => !a.IsDead), Colors.Object, true);
        Arena.Actors(RazorPlumes);
        Arena.Actors(SpinyPlumes);
        Arena.Actors(SatinPlumes);
        Arena.Actors(Chirada);
        Arena.Actors(Suparna);
    }
}
