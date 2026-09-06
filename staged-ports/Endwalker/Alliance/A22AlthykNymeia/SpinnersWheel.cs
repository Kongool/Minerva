// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A22AlthykNymeia;

class SpinnersWheelSelect(ModuleBase module) : ModuleComponent(module)
{
    public enum Branch { None, Gaze, StayMove }

    public Branch SelectedBranch;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var branch = status.ID switch
        {
            (uint)SID.ArcaneAttraction or (uint)SID.AttractionReversed => Branch.Gaze,
            (uint)SID.ArcaneFever or (uint)SID.FeverReversed => Branch.StayMove,
            _ => Branch.None
        };
        if (branch != Branch.None)
            SelectedBranch = branch;
    }
}

abstract class SpinnersWheelGaze(ModuleBase module, bool inverted, uint aid, uint sid) : Components.GenericGaze(module, aid)
{
    private readonly Actor? _source = module.Enemies((uint)OID.Nymeia).FirstOrDefault();
    private DateTime _activation;
    private BitMask _affected;

    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor)
    {
        if (_source != null && _affected[slot])
            return new Eye[1] { new(_source.Position, _activation, inverted: inverted) };
        return [];
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == sid)
        {
            _activation = status.ExpireAt;
            _affected[Raid.FindSlot(actor.InstanceID)] = true;
        }
    }
}
class SpinnersWheelArcaneAttraction(ModuleBase module) : SpinnersWheelGaze(module, false, (uint)AID.SpinnersWheelArcaneAttraction, (uint)SID.ArcaneAttraction);
class SpinnersWheelAttractionReversed(ModuleBase module) : SpinnersWheelGaze(module, true, (uint)AID.SpinnersWheelAttractionReversed, (uint)SID.AttractionReversed);

class SpinnersWheelStayMove(ModuleBase module) : Components.StayMove(module)
{
    public int ActiveDebuffs;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.ArcaneFever:
                if (Raid.FindSlot(actor.InstanceID) is var feverSlot && feverSlot >= 0)
                    PlayerStates[feverSlot] = new(Requirement.Stay, status.ExpireAt);
                break;
            case (uint)SID.FeverReversed:
                if (Raid.FindSlot(actor.InstanceID) is var revSlot && revSlot >= 0)
                    PlayerStates[revSlot] = new(Requirement.Move, status.ExpireAt);
                break;
            case (uint)SID.Pyretic:
            case (uint)SID.FreezingUp:
                ++ActiveDebuffs;
                break;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.Pyretic or (uint)SID.FreezingUp)
        {
            --ActiveDebuffs;
            if (Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
                PlayerStates[slot] = default;
        }
    }
}
