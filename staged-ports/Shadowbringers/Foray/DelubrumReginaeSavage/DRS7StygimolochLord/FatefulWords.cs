// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS7StygimolochLord;

sealed class FatefulWords(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.FatefulWordsAOE)
{
    private readonly Kind[] _mechanics = new Kind[PartyState.MaxPartySize];

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        var kind = _mechanics[slot];
        if (kind != Kind.None)
            return new Knockback[1] { new(Center, 6f, Module.CastFinishAt(Module.PrimaryActor.CastInfo), kind: kind, ignoreImmunes: true) };
        return [];
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var kind = status.ID switch
        {
            (uint)SID.WanderersFate => Kind.AwayFromOrigin,
            (uint)SID.SacrificesFate => Kind.TowardsOrigin,
            _ => Kind.None
        };
        if (kind != Kind.None)
            AssignMechanic(actor, kind);
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.WanderersFate or (uint)SID.SacrificesFate)
            AssignMechanic(actor, Kind.None);
    }

    private void AssignMechanic(Actor actor, Kind mechanic)
    {
        var slot = Raid.FindSlot(actor.InstanceID);
        if (slot >= 0 && slot < _mechanics.Length)
            _mechanics[slot] = mechanic;
    }
}
