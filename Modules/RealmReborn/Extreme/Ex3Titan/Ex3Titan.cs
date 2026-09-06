// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex3Titan;

class WeightOfTheLand(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WeightOfTheLandAOE, 6);
class GaolerVoidzone(ModuleBase module) : Components.Voidzone(module, 5, m => m.Enemies((uint)OID.GaolerVoidzone).Where(e => e.EventState != 7));

[ModuleInfo(CFCID = 64u, NameID = 1801u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex3Titan : ModuleBase
{
    private readonly List<Actor> _heart;
    public Actor? Heart() => _heart.Count != 0 ? _heart[0] : null;

    public readonly List<Actor> Gaolers;
    public readonly List<Actor> Gaols;
    public readonly List<Actor> Bombs;

    public Ex3Titan(WorldState ws, Actor primary) : base(ws, primary, new(0, 0), new ArenaBoundsCircle(25))
    {
        _heart = Enemies((uint)OID.TitansHeart);
        Gaolers = Enemies((uint)OID.GraniteGaoler);
        Gaols = Enemies((uint)OID.GraniteGaol);
        Bombs = Enemies((uint)OID.BombBoulder);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor, allowDeadAndUntargetable: true);
        Arena.Actors(Gaolers);
        Arena.Actors(Gaols, Colors.Object);
        Arena.Actors(Bombs, Colors.Object);
    }
}
