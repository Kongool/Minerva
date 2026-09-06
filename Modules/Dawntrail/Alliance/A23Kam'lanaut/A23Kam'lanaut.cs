// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A23Kamlanaut;

sealed class ElementalBladeWide(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.IceBladeWide, (uint)AID.LightningBladeWide, (uint)AID.FireBladeWide,
(uint)AID.EarthBladeWide, (uint)AID.WaterBladeWide, (uint)AID.WindBladeWide], new AOEShapeRect(80f, 10f), riskyWithSecondsLeft: 6d);
sealed class ElementalBladeNarrow(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.IceBladeNarrow, (uint)AID.LightningBladeNarrow, (uint)AID.FireBladeNarrow,
(uint)AID.EarthBladeNarrow, (uint)AID.WaterBladeNarrow, (uint)AID.WindBladeNarrow], new AOEShapeRect(80f, 2.5f), riskyWithSecondsLeft: 6d);
sealed class ElementalResonance(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElementalResonance, 18f, riskyWithSecondsLeft: 6d);
sealed class SublimeElementsWide(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SublimeIceWide, (uint)AID.SublimeLightningWide, (uint)AID.SublimeFireWide,
(uint)AID.SublimeEarthWide, (uint)AID.SublimeWaterWide, (uint)AID.SublimeWindWide], new AOEShapeCone(40f, 50f.Degrees()), riskyWithSecondsLeft: 6d);
sealed class SublimeElementsNarrow(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SublimeIceNarrow, (uint)AID.SublimeLightningNarrow, (uint)AID.SublimeFireNarrow,
(uint)AID.SublimeEarthNarrow, (uint)AID.SublimeWaterNarrow, (uint)AID.SublimeWindNarrow], new AOEShapeCone(40f, 10f.Degrees()), riskyWithSecondsLeft: 6d);
sealed class EmpyrealBanishIV(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.EmpyrealBanishIV, 5f, PartyState.MaxAllianceSize, PartyState.MaxAllianceSize);
sealed class EmpyrealBanishIII(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.EmpyrealBanishIII, 5f);
sealed class GreatWheelCircle(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.GreatWheelCircle1, (uint)AID.GreatWheelCircle2,
(uint)AID.GreatWheelCircle3, (uint)AID.GreatWheelCircle4], 10f);
sealed class GreatWheelCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GreatWheelCone, new AOEShapeCone(80f, 90f.Degrees()));
sealed class LightBladeIllumedEstoc(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.IllumedEstoc, (uint)AID.LightBlade], new AOEShapeRect(120f, 6.5f), riskyWithSecondsLeft: 3.5d)
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = Casters.Count;
        if (count == 0)
        {
            return [];
        }
        if (count != 2)
        {
            return base.ActiveAOEs(slot, actor);
        }
        return CollectionsMarshal.AsSpan(Casters);
    }
}

sealed class TranscendentUnion(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.TranscendentUnionVisual, (uint)AID.TranscendentUnion, 6.6d, "Raidwide x7");
sealed class EnspiritedSwordplayShockwave(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.EnspiritedSwordplay, (uint)AID.Shockwave]);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1058u, CFCID = 1058u, NameID = 14043u, PrimaryActorOID = (uint)OID.Kamlanaut, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class A23Kamlanaut : ModuleBase
{
    public A23Kamlanaut(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private A23Kamlanaut(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static Polygon[] BuildP1Circle() => [new(new(-200f, 150f), 29.5f, 128)]; // arena circle actually got 512 vertices, but 128 is a good enough approximation for this use case;
    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom(BuildP1Circle());
        return (arena.Center, arena);
    }
}
