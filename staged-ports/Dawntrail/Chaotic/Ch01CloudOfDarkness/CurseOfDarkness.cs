// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class CurseOfDarkness(ModuleBase module) : Components.CastCounter(module, (uint)AID.CurseOfDarknessAOE);

sealed class DarkEnergyParticleBeam(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.DarkEnergyParticleBeam)
{
    private readonly DateTime[] _activation = new DateTime[PartyState.MaxAllianceSize];

    private static readonly AOEShapeCone _shape = new(25f, 7.5f.Degrees());

    public override void Update()
    {
        CurrentBaits.Clear();
        var deadline = World.FutureTime(7d);
        foreach (var (i, p) in Raid.WithSlot(false, false, true))
        {
            ref var activation = ref _activation[i];
            if (activation != default && activation < deadline)
                CurrentBaits.Add(new(p, p, _shape, activation));
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.CurseOfDarkness && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            _activation[slot] = status.ExpireAt;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.CurseOfDarkness && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            _activation[slot] = default;
    }
}
