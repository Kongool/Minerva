// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel3Sartauvoir;

sealed class Pyrolatry(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Pyrolatry);
sealed class Flashover(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Flashover, 19f);
sealed class FlamingRain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlamingRain, 6f);
sealed class PillarOfFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PillarOfFlame, 8f);
sealed class TimeEruption(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TimeEruption1, (uint)AID.TimeEruption2], new AOEShapeRect(20f, 10f), 2, 4);
sealed class ThermalGust(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThermalGust, new AOEShapeRect(44f, 5f));
sealed class SearingWind(ModuleBase module) : Components.Voidzone(module, 3f, GetSearingWind)
{
    private static List<Actor> GetSearingWind(ModuleBase module) => module.Enemies((uint)OID.Peri);
}

sealed class Backdraft(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Backdraft, 16f, true)
{
    private static readonly Angle a45 = 45f.Degrees(), a90 = 90f.Degrees(), a225 = 22.5f.Degrees();

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var forbidden = new ShapeDistance[4];
            for (var i = 0; i < 4; ++i)
            {
                forbidden[i] = new SDInvertedCone(c.Origin, 5f, a45 + a90 * i, a225);
            }
            hints.AddForbiddenZone(new SDIntersection(forbidden), c.Activation);
        }
    }
}

[ModuleInfo(Group = ModuleGroup.BozjaDuel, GroupID = 735u, CFCID = 735u, NameID = 12u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class Duel3Sartauvoir(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-15f, 145f), new ArenaBoundsSquare(18f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InSquare(Center, 20f);
}