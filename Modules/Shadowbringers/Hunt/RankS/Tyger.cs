// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankS.Tyger;

public enum OID : uint
{
    Boss = 0x288E // R=5.92
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    TheLionsBreath = 16957, // Boss->self, 4.0s cast, range 30 120-degree cone
    TheScorpionsSting = 16961, // Boss->self, no cast, range 18 90-degree cone, 2-4s after a voice attack, timing seems to vary, maybe depends if voice was interrupted and how fast?
    TheDragonsBreath = 16959, // Boss->self, 4.0s cast, range 30 120-degree cone
    TheRamsBreath = 16958, // Boss->self, 4.0s cast, range 30 120-degree cone
    TheRamsEmbrace = 16960, // Boss->location, 3.0s cast, range 9 circle
    TheDragonsVoice = 16963, // Boss->self, 4.0s cast, range 8-30 donut, interruptible raidwide donut
    TheRamsVoice = 16962 // Boss->self, 4.0s cast, range 9 circle
}

sealed class Breaths(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TheDragonsBreath, (uint)AID.TheRamsBreath,
(uint)AID.TheLionsBreath], new AOEShapeCone(30f, 60f.Degrees()));

sealed class TheScorpionsSting(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private readonly AOEShapeCone cone = new(18f, 45f.Degrees());
    private WPos lastPos;
    private Angle lastRot;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void Update()
    {
        // unfortunately the boss can move after the cast if tank changes or moves, so we need to update
        if (_aoe.Length != 0 && Module.PrimaryActor is var prim && prim.Position is var pos && prim.Rotation is var rot && (lastRot != rot || lastPos != pos))
        {
            ref var aoe = ref _aoe[0];
            var origin = pos.Quantized();
            aoe.Origin = origin;
            aoe.Rotation = rot + 180f.Degrees();
            aoe.ShapeDistance = aoe.Shape.Distance(origin, rot);
            lastPos = pos;
            lastRot = rot;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.TheRamsVoice or (uint)AID.TheDragonsVoice) // timing varies, just used the lowest I could find, probably depends on interrupt status
        {
            var pos = spell.LocXZ;
            var rot = spell.Rotation + 180f.Degrees();
            _aoe = [new(cone, pos, rot, Module.CastFinishAt(spell, 2.3d), shapeDistance: cone.Distance(pos, rot))];
            lastPos = caster.Position;
            lastRot = spell.Rotation;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.TheScorpionsSting)
        {
            _aoe = [];
        }
    }
}

sealed class TheRamsEmbrace(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheRamsEmbrace, 9f);
sealed class TheRamsVoice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheRamsVoice, 9f);
sealed class TheRamsVoiceHint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.TheRamsVoice);
sealed class TheDragonsVoice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheDragonsVoice, new AOEShapeDonut(8f, 30f));
sealed class TheDragonsVoiceHint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.TheDragonsVoice, hintExtra: "Donut Raidwide");

sealed class TygerStates : StateMachineBuilder
{
    public TygerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Breaths>()
            .ActivateOnEnter<TheScorpionsSting>()
            .ActivateOnEnter<TheDragonsVoice>()
            .ActivateOnEnter<TheDragonsVoiceHint>()
            .ActivateOnEnter<TheRamsEmbrace>()
            .ActivateOnEnter<TheRamsVoice>()
            .ActivateOnEnter<TheRamsVoiceHint>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8905u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Tyger(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
