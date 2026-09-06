// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA1Owain;

sealed class PiercingLight1(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.PiercingLight1, 6f);
sealed class PiercingLight2(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.PiercingLight2, 6f);

sealed class Spiritcull(ModuleBase module) : Components.GenericStackSpread(module)
{
    private readonly List<Actor> targets = [];

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.DoritoStack)
        {
            targets.Add(actor);
            if (Stacks.Count == 0)
                Stacks.Add(new(actor, 1.5f, 24, 24, activation: World.FutureTime(5.1d)));
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.BloodSacrifice)
        {
            targets.Clear();
            Stacks.Clear();
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (targets.Contains(actor))
            base.AddHints(slot, actor, hints);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (targets.Contains(actor))
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (Stacks.Count != 0 && targets.Contains(pc))
        {
            Actor? actor = null;
            var minDistanceSq = float.MaxValue;
            var count = targets.Count;
            for (var i = 0; i < count; ++i)
            {
                var target = targets[i];
                if (target == pc)
                    continue;
                var distanceSq = (pc.Position - target.Position).LengthSq();
                if (distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    actor = target;
                }
            }
            Stacks[0] = Stacks[0] with { Target = actor ?? pc };

            base.DrawArenaForeground(pcSlot, pc);
        }
    }
}
