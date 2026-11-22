// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.DeepDungeon.PilgrimsTraverse.DD40ForgivenNaivety;

public enum OID : uint
{
    ForgivenNaivety = 0x460B, // R8.0
    ForgivenAdulation = 0x460C, // R5.0
    ForgivenAdulationHelper = 0x4823, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 45130, // ForgivenNaivety/ForgivenAdulation->player, no cast, single-target
    Teleport = 42122, // ForgivenNaivety/ForgivenAdulation->location, no cast, single-target

    Visual1 = 42143, // ForgivenNaivety->self, no cast, single-target
    Visual2 = 42127, // ForgivenNaivety/ForgivenAdulation->self, no cast, single-target
    BlownBlessing1 = 42124, // ForgivenNaivety->self, 3.0s cast, single-target
    BlownBlessing2 = 42126, // ForgivenAdulation->self, no cast, single-target
    BlownBlessing3 = 42123, // ForgivenNaivety->self, 3.0s cast, single-target
    BlownBlessing34 = 42125, // ForgivenAdulation->self, no cast, single-target

    ShiningShot = 42129, // Helper/ForgivenAdulationHelper->self, 10.0s cast, range 20 circle
    NearTideVisual = 45121, // ForgivenNaivety->self, 6.2+0,8s cast, single-target
    NearTide = 45169, // Helper->self, 7.0s cast, range 13 circle
    FarTideVisual = 45122, // ForgivenNaivety->self, 6.2+0,8s cast, single-target
    FarTide = 45170, // Helper->self, 7.0s cast, range 8-60 donut

    SaltwaterShot = 42128, // Helper/ForgivenAdulationHelper->self, 10.0s cast, range 40 circle, knockback 21, away from origin
    Chaser1 = 42131, // Helper->location, 3.0s cast, range 5 circle
    Chaser2 = 44047, // Helper->location, 3.0s cast, range 5 circle
    SelfDestructVisual = 43289, // ForgivenAdulation->self, 13.0s cast, single-target
    SelfDestruct = 43290 // ForgivenAdulation->self, no cast, range 40 circle
}

[SkipLocalsInit]
sealed class Chaser(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Chaser1, (uint)AID.Chaser2], 5f);
[SkipLocalsInit]
sealed class NearTide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NearTide, 13f);
[SkipLocalsInit]
sealed class FarTide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FarTide, new AOEShapeDonut(8f, 60f));

[SkipLocalsInit]
sealed class ShiningShot : Components.SimpleAOEs
{
    public ShiningShot(ModuleBase module) : base(module, (uint)AID.ShiningShot, 20f, 2)
    {
        MaxDangerColor = 1;
    }
}

// Three pushes 2.6s apart, each 21y from its caster. The push starts 0.48-0.52s after its cast ends (10 pushes, Korha's
// and Saar's, 2026-11-22), from wherever the player is by then, but the base list lets the caster go when the cast ends.
// In that half second the dodge set off for the next push's spot, 2.5-3.8y further from the caster, and the push
// carried the toon that much further: past the floor's edge on 7 of those 10, and into the damage beyond it (about
// 18.3 from the centre, some 45k a time) on 3, with all four toons hit in the second set. The guard now holds until the push starts.
[SkipLocalsInit]
sealed class SaltwaterShot(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.SaltwaterShot, 21f)
{
    private const double PushStartsAfterSeconds = 0.5d;
    // just past the latest push seen (0.52s): held any longer, the guard is still up mid-push and calls the landing danger
    private const double HoldAfterCastSeconds = 0.55d;
    private (WPos Origin, DateTime Push, DateTime Until)? held;

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == WatchedAction)
            held = (cast.LocXZ != default ? cast.LocXZ : caster.Position, World.FutureTime(PushStartsAfterSeconds), World.FutureTime(HoldAfterCastSeconds));
        base.OnCastFinished(caster, cast);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (held is { } h && World.CurrentTime < h.Until && !IsImmune(slot, h.Push))
        {
            // the push still to come, toward the next caster if there is one, as below
            if (Casters.Count != 0)
                hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOriginIntoDirection(Center, h.Origin, 21f, 16.5f, (Casters[0].Origin - h.Origin).Normalized(), 15f.Degrees()), h.Push);
            else
                hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(Center, h.Origin, 21f, 16.5f), h.Push);
            return;
        }

        var count = Casters.Count;
        if (count != 0)
        {
            var casters = CollectionsMarshal.AsSpan(Casters);
            for (var i = 0; i < count; ++i)
            {
                ref readonly var c = ref casters[i];
                var act = c.Activation;
                var center = Center;
                if (!IsImmune(slot, act))
                {
                    if (count > i + 1)
                    {
                        ref readonly var c2 = ref casters[i + 1];
                        hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOriginIntoDirection(center, c.Origin, 21f, 16.5f, (c2.Origin - c.Origin).Normalized(), 15f.Degrees()), act);
                    }
                    else
                    {
                        hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(center, c.Origin, 21f, 16.5f), act);
                    }
                    return;
                }
            }
        }
    }
}

[SkipLocalsInit]
sealed class DD40ForgivenNaivetyStates : StateMachineBuilder
{
    public DD40ForgivenNaivetyStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Chaser>()
            .ActivateOnEnter<NearTide>()
            .ActivateOnEnter<FarTide>()
            .ActivateOnEnter<ShiningShot>()
            .ActivateOnEnter<SaltwaterShot>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1035u, CFCID = 1035u, NameID = 13977u, PrimaryActorOID = (uint)OID.ForgivenNaivety, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
[SkipLocalsInit]
public sealed class DD40ForgivenNaivety : ModuleBase
{
    private static readonly WPos arenaCenter = new(-600f, -300f);

    public DD40ForgivenNaivety(WorldState ws, Actor primary) : base(ws, primary, arenaCenter, new ArenaBoundsCustom([new Polygon(arenaCenter, 16.777f, 64)], MapResolution: 0.5f))
    {
        Adulations = Enemies((uint)OID.ForgivenAdulation);
    }
    public readonly List<Actor> Adulations;

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Adulations);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.ForgivenAdulation => 1,
                _ => 0
            };
            if (e.Priority == 1)
            {
                e.ForbidDOTs = true;
            }
        }
    }
}
