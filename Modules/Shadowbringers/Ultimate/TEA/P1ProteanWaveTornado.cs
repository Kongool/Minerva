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
abstract class P1ProteanWaveTornado : Components.GenericBaitAway
{
    private readonly List<Actor> _liquidRage;

    public P1ProteanWaveTornado(ModuleBase module, bool enableHints) : base(module, (uint)AID.ProteanWaveTornadoInvis)
    {
        _liquidRage = module.Enemies((uint)OID.LiquidRage);
        EnableHints = enableHints;
    }

    public override void Update()
    {
        CurrentBaits.Clear();
        foreach (var tornado in _liquidRage)
        {
            var target = Raid.WithoutSlot(false, true, true).Closest(tornado.Position);
            if (target != null)
                CurrentBaits.Add(new(tornado, target, P1ProteanWaveLiquid.Cone));
        }
    }
}

[SkipLocalsInit]
sealed class P1ProteanWaveTornadoVisCast(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ProteanWaveTornadoVis, P1ProteanWaveLiquid.Cone);
[SkipLocalsInit]
sealed class P1ProteanWaveTornadoVisBait(ModuleBase module) : P1ProteanWaveTornado(module, false);
[SkipLocalsInit]
sealed class P1ProteanWaveTornadoInvis(ModuleBase module) : P1ProteanWaveTornado(module, true);
