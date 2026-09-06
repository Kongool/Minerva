// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

/*
 * The phase 2 boss is a different actor than the phase 1 boss.
 * There is a checkpoint midway through the fight where party can restart
 * if they die after phase 2 boss has been damaged.
 *
 * This module lets us change the primary actor to the phase 2 boss while
 * re-using all the components from phase 1. Without this then the module does
 * not load if party wipes after reaching phase 2.
 */
// import the components from phase 1 of the fight.
using Minerva.Shadowbringers.Trial.T04WarriorOfLightP1;

namespace Minerva.Shadowbringers.Trial.T04WarriorOfLightP2;

[SkipLocalsInit]
sealed class T04WarriorOfLightP2States : StateMachineBuilder
{
    public T04WarriorOfLightP2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TerrorUnleashed>()
            .ActivateOnEnter<SolemnConfiteor>()
            .ActivateOnEnter<ElementStayMove>()
            .ActivateOnEnter<CoruscantSaberDonut>()
            .ActivateOnEnter<CoruscantSaberCircle>()
            .ActivateOnEnter<Twincast>()
            .ActivateOnEnter<AbsoluteHoly>()
            .ActivateOnEnter<TheBitterEnd>()
            .ActivateOnEnter<RadiantBraver>()
            .ActivateOnEnter<RadiantBraverCleave>()
            .ActivateOnEnter<RadiantDesperado>()
            .ActivateOnEnter<Cauterize>()
            .ActivateOnEnter<ImbuedCoruscance>()
            .ActivateOnEnter<ImbuedCoruscanceDonut>()
            .ActivateOnEnter<ElddragonDive>()
            .ActivateOnEnter<BrimstoneEarth>()
            .ActivateOnEnter<BrimstoneEarthGrow>()
            .ActivateOnEnter<SwordOfLight>()
            .ActivateOnEnter<SuitonSan>()
            .ActivateOnEnter<RadiantMeteor>()
            .ActivateOnEnter<UltimateCrossover>()
            .ActivateOnEnter<Ascendance>()
            .ActivateOnEnter<KatonSan>()
            .ActivateOnEnter<PerfectDecimation>()
            .ActivateOnEnter<FlareBreath>()
            .ActivateOnEnter<FlareBreathAOE>()
            .ActivateOnEnter<DelugeOfDeath>();
    }
}

[ModuleInfo(CFCID = 738u, NameID = 9462u, PrimaryActorOID = (uint)OID.WarriorOfLightP2, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "wen (ported from BMR)")]

[SkipLocalsInit]
public sealed class T04WarriorOfLightP2(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f));
