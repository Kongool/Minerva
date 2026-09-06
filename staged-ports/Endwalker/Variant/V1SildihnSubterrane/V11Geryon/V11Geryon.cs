// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V11Geryon;

sealed class ColossalStrike(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ColossalStrike);
sealed class ColossalCharge(ModuleBase module) : Components.SimpleChargeAOEGroups(module, [(uint)AID.ColossalCharge1, (uint)AID.ColossalCharge2], 7f);

sealed class ColossalSlam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ColossalSlam, new AOEShapeCone(60f, 30f.Degrees()))
{
    private readonly Explosion _aoe = module.FindComponent<Explosion>()!;
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe.AOEs.Count == 5 ? [] : base.ActiveAOEs(slot, actor);
}

sealed class ColossalSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ColossalSwing, new AOEShapeCone(60f, 90f.Degrees()));

sealed class SubterraneanShudderColossalLaunch(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.SubterraneanShudder, (uint)AID.ColossalLaunch]);

sealed class RunawaySludge(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 9f, (uint)AID.RunawaySludge, GetVoidzones, 0.2d)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.SludgeVoidzone);
        var count = enemies.Count;
        if (count == 0)
        {
            return [];
        }
        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
            {
                voidzones[index++] = z;
            }
        }
        return voidzones[..index];
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 868u, CFCID = 868u, NameID = 11442u, PrimaryActorOID = (uint)OID.Geryon, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class V11Geryon(WorldState ws, Actor primary) : ModuleBase(ws, primary, primary.PosRot.X is var X && X < -150f ? new(-213f, 101f) : X > 100f ? new(183f, 177f) : default,
X < -150f ? new ArenaBoundsSquare(24.5f) : new ArenaBoundsSquare(19.5f));
