// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex4Ifrit;

// TODO: revise & generalize to 'baited aoe' component, with nice utilities for AI
class Eruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EruptionAOE, Radius)
{
    private DateTime _baitDetectDeadline;
    public BitMask Baiters;

    public const float Radius = 8f;

    public override void Update()
    {
        if (Casters.Count == 0)
            Baiters.Reset();
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        base.OnCastStarted(caster, spell);
        switch (spell.Action.ID)
        {
            case (uint)AID.Eruption:
                _baitDetectDeadline = World.FutureTime(1d);
                break;
            case (uint)AID.EruptionAOE:
                if (World.CurrentTime < _baitDetectDeadline)
                {
                    var baiter = Raid.WithoutSlot(false, true, true).Closest(spell.LocXZ);
                    if (baiter != null)
                        Baiters.Set(Raid.FindSlot(baiter.InstanceID));
                }
                break;
        }
    }
}
