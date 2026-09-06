// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex1Ultima;

class ViscousAetheroplasm(ModuleBase module) : Components.Cleave(module, (uint)AID.ViscousAetheroplasm, new AOEShapeCircle(2), originAtTarget: true)
{
    public bool NeedTankSwap;
    private readonly int[] _stacks = new int[PartyState.MaxPartySize];

    public override void Update()
    {
        var tankSlot = World.Party.FindSlot(Module.PrimaryActor.TargetID);
        NeedTankSwap = tankSlot >= 0 && _stacks[tankSlot] >= 4;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);

        if (NeedTankSwap && actor.Role == Role.Tank)
            hints.Add(Module.PrimaryActor.TargetID == actor.InstanceID ? "Pass aggro to co-tank!" : "Taunt boss!");
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.ViscousAetheroplasm)
            UpdateStacks(actor, status.Extra);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.ViscousAetheroplasm)
            UpdateStacks(actor, 0);
    }

    private void UpdateStacks(Actor actor, int stacks)
    {
        var slot = Raid.FindSlot(actor.InstanceID);
        if (slot >= 0)
            _stacks[slot] = stacks;
    }
}
