// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh.DD80Kajigakaka;

public enum OID : uint
{
    Boss = 0x23EC, // R4.5
    IcePillar = 0x23EE, // R2.0
    IceBoulder = 0x23ED // R1.5
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target

    HeavenswardHowl = 11985, // Boss->self, 2.5s cast, range 8+R 120-degree cone
    EclipticBite = 11986, // Boss->player, no cast, single-target
    HowlingMoon = 11988, // Boss->self, no cast, single-target
    PillarImpact = 11990, // IcePillar->self, 2.5s cast, range 4+R circle
    PillarPierce = 11989, // IcePillar->self, 2.5s cast, range 80+R width 4 rect
    SphereShatter = 11992, // IceBoulder->self, no cast, range 8 circle
    LunarCry = 11987 // Boss->self, 3.0s cast, range 80+R circle
}

class HeavenswardHowl(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeavenswardHowl, new AOEShapeCone(12.5f, 60f.Degrees()));
class PillarImpact(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PillarImpact, 6.5f);
class PillarPierce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PillarPierce, new AOEShapeRect(82f, 2f));
class LunarCry(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.LunarCry);

class SphereShatter(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(8f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.IceBoulder)
            _aoes.Add(new(circle, actor.Position.Quantized(), default, World.FutureTime(8.5d), actorID: actor.InstanceID));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SphereShatter)
        {
            var count = _aoes.Count;
            var id = caster.InstanceID;
            for (var i = 0; i < count; ++i)
            {
                if (_aoes[i].ActorID == id)
                {
                    _aoes.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

class DD80KajigakakaStates : StateMachineBuilder
{
    public DD80KajigakakaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HeavenswardHowl>()
            .ActivateOnEnter<PillarImpact>()
            .ActivateOnEnter<PillarPierce>()
            .ActivateOnEnter<SphereShatter>()
            .ActivateOnEnter<LunarCry>();
    }
}

[ModuleInfo(CFCID = 547u, NameID = 7490u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class DD80Kajigakaka(WorldState ws, Actor primary) : HoHArena2(ws, primary);
