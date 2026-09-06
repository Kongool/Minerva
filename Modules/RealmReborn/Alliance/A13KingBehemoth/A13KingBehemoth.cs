// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A13KingBehemoth;

class EclipticMeteor(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.EclipticMeteor, 60f)
{
    public override ReadOnlySpan<Actor> BlockerActors()
    {
        var boulders = Module.Enemies((uint)OID.Comet);
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
class SelfDestruct(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SelfDestruct, 8.4f);
class CharybdisAOE(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 6f, (uint)AID.CharybdisAOE, GetVoidzones, 0.1f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Charybdis);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

[ModuleInfo(CFCID = 92u, NameID = 727u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A13KingBehemoth(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-110, -380), new ArenaBoundsCircle(35))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.IronGiant));
        Arena.Actors(Enemies((uint)OID.Puroboros));
    }
}
