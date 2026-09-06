// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Quantum.Q1FinalVerse;

sealed class Quantumlevel(ModuleBase module) : ModuleComponent(module)
{
    public uint QuantumLevel;
    private readonly Q1FinalVerse bossmod = (Q1FinalVerse)module;

    public override void Update()
    {
        if (bossmod.BossEater == null || QuantumLevel == 40u)
        {
            return;
        }
        QuantumLevel = default;
        if (bossmod.BossEater.FindStatus((uint)SID.LightDamageUp) is ActorStatus light)
        {
            QuantumLevel += light.Extra;
        }
        if (Module.PrimaryActor.FindStatus((uint)SID.DarkDamageUp) is ActorStatus dark)
        {
            QuantumLevel += dark.Extra;
        }
        if (Module.PrimaryActor.FindStatus((uint)SID.PhysicalDamageUp) is ActorStatus dmg)
        {
            QuantumLevel += dmg.Extra;
        }
        if (Module.PrimaryActor.FindStatus((uint)SID.FireDamageUp) is ActorStatus fire)
        {
            QuantumLevel += fire.Extra;
        }
        if (Module.PrimaryActor.FindStatus((uint)SID.HPBoost) is ActorStatus hp)
        {
            QuantumLevel += hp.Extra;
        }
    }
}
