// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex7Zeromus;

class FlareTowers(ModuleBase module) : Components.CastTowers(module, (uint)AID.FlareAOE, 5f, 4, 4);

class FlareScald(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShapeCircle _shape = new(5);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.FlareAOE:
                _aoes.Add(new(_shape, caster.Position, default, World.FutureTime(2.1d)));
                break;
            case (uint)AID.FlareScald:
            case (uint)AID.FlareKill:
                ++NumCasts;
                break;
        }
    }
}

class ProminenceSpine(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ProminenceSpine, new AOEShapeRect(60f, 5f));
class SparklingBrandingFlare(ModuleBase module) : Components.CastStackSpread(module, (uint)AID.BrandingFlareAOE, (uint)AID.SparkingFlareAOE, 4f, 4f);

class Nox(ModuleBase module) : Components.StandardChasingAOEs(module, 10f, (uint)AID.NoxAOEFirst, (uint)AID.NoxAOERest, 5.5f, 1.6d, 5, icon: (uint)IconID.Nox);
