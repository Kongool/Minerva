// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A24TheCompound2P;

sealed class A24TheCompound2PStates : StateMachineBuilder
{
    public A24TheCompound2PStates(A24TheCompound2P module) : base(module)
    {
        TrivialPhase(default)
            .ActivateOnEnter<MechanicalLaceration>()
            .ActivateOnEnter<MechanicalDissection>()
            .ActivateOnEnter<MechanicalDecapitation>()
            .ActivateOnEnter<MechanicalContusionAOE>()
            .ActivateOnEnter<MechanicalContusionSpread>()
            .ActivateOnEnter<IncongruousSpin>()
            .ActivateOnEnter<MechanicalLacerationPhaseChange>()
            .Raw.Update = () => module.PrimaryActor.IsDestroyed || (module.BossP2?.IsTargetable ?? false);
        TrivialPhase(1u)
            .ActivateOnEnter<CentrifugalSlice>()
            .ActivateOnEnter<PrimeBladeCircle>()
            .ActivateOnEnter<PrimeBladeRect>()
            .ActivateOnEnter<PrimeBladeDonut>()
            .ActivateOnEnter<RelentlessSpiral>()
            .ActivateOnEnter<PrimeBladeTransfer>()
            .ActivateOnEnter<RelentlessSpiralTransfer>()
            .ActivateOnEnter<ThreePartsDisdainStack>()
            .ActivateOnEnter<ThreePartsDisdainKnockback>()
            .ActivateOnEnter<R012LaserAOE>()
            .ActivateOnEnter<R012LaserSpread>()
            .ActivateOnEnter<R012LaserTB>()
            .ActivateOnEnter<R011Laser>()
            .ActivateOnEnter<EnergyCompression>()
            .ActivateOnEnter<FourPartsResolveCircle>()
            .ActivateOnEnter<FourPartsResolveRect>()
            .Raw.Update = () => module.PrimaryActor.IsDestroyed && (module.BossP2?.IsDeadOrDestroyed ?? true);
    }
}
