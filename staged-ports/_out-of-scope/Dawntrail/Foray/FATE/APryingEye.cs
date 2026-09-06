// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.APryingEye;

public enum OID : uint
{
    Boss = 0x47DC, // R2.4
    ObserversEye1 = 0x47DD, // R0.6
    ObserversEye2 = 0x4818, // R0.6
    Helper = 0x47DE
}

public enum AID : uint
{
    AutoAttack = 43367, // Boss->player, no cast, single-target

    Search = 43038, // ObserversEye1/ObserversEye2->self, no cast, single-target
    MarkOfDeath = 43039, // ObserversEye1/ObserversEye2->self, no cast, range 6 90-degree cone
    Visual = 42839, // ObserversEye1/ObserversEye2->self, no cast, single-target
    Stare1 = 43268, // Boss->self, 5.0s cast, range 60 width 8 rect
    Stare2 = 43044, // Boss->self, 5.0s cast, range 60 width 8 rect
    JumpScare = 43041, // Boss->player, no cast, single-target
    Oogle = 43043, // Boss->self, 4.0s cast, range 40 circle, gaze
    VoidThunderII = 43045 // Helper->location, 3.0s cast, range 6 circle
}

sealed class SearchMarkOfDeath(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCone cone2 = new(9f, 60f.Degrees()); // bigger than MarkOfDeath since its moving and we want a safety margin
    private static readonly uint[] allObservers = [(uint)OID.ObserversEye1, (uint)OID.ObserversEye2];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var observers = Module.Enemies(allObservers);
        var count = observers.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        var act = World.FutureTime(1.1d);
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var observer = observers[i];
            if (!observer.IsDead)
            {
                aoes[index++] = new(cone2, observer.Position, observer.Rotation, act);
            }
        }
        return aoes.AsSpan()[..index];
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        var aoes = ActiveAOEs(slot, actor);
        var len = aoes.Length;
        if (len == 0)
            return;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            hints.AddForbiddenZone(new SDCone(aoe.Origin, 6f, aoe.Rotation, 45f.Degrees()));
        }
    }
}

sealed class Stare(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Stare1, (uint)AID.Stare2], new AOEShapeRect(60f, 4f));
sealed class VoidThunderII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidThunderII, 6f);
sealed class Oogle(ModuleBase module) : Components.CastGaze(module, (uint)AID.Oogle);

sealed class APryingEyeStates : StateMachineBuilder
{
    public APryingEyeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SearchMarkOfDeath>()
            .ActivateOnEnter<Stare>()
            .ActivateOnEnter<VoidThunderII>()
            .ActivateOnEnter<Oogle>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, CFCID = 1018u, NameID = 1970u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class APryingEye(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);

