// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.MSQ.ARequiemForHeroes;

class StormUnbound(ModuleBase module) : Components.Exaflare(module, 5)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TheStormUnboundCast)
            Lines.Add(new(caster.Position, 5f * caster.Rotation.ToDirection(), Module.CastFinishAt(spell), 1d, 4, 2));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.TheStormUnboundCast or (uint)AID.TheStormUnboundRepeat)
        {
            var count = Lines.Count;
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                var line = Lines[i];
                if (line.Next.AlmostEqual(pos, 1f))
                {
                    AdvanceLine(line, pos);
                    if (line.ExplosionsLeft == 0)
                        Lines.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

class LightlessSpark2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightlessSparkAdds, new AOEShapeCone(40f, 45f.Degrees()));

class ArtOfTheStorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArtOfTheStorm, 8f);
class EntropicFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EntropicFlame, new AOEShapeRect(50f, 4f));

class FloodOfDarkness(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FloodOfDarkness, 6f);
class VeinSplitter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VeinSplitter, 10f);
class LightlessSpark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightlessSpark, new AOEShapeCone(40f, 45f.Degrees()));
class SwellUnbound(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheSwellUnbound, new AOEShapeDonut(8f, 20f));
class Swell(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ArtOfTheSwell, 8f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 8f));
    }
}

abstract class ArtOfTheSword(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(40f, 3f));
class ArtOfTheSword1(ModuleBase module) : ArtOfTheSword(module, (uint)AID.ArtOfTheSword1);
class ArtOfTheSword2(ModuleBase module) : ArtOfTheSword(module, (uint)AID.ArtOfTheSword2);
class ArtOfTheSword3(ModuleBase module) : ArtOfTheSword(module, (uint)AID.ArtOfTheSword3);

class DarkAether(ModuleBase module) : Components.Voidzone(module, 1.5f, GetVoidzones, 3f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.DarkAether);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (!z.IsDead)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

// Dark Aether orbs come out of the middle in pairs every 3.2s and drift outward at about a yalm a second; one that
// comes within about a yalm of you bursts (Burst, radius 6 on the sheet, no cast bar). DarkAether above draws each orb
// once it exists, which cannot cover one spawning on top of you: newtoon2 stood in the middle and took four Bursts in
// 13s (2026-09-30, The Storm recording, 451-465s; every orb that burst was 0.9-1.1y away when it did, and none that
// stayed 1.3y or more did). So the spawn point is kept clear until the next pair is due, and a little after.
class DarkAetherSpawn(ModuleBase module) : Components.GenericAOEs(module)
{
    private const double Period = 3.2d;
    private static readonly AOEShapeCircle circle = new(2.5f); // contact ~1.1y, spawns scatter ~0.5y, erring wide
    private WPos spawn;
    private DateTime nextSpawn;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
        => nextSpawn != default && World.CurrentTime < nextSpawn.AddSeconds(1d) ? new[] { new AOEInstance(circle, spawn, default, nextSpawn) } : [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.DarkAether)
        {
            spawn = actor.Position;
            nextSpawn = World.FutureTime(Period);
        }
    }
}

class Adds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.TheStorm, (uint)OID.TheSwell, (uint)OID.AmeNoHabakiri]);

// Unmoving Troika: an instant frontal cone, then two more from a helper along the same facing, 1.4s and 1.8s after. The
// first has no tell and Zenos faces whoever he is on, so it lands; the two after are casts, and a step to his flank
// clears them. 120 degrees and 9+R as in Ala Mhigo's Zenos (BossmodReborn's D063), which casts the same thing. A Requiem
// for Heroes, 2026-09-30: all three hit, nothing drawn.
abstract class UnmovingTroika(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(9.92f, 60f.Degrees()));
class UnmovingTroikaSecond(ModuleBase module) : UnmovingTroika(module, (uint)AID.UnmovingTroikaSecond);
class UnmovingTroikaLast(ModuleBase module) : UnmovingTroika(module, (uint)AID.UnmovingTroikaLast);

// a 20-yalm circle from the centre of a 20-yalm arena, and a cast bigger than the arena: nowhere to stand clear
class TheSwordUnbound(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TheSwordUnbound);
class Concentrativity(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Concentrativity);

public class ZenosP2States : StateMachineBuilder
{
    public ZenosP2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FloodOfDarkness>()
            .ActivateOnEnter<VeinSplitter>()
            .ActivateOnEnter<LightlessSpark>()
            .ActivateOnEnter<LightlessSpark2>()
            .ActivateOnEnter<SwellUnbound>()
            .ActivateOnEnter<Swell>()
            .ActivateOnEnter<ArtOfTheSword1>()
            .ActivateOnEnter<ArtOfTheSword2>()
            .ActivateOnEnter<ArtOfTheSword3>()
            .ActivateOnEnter<ArtOfTheStorm>()
            .ActivateOnEnter<EntropicFlame>()
            .ActivateOnEnter<DarkAether>()
            .ActivateOnEnter<DarkAetherSpawn>()
            .ActivateOnEnter<StormUnbound>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<UnmovingTroikaSecond>()
            .ActivateOnEnter<UnmovingTroikaLast>()
            .ActivateOnEnter<TheSwordUnbound>()
            .ActivateOnEnter<Concentrativity>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, PrimaryActorOID = (uint)OID.BossP2, CFCID = 68721u, NameID = 6039u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class ZenosP2(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(233, -93.25f), new ArenaBoundsCircle(20f));
