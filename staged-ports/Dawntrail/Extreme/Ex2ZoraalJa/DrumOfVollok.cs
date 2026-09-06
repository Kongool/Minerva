// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex2ZoraalJa;

sealed class DrumOfVollokPlatforms(ModuleBase module) : ModuleComponent(module)
{
    public bool Active;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index != 0x0B)
        {
            return;
        }
        var a135 = 135f.Degrees();
        var center = new WPos(100f, 100f);
        switch (state)
        {
            case 0x00800040u:
                var dir135 = 15f * a135.ToDirection();
                var arenaNW = new ArenaBoundsCustom([new Square(center - dir135, 10f, a135), new Square(center + dir135, 10f, a135)], ScaleFactor: 1.24f);
                Bounds = arenaNW;
                Center = arenaNW.Center;
                Active = true;
                break;
            case 0x02000100u:
                var dirM135 = 15f * (-a135).ToDirection();
                var arenaNE = new ArenaBoundsCustom([new Square(center - dirM135, 10f, -a135), new Square(center + dirM135, 10f, -a135)], ScaleFactor: 1.24f);
                Bounds = arenaNE;
                Center = arenaNE.Center;
                Active = true;
                break;
        }
    }
}

sealed class DrumOfVollok(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DrumOfVollokAOE, 4f, 2, 2);

sealed class DrumOfVollokKnockback(ModuleBase module) : Components.GenericKnockback(module)
{
    private readonly DrumOfVollok? _main = module.FindComponent<DrumOfVollok>();

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_main == null)
        {
            return [];
        }
        var count = _main.Stacks.Count;
        for (var i = 0; i < count; ++i)
        {
            if (_main.Stacks[i].Target == actor)
            {
                return [];
            }
        }
        var sources = new List<Knockback>();
        for (var i = 0; i < count; ++i)
        {
            var s = _main.Stacks[i];
            if (actor.Position.InCircle(s.Target.Position, s.Radius))
            {
                sources.Add(new(s.Target.Position, 25f, s.Activation, ignoreImmunes: true));
            }
        }
        return CollectionsMarshal.AsSpan(sources);
    }
}
