// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankS.Gunitt;

public enum OID : uint
{
    Boss = 0x2852 // R=4.0
}

public enum AID : uint
{
    AutoAttack = 870, // 2852->player, no cast, single-target

    TheDeepSeeks = 17356, // 2852->player, 4.0s cast, single-target
    TheDeepReaches = 17357, // 2852->self, 4.0s cast, range 40 width 2 rect
    TheDeepBeckons = 17358, // 2852->self, 4.0s cast, range 40 circle
    Abordage = 17359, // 2852->players, no cast, width 8 rect charge, seems to target random player before stack marker, no telegraph?
    SwivelGun = 17361, // 2852->players, 5.0s cast, range 10 circle, stack marker, applies magic vuln up, 3 times in a row
    CoinToss = 17360, // 2852->self, 4.0s cast, range 40 circle, gaze, applies Seduced (forced march to boss)
    TheDeepRends = 17351, // 2852->self, 5.5s cast, range 20 60-degree cone
    TheDeepRends2 = 17352 // 2852->self, no cast, range 20 60-degree cone, seems to target 5 random players after first The Deep Rends, no telegraph?
}

public enum SID : uint
{
    MagicVulnerabilityUp = 1138 // Boss->player, extra=0x0
}

public enum IconID : uint
{
    Stackmarker = 93 // player
}

class TheDeepSeeks(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.TheDeepSeeks);
class TheDeepReaches(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheDeepReaches, new AOEShapeRect(40f, 1f));
class TheDeepBeckons(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TheDeepBeckons);
class CoinToss(ModuleBase module) : Components.CastGaze(module, (uint)AID.CoinToss);
class TheDeepRends(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheDeepRends, new AOEShapeCone(20f, 30f.Degrees()));
class TheDeepRendsHint(ModuleBase module) : Components.CastHint(module, (uint)AID.TheDeepRends, "Targets 5 random players after initial hit");

class SwivelGun(ModuleBase module) : Components.GenericStackSpread(module)
{
    private BitMask _forbidden;

    public override void Update()
    {
        if (Stacks.Count > 0) //updating forbiddenplayers because debuffs can be applied after new stack marker appears
        {
            var Forbidden = Stacks[0];
            Forbidden.ForbiddenPlayers = _forbidden;
            Stacks[0] = Forbidden;
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Stackmarker)
            Stacks.Add(new(actor, 10f, 8, activation: World.FutureTime(5d), forbiddenPlayers: _forbidden));
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.MagicVulnerabilityUp)
            _forbidden[Raid.FindSlot(actor.InstanceID)] = true;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.MagicVulnerabilityUp)
            _forbidden.Clear(Raid.FindSlot(actor.InstanceID));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SwivelGun)
            Stacks.RemoveAt(0);
    }
}

class GunittStates : StateMachineBuilder
{
    public GunittStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TheDeepBeckons>()
            .ActivateOnEnter<TheDeepReaches>()
            .ActivateOnEnter<TheDeepRends>()
            .ActivateOnEnter<TheDeepRendsHint>()
            .ActivateOnEnter<CoinToss>()
            .ActivateOnEnter<SwivelGun>()
            .ActivateOnEnter<TheDeepSeeks>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8895u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Gunitt(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
