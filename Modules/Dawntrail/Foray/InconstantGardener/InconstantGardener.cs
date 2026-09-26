// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.InconstantGardener;

public enum OID : uint
{
    Iambe = 0x4C41,
    Helper = 0x233C,
    Iambe1 = 0x4C42, // R1.000, x0 (spawn during fight)
    WinsomeSeed = 0x4C43, // R0.240-0.528, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 50855, // Iambe->player, no cast, single-target
    DirectSeeding = 48029, // Iambe->self, 3.0s cast, single-target
    GardenersHymnCast = 48031, // Iambe->self, 2.5s cast, single-target
    GardenersHymn = 48032, // 4C42->location, 6.0s cast, range 5 circle
    Burst = 48033, // 4C43->self, 2.0s cast, range 15 circle
    OdeOfTheUnderfoot = 48037, // Iambe->self, 5.0s cast, range 10 circle
    IambicMarch = 48035, // Iambe->self, 3.0s cast, range 40 circle
}

public enum SID : uint
{
    ForwardMarch = 5142, // Iambe->player, extra=0x0
    AboutFace = 5143, // Iambe->player, extra=0x0
    ForcedMarch = 1257, // Iambe->player, extra=0x1/0x2
    Gen = 5106, // 4C42->4C43, extra=0x1
    Gen1 = 5107, // 4C42->4C43, extra=0x1
}

sealed class GardenersHymn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GardenersHymn, 5f);
sealed class OdeOfTheUnderfoot(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OdeOfTheUnderfoot, 10f);

/// <summary>
/// Iambic March, aimed by the shared facing search -- told only that the boss's own circle is coming.
///
/// <para>Ode of the Underfoot, a ten-yalm circle on the boss, lands 2.2 seconds after every walk (both 2026-09-25
/// pulls: the march resolves 6.5s ahead, the walk, then Ode). So the walk must end clear of it. BossmodReborn's
/// rule said so by pointing every character straight away from the boss, which is right about the circle and
/// blind to what is behind: all eight walks that night went outward and two ended outside the FATE. Declaring the
/// circle unsafe instead lets the search pick any facing that clears it and stays on the floor -- a toon already
/// out near the edge walks round the boss rather than off the far side. Declared rather than read, because Ode's
/// cast only begins 2.8s before the walk, and the march is decided before that.</para>
/// </summary>
sealed class IambicMarch(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 2.0f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, default, default)
{
    /// <summary>Ode of the Underfoot's radius and a yalm and a half to spare.</summary>
    private const float OdeReach = 11.5f;

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
        => base.DestinationUnsafe(slot, actor, pos) || (pos - Module.PrimaryActor.Position).LengthSq() < OdeReach * OdeReach;
}

sealed class Burst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Burst, 15.0f, riskyWithSecondsLeft: 6.0f)
{
    private readonly List<Actor> seeds = [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.WinsomeSeed)
        {
            seeds.Add(actor);
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == (uint)OID.WinsomeSeed)
        {
            seeds.Remove(actor);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.GardenersHymn)
        {
            foreach (var seed in seeds)
            {
                if (seed.Position.InCircle(spell.LocXZ, 5.0f))
                {
                    Casters.Add(new(Shape, seed.Position, default, Module.CastFinishAt(spell, 3.5f), actorID: seed.InstanceID,
                        shapeDistance: Shape.Distance(seed.Position, default)));
                }
            }
        }
    }
}

[SkipLocalsInit]
sealed class InconstantGardenerStates : StateMachineBuilder
{
    public InconstantGardenerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<GardenersHymn>()
            .ActivateOnEnter<OdeOfTheUnderfoot>()
            .ActivateOnEnter<IambicMarch>()
            .ActivateOnEnter<Burst>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, GroupID = 1093u, CFCID = 1093u, NameID = 2079u, PrimaryActorOID = (uint)OID.Iambe, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
// The FATE's own circle, as BMR has it. This used to be a fixed centre fitted to one pull's cast locations
// (2026-08-24), 3.9y off the circle the game reports -- (-170, -500), 30y, in both 2026-09-25 recordings --
// and the march is aimed against these bounds, so a walk kept "inside" could still end outside the FATE.
public sealed class InconstantGardener(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
