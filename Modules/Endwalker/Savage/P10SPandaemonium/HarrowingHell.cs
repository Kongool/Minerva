// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P10SPandaemonium;

class HarrowingHell(ModuleBase module) : ModuleComponent(module)
{
    public int NumCasts;
    private BitMask _closestTargets;

    public override void Update()
    {
        // boss always points to (0,1) => offset dot dir == z + const
        _closestTargets = Raid.WithSlot(false, true, true).OrderBy(ia => ia.Item2.PosRot.Z).Take(2).Mask();
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var soaking = _closestTargets[slot];
        var shouldSoak = actor.Role == Role.Tank;
        if (soaking != shouldSoak)
            hints.Add(shouldSoak ? "Stay in front of the raid!" : "Go behind tanks!");
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.HarrowingHellAOE1 or (uint)AID.HarrowingHellAOE2 or (uint)AID.HarrowingHellAOE3 or (uint)AID.HarrowingHellAOE4 or (uint)AID.HarrowingHellAOE5 or (uint)AID.HarrowingHellAOE6 or (uint)AID.HarrowingHellAOE7 or (uint)AID.HarrowingHellAOE8 or (uint)AID.HarrowingHellKnockback)
            ++NumCasts;
    }
}
