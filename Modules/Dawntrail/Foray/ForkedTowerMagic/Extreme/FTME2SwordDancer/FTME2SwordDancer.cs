// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Extreme.FTME2SwordDancer;

[SkipLocalsInit]
sealed class SwordStorm(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SwordStorm);
[SkipLocalsInit]
sealed class Rush(ModuleBase module) : Components.SimpleChargeAOEGroups(module, [(uint)AID.Rush1, (uint)AID.Rush2, (uint)AID.Rush3, (uint)AID.Rush4], 3.5f, 2, 2);
[SkipLocalsInit]
sealed class RushLong(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RushLong, new AOEShapeRect(30f, 3f), 8);
[SkipLocalsInit]
sealed class TurnInner(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Turn1], new AOEShapeDonutSector(9f, 14f, 45f.Degrees()));
[SkipLocalsInit]
sealed class TurnOuter(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Turn2, (uint)AID.Turnabout2], new AOEShapeDonutSector(19f, 24f, 45f.Degrees()));
sealed class TurnMiddle(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Turn3, (uint)AID.Turn4, (uint)AID.Turnabout1], new AOEShapeDonutSector(14f, 19f, 45f.Degrees()));
[SkipLocalsInit]
sealed class MartialMystique(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MartialMystique, new AOEShapeRect(48f, 48f));
[SkipLocalsInit]
sealed class Pierce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pierce, 5f);
[SkipLocalsInit]
sealed class SwordDance(ModuleBase module) : Components.GenericAOEs(module)
{
    // do sword markers rotate like in normal mode or can it be different?
    private readonly List<AOEInstance> _aoes = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var max = count > 2 ? 2 : count;
        var aoes = CollectionsMarshal.AsSpan(_aoes);

        ref var aoe = ref aoes[0];
        aoe.Color = Colors.Danger;

        return aoes[..max];
    }
    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.SwordDanceMarker && state == 0x00010002)
        {
            // 8.8s between 1st mark and 1st cast
            // 1s between eanims, 2.4s-ish between actual cast
            var count = _aoes.Count;
            var act = World.FutureTime(8.8d + 2.4d * count);
            _aoes.Add(new(new AOEShapeRect(30f, 10f, 30f), actor.Position, actor.Rotation, act));
        }
    }
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.SwordDance)
        {
            ++NumCasts;
            _aoes.RemoveAt(0);
        }
    }
}

[ModuleInfo(Group = ModuleGroup.TheForkedTowerMagic, CFCID = 1114u, NameID = 14820u, PrimaryActorOID = (uint)OID.SwordDancer, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "gynorhino (ported from BMR)")]
[SkipLocalsInit]
public sealed class FTME2SwordDancer(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600f, 704f), new ArenaBoundsCircle(24f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Module.Center, 24f);
}
