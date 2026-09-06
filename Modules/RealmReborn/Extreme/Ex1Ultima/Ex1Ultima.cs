// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex1Ultima;

class RadiantPlume(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RadiantPlume, 8);
class WeightOfTheLand(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WeightOfTheLand, 6);
class Eruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Eruption, 8);

abstract class MagitekRay(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(40, 3));
class MagitekRayCenter(ModuleBase module) : MagitekRay(module, (uint)AID.MagitekRayCenter);
class MagitekRayLeft(ModuleBase module) : MagitekRay(module, (uint)AID.MagitekRayLeft);
class MagitekRayRight(ModuleBase module) : MagitekRay(module, (uint)AID.MagitekRayRight);

class AssaultCannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AssaultCannon, new AOEShapeRect(45, 1));

// TODO: homing lasers & ceruleum vent? do we care? damage is not high, and visual clutter sucks
// TODO: diffractive laser cleave? do we care?.. it's a bit problematic, since it shouldn't be active when many other mechanics are in progress, and it doesn't really add much value...
public class Ex1UltimaStates : StateMachineBuilder
{
    public Ex1UltimaStates(ModuleBase module) : base(module)
    {
        // note: we don't use phases/states to represent fight progress, because 'phase' change can happen at different time and have different impact on ability timings
        // also 'phase' change can happen mid-mechanic, in which case mechanic still runs to completion - so we can't just remove components immediately
        // garuda end is PATE 0655 on UltimaGaruda
        // titan end is PATE 0656 on UltimaTitan
        // ifrit end is PATE 0657 on UltimaIfrit
        TrivialPhase(0, 600)
            .ActivateOnEnter<ViscousAetheroplasm>()
            .ActivateOnEnter<MistralSongVulcanBurst>()
            .ActivateOnEnter<EyeOfTheStormGeocrush>()
            .ActivateOnEnter<RadiantPlume>()
            .ActivateOnEnter<WeightOfTheLand>()
            .ActivateOnEnter<Eruption>()
            .ActivateOnEnter<CrimsonCyclone>()
            .ActivateOnEnter<TankPurge>()
            .ActivateOnEnter<MagitekRayCenter>()
            .ActivateOnEnter<MagitekRayLeft>()
            .ActivateOnEnter<MagitekRayRight>()
            .ActivateOnEnter<AethericBoom>()
            .ActivateOnEnter<Aetheroplasm>()
            .ActivateOnEnter<AssaultCannon>()
            .ActivateOnEnter<Freefire>()
            .ActivateOnEnter<Ex1UltimaAI>();
    }
}

[ModuleInfo(CFCID = 68u, NameID = 2137u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex1Ultima(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 0), new ArenaBoundsCircle(20))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.MagitekBit));
    }
}
