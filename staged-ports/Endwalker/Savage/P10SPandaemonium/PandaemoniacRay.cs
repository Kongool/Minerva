// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P10SPandaemonium;

class PandaemoniacRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PandaemoniacRayAOE, new AOEShapeRect(30f, 25f));

class JadePassage(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.JadePassage)
{
    private readonly DateTime _activation = module.World.FutureTime(3.6d);

    private readonly AOEShapeRect _shape = new(40f, 1f, 40f);

    private static Actor[] GetSpheres(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.ArcaneSphere);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (!z.IsDead)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var spheres = GetSpheres(Module);
        var count = spheres.Length;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var s = spheres[i];
            aoes[i] = new(_shape, s.Position, s.Rotation, Module.CastFinishAt(s.CastInfo, 0, _activation));
        }
        return aoes;
    }
}
