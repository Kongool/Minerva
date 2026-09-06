// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.TOP;

sealed class P2Invincibility(ModuleBase module) : Components.InvincibleStatus(module, (uint)SID.Invincibility);

sealed class P2PacketFilter(ModuleBase module) : Components.GenericInvincible(module)
{
    enum Firewall
    {
        None,
        PacketFilterF,
        PacketFilterM
    }

    private readonly List<Actor> _omegaM = [];
    private readonly List<Actor> _omegaF = [];

    private readonly Firewall[] _playerStates = Utils.MakeArray(PartyState.MaxPartySize, Firewall.None);

    protected override ReadOnlySpan<Actor> ForbiddenTargets(int slot, Actor actor) => _playerStates.BoundSafeAt(slot) switch
    {
        Firewall.PacketFilterF => CollectionsMarshal.AsSpan(_omegaF),
        Firewall.PacketFilterM => CollectionsMarshal.AsSpan(_omegaM),
        _ => []
    };

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.OmegaF:
                _omegaM.Remove(actor);
                _omegaF.Add(actor);
                break;
            case (uint)SID.OmegaM:
                _omegaF.Remove(actor);
                _omegaM.Add(actor);
                break;
            case (uint)SID.PacketFilterF:
            case (uint)SID.PacketFilterM:
                if (Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
                {
                    _playerStates[slot] = status.ID == (uint)SID.PacketFilterF ? Firewall.PacketFilterF : Firewall.PacketFilterM;
                }
                break;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.PacketFilterF or (uint)SID.PacketFilterM && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            _playerStates[slot] = Firewall.None;
        }
    }
}
