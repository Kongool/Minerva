// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.Job.Summoner.AnArtForTheLiving;

public enum OID : uint
{
    Boss = 0x1CBA,
    ExplosiveIndicator = 0x1CD7, // R0.5
    AetherochemicalExplosive = 0x1CD5, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    PiercingLaser = 8683, // Boss->self, 3.0s cast, range 30+R width 6 rect
    NerveGas = 8707, // 1CD8->self, 3.0s cast, range 30+R 120-degree cone
    NerveGasLeft = 8708, // FX1979->self, 3.0s cast, range 30+R 180-degree cone
    NerveGasRight = 8709, // 1CD8->self, 3.0s cast, range 30+R 180-degree cone
    W111TonzeSwing = 8697, // 1CD1->self, 4.0s cast, range 8+R circle
    W11TonzeSwipe = 8699 // 1CD1->self, 3.0s cast, range 5+R 120-degree cone
}

public enum SID : uint
{
    Invincibility = 325
}

class OneOneOneTonzeSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.W111TonzeSwing, 12f);
class OneOneTonzeSwipe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.W11TonzeSwipe, new AOEShapeCone(9f, 60f.Degrees()));

class NerveGas1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NerveGas, new AOEShapeCone(35f, 60f.Degrees()));

abstract class NerveGasLR(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(35f, 90f.Degrees()));
class NerveGasRight(ModuleBase module) : NerveGasLR(module, (uint)AID.NerveGasRight);
class NerveGasLeft(ModuleBase module) : NerveGasLR(module, (uint)AID.NerveGasLeft);

class PiercingLaser(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PiercingLaser, new AOEShapeRect(33.68f, 3f));

class AetherochemicalExplosive(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<(Actor Actor, bool Primed, DateTime Activation)> Explosives = [];
    private static readonly AOEShapeCircle circle = new(5);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = Explosives.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var e = Explosives[i];
            if (!e.Actor.IsDead || !e.Primed)
            {
                aoes[i] = new(circle, e.Actor.Position, activation: e.Activation);
            }
        }
        return aoes;
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.ExplosiveIndicator)
        {
            Explosives.Add((actor, false, World.CurrentTime.AddSeconds(3d)));
        }
        else if (actor.OID == (uint)OID.AetherochemicalExplosive)
        {
            var count = Explosives.Count;
            for (var i = 0; i < count; ++i)
            {
                var e = Explosives[i];
                if (e.Actor.Position.AlmostEqual(actor.Position, 1f))
                {
                    Explosives[i] = (actor, true, e.Activation);
                    return;
                }
            }
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == (uint)OID.AetherochemicalExplosive)
        {
            var count = Explosives.Count;
            for (var i = 0; i < count; ++i)
            {
                var e = Explosives[i];
                if (e.Actor.Position.AlmostEqual(actor.Position, 1f))
                {
                    Explosives.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

class Adds(ModuleBase module) : Components.AddsMulti(module, [0x1CB6, 0x1CD1, 0x1CD6, 0x1CD8])
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.FindStatus((uint)SID.Invincibility) == null ? 1 : AIHints.Enemy.PriorityInvincible;
        }
    }
}

class SummoningNodeStates : StateMachineBuilder
{
    public SummoningNodeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PiercingLaser>()
            .ActivateOnEnter<Adds>()
            .ActivateOnEnter<NerveGas1>()
            .ActivateOnEnter<NerveGasLeft>()
            .ActivateOnEnter<NerveGasRight>()
            .ActivateOnEnter<AetherochemicalExplosive>()
            .ActivateOnEnter<OneOneOneTonzeSwing>()
            .ActivateOnEnter<OneOneTonzeSwipe>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68165u, NameID = 6695u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class SummoningNode(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly WPos[] vertices = [new(-115.5f, -272.34f), new(-106.5f, -272.34f), new(-93f, -280.25f),
    new(-88.8f, -287.6f), new(-88.3f, -287.6f), new(-88.3f, -302.4f), new(-88.8f, -302.4f), new(-92.85f, -310.77f),
    new(-106.5f, -318.68f), new(-115.5f, -318.68f), new(-129.15f, -310.77f), new(-133.2f, -302.4f), new(-133.7f, -302.4f),
    new(-133.7f, -288.6f), new(-133.2f, -288.6f), new(-129f, -280.25f)];
    private static readonly ArenaBoundsCustom arena = new([new PolygonCustom(vertices)]);
}
