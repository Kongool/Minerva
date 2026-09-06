// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Quest.MSQ.Endwalker;

sealed class AkhMorn(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private DateTime _activation;
    private static readonly AOEShapeCircle circle = new(4f);

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_activation != default)
            hints.Add($"Tankbuster x{NumExpectedCasts()}");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_activation != default)
            hints.AddPredictedDamage(new(1), _activation);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.AkhMorn)
        {
            CurrentBaits.Add(new(Module.PrimaryActor, Raid.Player()!, circle));
            _activation = Module.CastFinishAt(spell);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.AkhMorn)
            ++NumCasts;
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.AkhMornVisual)
        {
            ++NumCasts;
            if (NumCasts == NumExpectedCasts())
            {
                CurrentBaits.Clear();
                NumCasts = 0;
                _activation = default;
            }
        }
    }

    private int NumExpectedCasts() => Module.PrimaryActor.IsDead ? 8 : 6;
}
