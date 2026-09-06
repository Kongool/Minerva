// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex4Ifrit;

// note on mechanic: typically boss casts inferno howl, then target gains status, then a total of 3 searing wind casts are baited on target
// however, sometimes (typically on phase switches) boss might cast new inferno howl while previous target still has debuff with large timer
// in such case old target will not have any more searing winds cast on it, despite having debuff
// TODO: verify whether searing wind on previous target can still be cast if inferno howl is in progress?
class SearingWind(ModuleBase module) : Components.UniformStackSpread(module, default, 14f)
{
    public override bool KeepOnPhaseChange => true;
    private int _searingWindsLeft;
    private DateTime _showHintsAfter = DateTime.MaxValue;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // we let AI provide soft positioning hints until resolve is imminent
        if (World.CurrentTime > _showHintsAfter)
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.InfernoHowl)
        {
            Spreads.Clear();
            if (World.Actors.Find(spell.TargetID) is var target && target != null)
                AddSpread(target, World.FutureTime(5.4d));
            _searingWindsLeft = 3;
            _showHintsAfter = World.FutureTime(3.4d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        // note: there are 3 casts total, 6s apart - last one happens ~4.8s before status expires
        if (spell.Action.ID == (uint)AID.SearingWind)
        {
            if (--_searingWindsLeft == 0)
            {
                Spreads.Clear();
                _showHintsAfter = DateTime.MaxValue;
            }
            else
            {
                foreach (ref var s in Spreads.AsSpan())
                    s.Activation = World.FutureTime(6d);
            }
        }
    }
}
