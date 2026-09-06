// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D15WanderersPalace.D151KeeperOfHalidom;

public enum OID : uint
{
    Boss = 0x41C
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    Beatdown = 575, // Boss->player, no cast, single-target, mini tankbuster
    MoldySneeze = 1090, // Boss->self, no cast, range 6+R 90-degree frontal cone
    Inhale = 950, // Boss->self, 2.5s cast, range 20+R 90-degree cone
    GoobbuesGrief = 942, // Boss->self, 0.5s cast, range 6+R circle
    MoldyPhlegm = 941 // Boss->location, 2.5s cast, range 6 circle
}

class MoldySneeze(ModuleBase module) : Components.Cleave(module, (uint)AID.MoldySneeze, new AOEShapeCone(8.85f, 45.Degrees()));

class InhaleGoobbuesGrief(ModuleBase module) : Components.GenericAOEs(module)
{
    private bool _showInhale;
    private bool _showGrief;
    private DateTime _griefActivation;

    private static readonly AOEShapeCone _shapeInhale = new(22.85f, 45f.Degrees());
    private static readonly AOEShapeCircle _shapeGrief = new(8.85f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var aoes = new List<AOEInstance>(2);
        if (_showInhale)
            aoes.Add(new(_shapeInhale, Module.PrimaryActor.CastInfo!.LocXZ, Module.PrimaryActor.CastInfo.Rotation, Module.CastFinishAt(Module.PrimaryActor.CastInfo)));
        if (_showGrief)
            aoes.Add(new(_shapeGrief, Module.PrimaryActor.Position, default, _griefActivation));
        return CollectionsMarshal.AsSpan(aoes);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.Inhale:
                _showInhale = _showGrief = true;
                _griefActivation = Module.CastFinishAt(spell, 1.1f);
                break;
            case (uint)AID.GoobbuesGrief:
                _griefActivation = Module.CastFinishAt(spell);
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.Inhale:
                _showInhale = false;
                break;
            case (uint)AID.GoobbuesGrief:
                _showGrief = false;
                break;
        }
    }
}

class MoldyPhlegm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MoldyPhlegm, 6f);

class D151KeeperOfHalidomStates : StateMachineBuilder
{
    public D151KeeperOfHalidomStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MoldySneeze>()
            .ActivateOnEnter<InhaleGoobbuesGrief>()
            .ActivateOnEnter<MoldyPhlegm>();
    }
}

[ModuleInfo(CFCID = 10u, NameID = 1548u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class D151KeeperOfHalidom(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(125f, 108f), new ArenaBoundsSquare(20f));
