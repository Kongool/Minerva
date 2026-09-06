// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Criterion.C01AMT.C011DaryaTheSeaMaid;

// TODO (in-order of priority):
//  - Improve visual for CrossCurrent
//  - Add priority order to Tidalspout mechanic - DPS flex, but add configuration to module so it can be picked by the player
//  - Rewrite AquaSpear code (mechanic works fine) - Remove grid map or improve it - can use WaterTile OID 

class PiercingPlunge(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.PiercingPlunge);

class SurgingCurrent(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SurgingCurrent1, new AOEShapeCone(60f, 45.Degrees()))
{
    private readonly List<AOEInstance> aoes = [];
    public int maxShow = 4;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        aoes.Clear();

        SortHelpers.SortAOEByActivation(Casters);
        var show = 0;
        foreach (var caster in Casters)
        {
            if (show >= maxShow)
            {
                break;
            }

            var colour = (show < 2) ? Colors.Danger : Colors.AOE;
            aoes.Add(new(caster.Shape, caster.Origin, caster.Rotation, caster.Activation, colour, show < 2));
            show++;
        }

        return CollectionsMarshal.AsSpan(aoes);
    }
}

class SurgingCurrent2 : SurgingCurrent
{
    public SurgingCurrent2(ModuleBase module) : base(module)
    {
        maxShow = 2;
    }
}

class CrossCurrent(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.CrossCurrent)
{
    private readonly List<AOEInstance> aoes = [];
    BitMask _targets;
    DateTime _activation;

    public static WPos TileCenter(Actor a)
    {
        WPos arenaCenter = new(375f, 530f);
        var dir = (a.Position - arenaCenter) / 8;
        return arenaCenter + dir.Rounded() * 8;
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        aoes.Clear();
        foreach (var (_, p) in Raid.WithSlot(false, true, true).IncludedInMask(_targets).Exclude(actor))
        {
            aoes.Add(new(new AOEShapeCross(36, 4), TileCenter(p), default, _activation));
        }

        return CollectionsMarshal.AsSpan(aoes);
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.CrossCurrent)
        {
            _targets.Set(Raid.FindSlot(targetID));
            _activation = World.FutureTime(6.1d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.CrossCurrent)
        {
            ++NumCasts;
            _targets.Reset();
        }
    }
}

class AquaBall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AquaBall1, new AOEShapeCircle(5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1079u, CFCID = 1079u, NameID = 14291u, PrimaryActorOID = (uint)OID.DaryaTheSeaMaid, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
public sealed class DaryaTheSeaMaid(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(375f, 530f), new ArenaBoundsSquare(20));
