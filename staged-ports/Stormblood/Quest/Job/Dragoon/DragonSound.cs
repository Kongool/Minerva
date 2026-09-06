// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.Job.Dragoon.DragonSound;

public enum OID : uint
{
    Boss = 0x1CDD, // R6.840, x1
    Faunehm = 0x18D6, // R0.500, x9
}

public enum AID : uint
{
    AbyssicBuster = 8929, // Boss->self, 2.0s cast, range 25+R 90-degree cone
    Heavensfall1 = 8935, // 18D6->location, 2.0s cast, range 5 circle
    DarkStar = 8931, // Boss->self, 2.0s cast, range 50+R circle
}

public enum SID : uint
{
    Enervation = 1401, // Boss->1CDE/player, extra=0x0
}

class AbyssicBuster(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbyssicBuster, new AOEShapeCone(31.84f, 45.Degrees()));
class Heavensfall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Heavensfall1, 5);
class DarkStar(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DarkStar);

// scripted interaction, no idea if it's required to complete the duty but might as well do it
class Enervation(ModuleBase module) : ModuleComponent(module)
{
    private bool Active;
    private Actor? OrnKhai;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (actor.OID == 0 && status.ID == (uint)SID.Enervation)
            Active = true;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (actor.OID == 0 && status.ID == (uint)SID.Enervation)
            Active = false;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!Active)
            return;

        OrnKhai ??= World.Actors.FirstOrDefault(x => x.OID == 0x1CDF);
        if (OrnKhai == null)
            return;

        hints.ActionsToExecute.Push(ActionID.MakeSpell(DRG.AID.ElusiveJump), actor, ActionQueue.Priority.Medium, facingAngle: -actor.AngleTo(OrnKhai));

        var okp = OrnKhai.Position;
        hints.GoalZones.Add(p => p.InCircle(okp, 3) ? 100 : 0);
    }
}

class FaunehmStates : StateMachineBuilder
{
    public FaunehmStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AbyssicBuster>()
            .ActivateOnEnter<Heavensfall>()
            .ActivateOnEnter<DarkStar>()
            .ActivateOnEnter<Enervation>();
    }
}

[ModuleInfo(CFCID = 68450u, NameID = 6347u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Faunehm(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(4, 248.5f), new ArenaBoundsCircle(25));

