// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P2Flarethrower(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.FlarethrowerP2AOE)
{
    private Actor? _source;

    private readonly AOEShapeCone _shape = new(100f, 45f.Degrees());

    public override void Update()
    {
        CurrentBaits.Clear();
        if (_source != null && Raid.WithoutSlot(false, true, true).Closest(_source.Position) is var target && target != null)
            CurrentBaits.Add(new(_source, target, _shape));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (_source != null && CurrentBaits.Any(b => b.Target == actor) && Module.Enemies((uint)OID.LiquidRage).Any(r => !_shape.Check(r.Position, _source.Position, Angle.FromDirection(actor.Position - _source.Position))))
            hints.Add("Aim towards tornado!");
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FlarethrowerP2)
        {
            _source = caster;
            ForbiddenPlayers = Raid.WithSlot(true, true, true).WhereActor(a => a.InstanceID != caster.TargetID).Mask(); // TODO: unsure about this... assumes BJ main target should bait
        }
    }
}
