// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex1Valigarmanda;

sealed class ThunderousBreath : Components.CastCounter
{
    public ThunderousBreath(ModuleBase module) : base(module, (uint)AID.ThunderousBreathAOE)
    {
        var platform = module.FindComponent<ThunderPlatform>();
        if (platform != null)
        {
            var party = module.Raid.WithSlot(true, true, true);
            var len = party.Length;
            for (var i = 0; i < len; ++i)
            {
                var slot = party[i].Item1;
                platform.RequireHint[slot] = platform.RequireLevitating[slot] = true;
            }
        }
    }
}

sealed class ArcaneLighning(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.ArcaneLightning)
{
    public readonly List<AOEInstance> AOEs = [];

    private readonly AOEShapeRect rect = new(50f, 2.5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.ArcaneSphere)
        {
            var pos = actor.Position.Quantized();
            var rot = actor.Rotation;
            AOEs.Add(new(rect, pos, rot, World.FutureTime(8.6d), shapeDistance: rect.Distance(pos, rot)));
        }
    }
}
