// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

// component for infralateral arc mechanic (role stacks)
class InfralateralArc(ModuleBase module) : Components.CastCounter(module, (uint)AID.InfralateralArcAOE)
{
    private static readonly Angle _coneHalfAngle = 45.Degrees();

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var pcRole = EffectiveRole(actor);
        var pcDir = Angle.FromDirection(actor.Position - Module.PrimaryActor.Position);
        if (Raid.WithoutSlot(false, true, true).Any(a => EffectiveRole(a) != pcRole && a.Position.InCone(Module.PrimaryActor.Position, pcDir, _coneHalfAngle)))
            hints.Add("Spread by roles!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var pcRole = EffectiveRole(pc);
        var pcDir = Angle.FromDirection(pc.Position - Module.PrimaryActor.Position);
        foreach (var actor in Raid.WithoutSlot(false, true, true).Where(a => EffectiveRole(a) != pcRole))
            Arena.Actor(actor, actor.Position.InCone(Module.PrimaryActor.Position, pcDir, _coneHalfAngle) ? Colors.Danger : Colors.PlayerGeneric);
    }

    private static Role EffectiveRole(Actor a) => a.Role == Role.Ranged ? Role.Melee : a.Role;
}
