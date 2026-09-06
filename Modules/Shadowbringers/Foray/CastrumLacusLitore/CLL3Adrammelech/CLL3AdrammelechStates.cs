// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL3Adrammelech;

sealed class CLL3AdrammelechStates : StateMachineBuilder
{
    public CLL3AdrammelechStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<HolyIV>()
            .ActivateOnEnter<WaterIV1>()
            .ActivateOnEnter<WaterIV3>()
            .ActivateOnEnter<BlizzardIV>()
            .ActivateOnEnter<FireIV>()
            .ActivateOnEnter<Flare>()
            .ActivateOnEnter<BurstIITornado>()
            .ActivateOnEnter<Shock>()
            .ActivateOnEnter<WarpedLight>()
            .ActivateOnEnter<ThunderIV>()
            .ActivateOnEnter<AeroIV>()
            .ActivateOnEnter<Twister>()
            .ActivateOnEnter<StoneIV>();
    }
}
