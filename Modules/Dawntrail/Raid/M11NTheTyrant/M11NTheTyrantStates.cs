// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Raid.M11NTheTyrant;

sealed class M11NTheTyrantStates : StateMachineBuilder
{
    public M11NTheTyrantStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CrownOfArcadia>()
            .ActivateOnEnter<Smashdown1>()
            .ActivateOnEnter<Smashdown2>()
            .ActivateOnEnter<Smashdown3>()
            .ActivateOnEnter<VoidStardust>()
            .ActivateOnEnter<Cometite>()
            .ActivateOnEnter<AssaultEvolved1>()
            .ActivateOnEnter<AssaultEvolved2>()
            .ActivateOnEnter<AssaultEvolved3>()
            .ActivateOnEnter<DanceOfDomination>()
            .ActivateOnEnter<Explosion1>()
            .ActivateOnEnter<Explosion2>()
            .ActivateOnEnter<Explosion3>()
            .ActivateOnEnter<RawSteelTankBuster>()
            .ActivateOnEnter<RawSteelSpreads>()
            .ActivateOnEnter<Charybdistopia>()
            .ActivateOnEnter<Maelstrom>()
            .ActivateOnEnter<PowerfulGust>()
            .ActivateOnEnter<OneAndOnly>()
            .ActivateOnEnter<CosmicKiss>()
            .ActivateOnEnter<MassiveMeteor>()
            .ActivateOnEnter<ForegoneFatality>()
            .ActivateOnEnter<DoubleTyrannhilation>()
            .ActivateOnEnter<HiddenTyrannhilation>()
            .ActivateOnEnter<Flatliner>()
            .ActivateOnEnter<FlatlinerKnockUp>()
            .ActivateOnEnter<MajesticMeteor>()
            .ActivateOnEnter<MajesticMeteorain>()
            .ActivateOnEnter<FireAndFury1>()
            .ActivateOnEnter<FireAndFury2>()
            .ActivateOnEnter<MammothMeteor>()
            .ActivateOnEnter<ExplosionTower>()
            .ActivateOnEnter<ExplosionKnockUp>()

            .ActivateOnEnter<ArcadionAvalanche>()
            .ActivateOnEnter<ArcadionAvalancheToss>()
            .ActivateOnEnter<HeartbreakKick>()
            .ActivateOnEnter<GreatWallOfFire>();
    }
}
