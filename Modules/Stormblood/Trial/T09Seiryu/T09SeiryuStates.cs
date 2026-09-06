// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T09Seiryu;

sealed class T09SeiryuStates : StateMachineBuilder
{
    public T09SeiryuStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<BlueBolt>()
            .ActivateOnEnter<RedRush>()
            .ActivateOnEnter<HundredTonzeSwing>()
            .ActivateOnEnter<Handprint>()
            .ActivateOnEnter<CoursingRiver>()
            .ActivateOnEnter<DragonsWake>()
            .ActivateOnEnter<FifthElement>()
            .ActivateOnEnter<FortuneBladeSigil>()
            .ActivateOnEnter<InfirmSoul>()
            .ActivateOnEnter<KanaboAOE>()
            .ActivateOnEnter<KanaboBait>()
            .ActivateOnEnter<OnmyoSerpentEyeSigil>()
            .ActivateOnEnter<SerpentDescending>()
            .ActivateOnEnter<YamaKagura>()
            .ActivateOnEnter<ForbiddenArts>()
            .ActivateOnEnter<SerpentAscending>()
            .ActivateOnEnter<ForceOfNature1>()
            .ActivateOnEnter<ForceOfNature2>();
    }
}
