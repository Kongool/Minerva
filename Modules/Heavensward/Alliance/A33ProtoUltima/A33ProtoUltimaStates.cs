// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A33ProtoUltima;

class A33ProtoUltimaStates : StateMachineBuilder
{
    public A33ProtoUltimaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<WreckingBall>()
            .ActivateOnEnter<AetherochemicalFlare>()
            .ActivateOnEnter<AetherochemicalLaser1>()
            .ActivateOnEnter<AetherochemicalLaser2>()
            .ActivateOnEnter<AetherochemicalLaser3>()
            .ActivateOnEnter<CitadelBuster2>()
            .ActivateOnEnter<FlareStar>()
            .ActivateOnEnter<Rotoswipe>();
    }
}
