// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS1TrinitySeeker;

sealed class MercifulMoon(ModuleBase module) : Components.GenericGaze(module, (uint)AID.MercifulMoon)
{
    private Eye[] _eye = [];

    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor) => _eye;

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.AetherialOrb)
        {
            _eye = [new(actor.Position, World.FutureTime(5.8d))];
        }
    }
}
