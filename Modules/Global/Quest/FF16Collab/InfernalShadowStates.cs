// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.Quest.FF16Collab.InfernalShadow;

class InfernalShadowStates : StateMachineBuilder
{
    public InfernalShadowStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<VulcanBurst>()
            .ActivateOnEnter<Pyrosault>()
            .ActivateOnEnter<Incinerate>()
            .ActivateOnEnter<SpreadingFire>()
            .ActivateOnEnter<SmolderingClaw>()
            .ActivateOnEnter<TailStrike>()
            .ActivateOnEnter<CrimsonRush>()
            .ActivateOnEnter<Fireball>()
            .ActivateOnEnter<CrimsonStreak>()
            .ActivateOnEnter<Hellfire>()
            .ActivateOnEnter<FireRampageCleave>()
            .ActivateOnEnter<FieryRampageCircle>()
            .ActivateOnEnter<FieryRampageRaidwide>()
            .ActivateOnEnter<Eruption>()
            .ActivateOnEnter<Eruption2>()
            .ActivateOnEnter<BurningStrike>()
            .ActivateOnEnter<SearingStomp>()
            .Raw.Update = () => Module.PrimaryActor.HPMP.CurHP == 1 || Module.PrimaryActor.IsDeadOrDestroyed;
    }
}
