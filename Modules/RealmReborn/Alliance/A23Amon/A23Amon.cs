// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A23Amon;

class BlizzagaForte(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlizzagaForte, 10f);
class Darkness(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Darkness, new AOEShapeCone(6f, 22.5f.Degrees()));

class CurtainCall(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.CurtainCall, 60f)
{
    public override ReadOnlySpan<Actor> BlockerActors()
    {
        var boulders = Module.Enemies((uint)OID.IceCage);
        var count = boulders.Count;
        if (count == 0)
            return [];
        var actors = new List<Actor>();
        for (var i = 0; i < count; ++i)
        {
            var b = boulders[i];
            if (!b.IsDead)
                actors.Add(b);
        }
        return CollectionsMarshal.AsSpan(actors);
    }
}

class ThundagaForte1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThundagaForte1, 6f);
class ThundagaForte2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThundagaForte2, 6f);

[ModuleInfo(CFCID = 102u, NameID = 2821u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A23Amon(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(default, -200f), new ArenaBoundsCircle(30f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.KumKum));
        Arena.Actors(Enemies((uint)OID.Kichiknebik));
        Arena.Actors(Enemies((uint)OID.ExperimentalByProduct66));
        Arena.Actors(Enemies((uint)OID.ExperimentalByProduct33));
    }
}
