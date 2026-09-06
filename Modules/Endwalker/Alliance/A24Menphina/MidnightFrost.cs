// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A24Menphina;

class MidnightFrostWaxingClaw(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCone _shape = new(60f, 90f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.MidnightFrostShortNormalFrontAOE:
            case (uint)AID.MidnightFrostShortNormalBackAOE:
            case (uint)AID.MidnightFrostShortMountedFrontAOE:
            case (uint)AID.MidnightFrostShortMountedBackAOE:
            case (uint)AID.MidnightFrostLongMountedFrontAOE:
            case (uint)AID.MidnightFrostLongMountedBackAOE:
            case (uint)AID.MidnightFrostLongDismountedFrontAOE:
            case (uint)AID.MidnightFrostLongDismountedBackAOE:
            case (uint)AID.WaxingClawRight:
            case (uint)AID.WaxingClawLeft:
                _aoes.Add(new(_shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.MidnightFrostShortNormalFrontAOE:
            case (uint)AID.MidnightFrostShortNormalBackAOE:
            case (uint)AID.MidnightFrostShortMountedFrontAOE:
            case (uint)AID.MidnightFrostShortMountedBackAOE:
            case (uint)AID.MidnightFrostLongMountedFrontAOE:
            case (uint)AID.MidnightFrostLongMountedBackAOE:
            case (uint)AID.MidnightFrostLongDismountedFrontAOE:
            case (uint)AID.MidnightFrostLongDismountedBackAOE:
            case (uint)AID.WaxingClawRight:
            case (uint)AID.WaxingClawLeft:
                ++NumCasts;
                _aoes.Clear();
                break;
        }
    }
}
