// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C012Gladiator;

sealed class CurseOfTheFallen(ModuleBase module) : Components.UniformStackSpread(module, 5f, 6f, 3, 3)
{
    private readonly List<Actor> _fallen = [];
    private Actor? _thunderous;
    private BitMask _lingering;
    private DateTime _spreadResolve;
    private DateTime _stackResolve;
    private bool _dirty;

    public override void Update()
    {
        if (_dirty)
        {
            _dirty = false;

            Spreads.Clear();
            Stacks.Clear();

            if (_fallen.Count > 0 && (_thunderous == null || _spreadResolve < _stackResolve))
            {
                AddSpreads(_fallen, _spreadResolve);
            }
            else if (_thunderous != null && (_fallen.Count == 0 || _stackResolve < _spreadResolve))
            {
                AddStack(_thunderous, _stackResolve, _lingering);
            }
        }
        base.Update();
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.EchoOfTheFallen:
                _fallen.Add(actor);
                _spreadResolve = status.ExpireAt;
                _dirty = true;
                break;
            case (uint)SID.ThunderousEcho:
                _thunderous = actor;
                _stackResolve = status.ExpireAt;
                _dirty = true;
                break;
            case (uint)SID.LingeringEchoes:
                _lingering.Set(Raid.FindSlot(actor.InstanceID));
                _dirty = true;
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.NEchoOfTheFallen:
            case (uint)AID.SEchoOfTheFallen:
                _fallen.RemoveAll(a => a.InstanceID == spell.MainTargetID);
                _dirty = true;
                break;
            case (uint)AID.NThunderousEcho:
            case (uint)AID.SThunderousEcho:
                _thunderous = null;
                _dirty = true;
                break;
            case (uint)AID.NLingeringEcho:
            case (uint)AID.SLingeringEcho:
                _lingering.Reset();
                _dirty = true;
                break;
        }
    }
}

abstract class RingOfMight1Out(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 8f);
sealed class NRingOfMight1Out(ModuleBase module) : RingOfMight1Out(module, (uint)AID.NRingOfMight1Out);
sealed class SRingOfMight1Out(ModuleBase module) : RingOfMight1Out(module, (uint)AID.SRingOfMight1Out);

abstract class RingOfMight2Out(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 13f);
sealed class NRingOfMight2Out(ModuleBase module) : RingOfMight2Out(module, (uint)AID.NRingOfMight2Out);
sealed class SRingOfMight2Out(ModuleBase module) : RingOfMight2Out(module, (uint)AID.SRingOfMight2Out);

abstract class RingOfMight3Out(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 18f);
sealed class NRingOfMight3Out(ModuleBase module) : RingOfMight3Out(module, (uint)AID.NRingOfMight3Out);
sealed class SRingOfMight3Out(ModuleBase module) : RingOfMight3Out(module, (uint)AID.SRingOfMight3Out);

abstract class RingOfMight1In(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(8f, 30f));
sealed class NRingOfMight1In(ModuleBase module) : RingOfMight1In(module, (uint)AID.NRingOfMight1In);
sealed class SRingOfMight1In(ModuleBase module) : RingOfMight1In(module, (uint)AID.SRingOfMight1In);

abstract class RingOfMight2In(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(13f, 30f));
sealed class NRingOfMight2In(ModuleBase module) : RingOfMight2In(module, (uint)AID.NRingOfMight2In);
sealed class SRingOfMight2In(ModuleBase module) : RingOfMight2In(module, (uint)AID.SRingOfMight2In);

abstract class RingOfMight3In(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(18f, 30f));
sealed class NRingOfMight3In(ModuleBase module) : RingOfMight3In(module, (uint)AID.NRingOfMight3In);
sealed class SRingOfMight3In(ModuleBase module) : RingOfMight3In(module, (uint)AID.SRingOfMight3In);
