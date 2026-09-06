// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V22Moko;

sealed class AzureAuspice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AzureAuspice, new AOEShapeDonut(6f, 60f));
sealed class KenkiReleaseMoonlessNight(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.KenkiRelease, (uint)AID.MoonlessNight]);
sealed class IronRain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IronRain, 10f);
sealed class Unsheathing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Unsheathing, 3f);
sealed class VeilSever(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VeilSever, new AOEShapeRect(40f, 2.5f));
sealed class ScarletAuspice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScarletAuspice, 6f);
sealed class Clearout(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Clearout, new AOEShapeCone(22f, 90f.Degrees()));
sealed class BoundlessScarletAzure(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BoundlessScarlet, (uint)AID.BoundlessAzure], new AOEShapeRect(60f, 5f));

sealed class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, new AOEShapeRect(60f, 15f), 2)
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = Casters.Count;
        if (count == 0)
        {
            return [];
        }
        var aoes = CollectionsMarshal.AsSpan(Casters);

        var hasDifferentRotations = false;
        if (count > 1)
        {
            ref var aoe0 = ref aoes[0];
            ref var aoe1 = ref aoes[1];
            hasDifferentRotations = aoe0.Rotation != aoe1.Rotation;
        }

        var max = count > MaxCasts ? MaxCasts : count;

        for (var i = 0; i < max; ++i)
        {
            ref var aoe = ref aoes[i];
            aoe.Color = i == 0 && count > i ? Colors.Danger : default;
            aoe.Risky = i == 0 || hasDifferentRotations;
        }
        return aoes[..max];
    }
}

sealed class GhastlyGrasp(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GhastlyGrasp, 5f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 945u, CFCID = 945u, NameID = 12357u, PrimaryActorOID = (uint)OID.Moko, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V22MokoOtherPaths(WorldState ws, Actor primary) : V22Moko(ws, primary);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 945u, CFCID = 945u, NameID = 12357u, PrimaryActorOID = (uint)OID.MokoP2, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V22MokoPath2(WorldState ws, Actor primary) : V22Moko(ws, primary);

public abstract class V22Moko(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-700f, 540f), new ArenaBoundsSquare(24.5f));
