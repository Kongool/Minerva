// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Dungeon.D09DrownedCityOfSkalla.D091Kelpie;

public enum OID : uint
{
    Kelpie = 0x1FAB, // R5.400, x?
    Kelpie1 = 0x18D6, // R0.500, x?, Helper type
    Hydrosphere = 0x2051, // R1.200, x?, Helper type
    WaterPuddles = 0x1EA7D5, // R0.500, x?, EventObj type
}

public enum AID : uint
{
    AutoAttack = 872, // 1FAB->player, no cast, single-target
    Torpedo = 9807, // 1FAB->player, 2.0s cast, single-target
    RisingSeas = 9808, // 1FAB->self, 3.0s cast, range 50+R circle
    Gallop = 9811, // 1FAB->location, no cast, ???
    HydroPull = 9809, // 1FAB->self, 7.0s cast, ??? : Draw in
    HydroPush = 9810, // 1FAB->self, 7.0s cast, range 50+R circle : Knockback
    BloodyPuddle = 9812, // 1FAB->self, 5.0s cast, single-target
    BloodyPuddle1 = 9813, // 18D6->self, no cast, range 8 circle
    BubbleBurst = 9755, // 2051->self, 1.0s cast, range 6 circle
}

public enum IconID : uint
{
    BloodyPuddleBaitIcon = 43, // player/3F75/40C0->self
    BloodyPuddleTetherIcon = 1, // player/3F75/40C0->self
}

public enum TetherID : uint
{
    HydrosphereTether = 3, // Hydrosphere->player/3F75/40C0
}

sealed class RisingSeas(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RisingSeas);

sealed class HydroPull(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.HydroPull, 20f, kind: Kind.DirBackward)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var kb = ref Casters.Ref(0);
            var act = kb.Activation;
            if (!IsImmune(slot, act))
            {
                hints.AddForbiddenZone(new SDRect(kb.Origin, kb.Direction, 32f, 32f, 14.5f));
            }
        }
        base.AddAIHints(slot, actor, assignment, hints);
    }
}

sealed class HydroPush(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.HydroPush, 20f, kind: Kind.DirForward)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var kb = ref Casters.Ref(0);
            var act = kb.Activation;
            if (!IsImmune(slot, act))
            {
                // We avoid the corners in case a puddle was placed there.
                hints.AddForbiddenZone(new SDInvertedRect(kb.Origin, kb.Direction, 10f, 10f, 11f));
            }
        }
        base.AddAIHints(slot, actor, assignment, hints);
    }
}

sealed class BloodyPuddle(ModuleBase module) : Components.BaitAwayIcon(module, 4f, (uint)IconID.BloodyPuddleBaitIcon, (uint)AID.BloodyPuddle1, 0f, true)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var _activation = World.FutureTime(5.0f);
        // We want to path to the corner if we have a puddle bait.
        WPos[] corners = [new(-207f, -9f), new(-233f, -9f), new(-233f, 17f), new(-207f, 17f)];
        var count = corners.Length;
        if (count != 0 && ActiveBaits.Count > 0)
        {
            var baitZonez = new ShapeDistance[count];
            for (var i = 0; i < count; ++i)
            {
                var c = corners[i];
                // purposefully smaller than the puddle.
                baitZonez[i] = new SDInvertedCircle(c, 1.5f);
            }
            hints.AddForbiddenZone(new SDIntersection(baitZonez), _activation);
        }
        base.AddAIHints(slot, actor, assignment, hints);
    }
}

/*
 * Puddles that stay on the arena for several minutes.
 */
sealed class WaterPuddles(ModuleBase module) : ModuleComponent(module)
{
    public static List<Actor> GetPuddles(ModuleBase module)
    {
        var orbs = module.Enemies((uint)OID.WaterPuddles);
        var count = orbs.Count;
        if (count == 0)
            return [];

        var filteredWater = new List<Actor>(count);
        for (var i = 0; i < count; ++i)
        {
            var z = orbs[i];
            if (!z.IsDead)
                filteredWater.Add(z);
        }
        return filteredWater;
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        var orbs = GetPuddles(Module);
        var count = orbs.Count;
        if (count != 0)
            hints.Add("Avoid puddles.");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var puddles = GetPuddles(Module);
        var count = puddles.Count;

        if (count != 0)
        {
            var puddlez = new ShapeDistance[count];
            for (var i = 0; i < count; ++i)
            {
                var p = puddles[i];
                puddlez[i] = new SDCircle(p.Position, 8f);
            }
            hints.AddForbiddenZone(new SDUnion(puddlez));
        }
        base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var orbs = GetPuddles(Module);
        var count = orbs.Count;
        for (var i = 0; i < count; ++i)
        {
            // TODO would be nice if these only draw the portion inside arena bounds.
            Arena.ZoneCircle(orbs[i].Position, 7f, Colors.Danger);
        }
    }
}

// Show the tether object
class BloodyBurstTether(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeCircle(6f), (uint)TetherID.HydrosphereTether, (uint)AID.BubbleBurst, enemyOID: (uint)OID.Hydrosphere);

[SkipLocalsInit]
sealed class D091KelpieStates : StateMachineBuilder
{
    public D091KelpieStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<RisingSeas>()
            .ActivateOnEnter<HydroPull>()
            .ActivateOnEnter<HydroPush>()
            .ActivateOnEnter<WaterPuddles>()
            .ActivateOnEnter<BloodyPuddle>()
            .ActivateOnEnter<BloodyBurstTether>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 279u, CFCID = 279u, NameID = 6907u, PrimaryActorOID = (uint)OID.Kelpie, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "wen (ported from BMR)")]
[SkipLocalsInit]

public sealed class D091Kelpie(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-220f, 4f), new ArenaBoundsSquare(14.5f));
