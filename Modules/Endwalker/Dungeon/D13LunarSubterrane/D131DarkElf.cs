// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D13LunarSubterrane.D131DarkElf;

public enum OID : uint
{
    Boss = 0x3FE2, // R=5.0
    HexingStaff = 0x3FE3, // R=1.2
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    HexingStaves = 34777, // Boss->self, 3.0s cast, single-target
    RuinousHexVisual = 34783, // HexingStaff->self, 5.0s cast, single-target
    RuinousHex1 = 35254, // HexingStaff->self, 5.0s cast, range 40 width 8 cross
    RuinousHex2 = 34789, // Helper->self, 5.5s cast, range 40 width 8 cross
    RuinousConfluence = 35205, // Boss->self, 5.0s cast, single-target
    ShadowySigil1 = 34779, // Boss->self, 6.0s cast, single-target
    ShadowySigil2 = 34780, // Boss->self, 6.0s cast, single-target
    Explosion = 34787, // Helper->self, 6.5s cast, range 8 width 8 rect
    SorcerousShroud = 34778, // Boss->self, 5.0s cast, single-target
    VoidDarkII = 34781, // Boss->self, 2.5s cast, single-target
    VoidDarkII2 = 34788, // Helper->player, 5.0s cast, range 6 circle
    StaffSmite = 35204, // Boss->player, 5.0s cast, single-target
    AbyssalOutburst = 34782 // Boss->self, 5.0s cast, range 60 circle
}

public enum SID : uint
{
    Doom = 3364 // none->player, extra=0x0
}

class HexingStaves(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly Explosion _aoe = module.FindComponent<Explosion>()!;
    private static readonly AOEShapeCross cross = new(40f, 4f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0 || _aoe.Casters.Count != 0)
            return [];
        var aoes = new AOEInstance[count];
        var risky = _aoes[0].Activation.AddSeconds(-5d) < World.CurrentTime;
        for (var i = 0; i < count; ++i)
        {
            var aoe = _aoes[i];
            aoes[i] = aoe with { Risky = risky };
        }
        return aoes;
    }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor.OID == (uint)OID.HexingStaff && animState2 == 1)
        {
            var delay = NumCasts switch
            {
                0 => 8.1d,
                1 => 25.9d,
                _ => 32d
            };
            _aoes.Add(new(cross, actor.Position.Quantized(), actor.Rotation, World.FutureTime(delay)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.RuinousConfluence:
                ++NumCasts;
                break;
            case (uint)AID.RuinousHex1:
            case (uint)AID.RuinousHex2:
                _aoes.Clear();
                break;
        }
    }
}

class StaffSmite(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.StaffSmite);
class VoidDarkII(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.VoidDarkII2, 6f);
class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, new AOEShapeRect(8f, 4f));
class AbyssalOutburst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AbyssalOutburst);

class Doom(ModuleBase module) : Components.CleansableDebuff(module, (uint)SID.Doom);

class D131DarkElfStates : StateMachineBuilder
{
    public D131DarkElfStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<StaffSmite>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<HexingStaves>()
            .ActivateOnEnter<AbyssalOutburst>()
            .ActivateOnEnter<VoidDarkII>()
            .ActivateOnEnter<Doom>();
    }
}

[ModuleInfo(CFCID = 823u, NameID = 12500u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D131DarkElf(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-401f, -231f), new ArenaBoundsSquare(15.5f));
