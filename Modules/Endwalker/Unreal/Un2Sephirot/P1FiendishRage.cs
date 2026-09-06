// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un2Sephirot;

class P1FiendishRage(ModuleBase module) : Components.CastCounter(module, (uint)AID.FiendishRage)
{
    private BitMask _targets;

    private const float _range = 6;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_targets.Any())
        {
            var numClips = Raid.WithSlot(true, true, true).IncludedInMask(_targets).InRadius(actor.Position, _range).Count();
            if (Module.PrimaryActor.TargetID == actor.InstanceID)
            {
                if (numClips > 0)
                {
                    hints.Add("GTFO from marked players!");
                }
            }
            else if (numClips != 1)
            {
                hints.Add("Stack with single group!");
            }
        }
    }

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
    {
        return _targets[playerSlot] ? PlayerPriority.Danger : PlayerPriority.Irrelevant;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var target in Raid.WithSlot(true, true, true).IncludedInMask(_targets))
            Arena.ZoneCircleOutline(target.Item2.Position, _range, Colors.Danger);
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if ((IconID)iconID == IconID.FiendishRage)
            _targets.Set(Raid.FindSlot(actor.InstanceID));
    }
}
