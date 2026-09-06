// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Trial.T08Asura;

class LowerRealm(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.LowerRealm);
class Ephemerality(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Ephemerality);

class CuttingJewel(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.CuttingJewel, 4f, tankbuster: true);

class IconographyPedestalPurge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IconographyPedestalPurge, 10f);
class PedestalPurge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PedestalPurge, 60f);
class IconographyWheelOfDeincarnation(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IconographyWheelOfDeincarnation, new AOEShapeDonut(8f, 40f));
class WheelOfDeincarnation(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WheelOfDeincarnation, new AOEShapeDonut(48f, 96f));
class IconographyBladewise(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IconographyBladewise, new AOEShapeRect(50f, 3f));
class Bladewise(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Bladewise, new AOEShapeRect(100f, 14f));
class Scattering(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Scattering, new AOEShapeRect(20f, 3f));
class OrderedChaos(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.OrderedChaos, 5f);
class MyriadAspects(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.MyriadAspects1, (uint)AID.MyriadAspects2], new AOEShapeCone(40f, 15f.Degrees()), 6, 12);

class T08AsuraStates : StateMachineBuilder
{
    public T08AsuraStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<Ephemerality>()
            .ActivateOnEnter<LowerRealm>()
            .ActivateOnEnter<AsuriChakra>()
            .ActivateOnEnter<Chakra1>()
            .ActivateOnEnter<Chakra2>()
            .ActivateOnEnter<Chakra3>()
            .ActivateOnEnter<Chakra4>()
            .ActivateOnEnter<Chakra5>()
            .ActivateOnEnter<CuttingJewel>()
            .ActivateOnEnter<Laceration>()
            .ActivateOnEnter<IconographyPedestalPurge>()
            .ActivateOnEnter<PedestalPurge>()
            .ActivateOnEnter<IconographyWheelOfDeincarnation>()
            .ActivateOnEnter<WheelOfDeincarnation>()
            .ActivateOnEnter<IconographyBladewise>()
            .ActivateOnEnter<Bladewise>()
            .ActivateOnEnter<SixBladedKhadga>()
            .ActivateOnEnter<MyriadAspects>()
            .ActivateOnEnter<Scattering>()
            .ActivateOnEnter<OrderedChaos>()
            .ActivateOnEnter<ManyFaces>();
    }
}

[ModuleInfo(CFCID = 944u, NameID = 12351u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class T08Asura(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, StartingArena)
{
    private static readonly WPos arenaCenter = new(100f, 100f);
    public static readonly ArenaBoundsCustom StartingArena = new([new Polygon(arenaCenter, 19.5f * CosPI.Pi32th, 32)]);
    public static readonly ArenaBoundsCustom DefaultArena = new([new Polygon(arenaCenter, 19.165f, 32)]);
}
