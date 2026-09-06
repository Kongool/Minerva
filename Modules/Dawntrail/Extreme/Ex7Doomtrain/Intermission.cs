// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;


namespace Minerva.Dawntrail.Extreme.Ex7Doomtrain;


// // // Technically I've seen two survive aetherosote, but we want the highlighting for fewer than 3 in a stack
sealed class AetherocharAetherosote(ModuleBase module) : Components.IconStackSpread(module, stackIcon: (uint)IconID.Aetherosote, spreadIcon: (uint)IconID.Aetherochar, stackAID: (uint)AID.Aetherosote, spreadAID: (uint)AID.Aetherochar, stackRadius: 6f, spreadRadius: 6f, minStackSize: 3, maxStackSize: 3, activationDelay: 6.5d)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        // for some reason, we can't use stackIcon and spreadIcon here, so we have to use the AID values directly
        if (iconID is (uint)IconID.Aetherosote or (uint)IconID.Aetherochar)
        {
            var raid = Raid.WithoutSlot(false, true, true);
            if (iconID == (uint)IconID.Aetherosote)
            {
                // Stack always targets healers
                foreach (var member in raid)
                {
                    if (member.Role == Role.Healer)
                    {
                        AddStack(member, World.FutureTime(ActivationDelay));
                    }
                }
            }
            else
            {
                // Spread targets any 3, but we can't tell which
                foreach (var member in raid)
                {
                    if (member.Role != Role.Tank)
                    {
                        AddSpread(member, World.FutureTime(ActivationDelay));
                    }
                }
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var aid = spell.Action.ID;
        if (aid == StackAction)
        {
            var id = spell.MainTargetID;
            if (MaxCasts != 1 && Stacks.Count == 1 && Stacks.Ref(0).Target.InstanceID != id) // multi hit stack target died and new target got selected
            {
                Stacks.Ref(0).Target = World.Actors.Find(id)!;
            }
            if (++CastCounter == MaxCasts)
            {
                var count = Stacks.Count;
                var stacks = CollectionsMarshal.AsSpan(Stacks);
                for (var i = 0; i < count; ++i)
                {
                    if (stacks[i].Target.InstanceID == id)
                    {
                        ++NumFinishedStacks;
                        CastCounter = 0;
                        Stacks.RemoveAt(i);
                        return;
                    }
                }
                // stack not found, probably due to being self targeted
                if (count != 0)
                {
                    ++NumFinishedStacks;
                    CastCounter = 0;
                    Stacks.RemoveAt(0);
                }
            }
        }
        else if (aid == SpreadAction)
        {
            Spreads.Clear();
        }
    }
}

[SkipLocalsInit]
sealed class AetherialRay(ModuleBase module) : Components.BaitAwayIcon(module, shape: new AOEShapeCone(50f, 22.5f.Degrees()), iconID: (uint)IconID.AetherialRay, aid: (uint)AID.AetherialRay, activationDelay: 7.6d, centerAtTarget: false, tankbuster: true)
{
    public override Actor? BaitSource(Actor target) => Module.Enemies((uint)OID.GhostTrain).First();
}

sealed class RunawayTrain(ModuleBase module) : Components.RaidwideCastDelay(module, actionVisual: (uint)AID.RunawayTrainVisual2, actionAOE: (uint)AID.RunawayTrain, delay: 15d, hint: "Choo choo, the train is coming! (Raidwide)");
