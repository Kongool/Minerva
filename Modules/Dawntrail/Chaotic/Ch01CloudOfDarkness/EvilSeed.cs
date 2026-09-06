// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class EvilSeedBait(ModuleBase module) : ModuleComponent(module)
{
    public BitMask Baiters;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var p in Raid.WithSlot(false, false, true).IncludedInMask(Baiters).Actors())
            Arena.ZoneCircleOutline(p.Position, 5f);
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.EvilSeed)
            Baiters.Set(Raid.FindSlot(actor.InstanceID));
    }
}

sealed class EvilSeedAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EvilSeedAOE, 5f);

sealed class EvilSeedVoidzone(ModuleBase module) : Components.Voidzone(module, 5f, module => module.Enemies((uint)OID.EvilSeed).Where(z => z.EventState != 7));

sealed class ThornyVine(ModuleBase module) : Components.Chains(module, (uint)TetherID.ThornyVine, default, 25f)
{
    public BitMask Targets;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.ThornyVineBait)
            Targets[Raid.FindSlot(actor.InstanceID)] = true;
    }
}
