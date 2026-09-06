// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P1ProteanWaveLiquid(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.ProteanWaveLiquidVisBoss, (uint)AID.ProteanWaveLiquidVisHelper], Cone)
{
    public static readonly AOEShapeCone Cone = new(40f, 15f.Degrees());
}

// single protean ("shadow") that fires in the direction the boss is facing
[SkipLocalsInit]
sealed class P1ProteanWaveLiquidInvisFixed(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.ProteanWaveLiquidInvisBoss)
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return new AOEInstance[1] { new(P1ProteanWaveLiquid.Cone, Module.PrimaryActor.Position, Module.PrimaryActor.Rotation) };
    }
}

// proteans baited on 4 closest targets
[SkipLocalsInit]
sealed class P1ProteanWaveLiquidInvisBaited(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.ProteanWaveLiquidInvisHelper)
{
    public override void Update()
    {
        CurrentBaits.Clear();
        foreach (var target in Raid.WithoutSlot(false, true, true).SortedByRange(Module.PrimaryActor.Position).Take(4))
            CurrentBaits.Add(new(Module.PrimaryActor, target, P1ProteanWaveLiquid.Cone));
    }
}
