// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Criterion.C01AMT.C013PariOfPlenty;

class SpurningFlames(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SpurningFlames);

class ScouringScorn(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ScouringScorn);

class ImpassionedSparks(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ImpassionedSparks3, new AOEShapeCircle(8f), 8);

class BurningPillar(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.BurningPillar)
        {
            aoes.Add(new AOEInstance(new AOEShapeCircle(10f), caster.Position, default, default, Colors.Danger));
            NumCasts++;
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return CollectionsMarshal.AsSpan(aoes);
    }
}

class BurningPillarSpreads(ModuleBase module) : Components.IconStackSpread(module, default, (uint)IconID.HotFootSpread, default, (uint)AID.HotFoot, default, 10f, default);

class FireChains(ModuleBase module) : Components.Chains(module, (uint)TetherID.FireChain, default, 15F);