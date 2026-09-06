// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL3Adrammelech;

sealed class FireIV(ModuleBase module) : Components.StayMove(module, 3d)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.FireIV1 or (uint)AID.FireIV3)
        {
            Array.Fill(PlayerStates, new(Requirement.Stay, Module.CastFinishAt(spell)));
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Pyretic && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            PlayerStates[slot] = default;
        }
    }
}

sealed class BlizzardIV(ModuleBase module) : Components.StayMove(module, 4d)
{
    private DateTime _activation;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.BlizzardIV1 or (uint)AID.BlizzardIV3)
        {
            var act = Module.CastFinishAt(spell, 2d);
            Array.Fill(PlayerStates, new(Requirement.Move, act));
            _activation = act; // freeze seems to activate a little after the spell finishes somehow
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Pyretic && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            PlayerStates[slot] = default;
        }
    }

    public override void Update()
    {
        if (_activation != default && _activation < World.CurrentTime)
        {
            Array.Clear(PlayerStates);
            _activation = default;
        }
    }
}
