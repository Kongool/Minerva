// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.TOP;

sealed class P6FlashGale : Components.GenericBaitAway
{
    private readonly Actor? _source;

    private static readonly AOEShapeCircle _shape = new(5);

    public P6FlashGale(ModuleBase module) : base(module, centerAtTarget: true)
    {
        _source = module.Enemies((uint)OID.BossP6)[0];
        ForbiddenPlayers = Raid.WithSlot(true, true, true).WhereActor(p => p.Role != Role.Tank).Mask();
    }

    public override void Update()
    {
        CurrentBaits.Clear();
        if (_source != null)
        {
            var mainTarget = World.Actors.Find(_source.TargetID);
            var farTarget = Raid.WithoutSlot(false, true, true).Farthest(_source.Position);
            if (mainTarget != null)
                CurrentBaits.Add(new(_source, mainTarget, _shape));
            if (farTarget != null)
                CurrentBaits.Add(new(_source, farTarget, _shape));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.FlashGale)
            ++NumCasts;
    }
}
