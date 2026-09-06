// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankA.Grassman;

public enum OID : uint
{
    Boss = 0x283A // R=4.0
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    ChestThump = 17859, // Boss->self, 4.0s cast, range 30 circle, one cast on 1st time, 5 hits on subsequent times, dmg buff on boss for each cast
    ChestThump2 = 17863, // Boss->self, no cast, range 30 circle
    StoolPelt = 17861, // Boss->location, 3.0s cast, range 5 circle
    Browbeat = 17860, // Boss->player, 4.0s cast, single-target
    Streak = 17862 // Boss->location, 3.0s cast, width 6 rect charge, knockback 10, away from source
}

class ChestThump(ModuleBase module) : ModuleComponent(module)
{
    private int NumCasts2;
    private bool casting;
    private DateTime _activation;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ChestThump)
        {
            casting = true;
            _activation = Module.CastFinishAt(spell);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ChestThump)
        {
            ++NumCasts;
            if (NumCasts == 1)
                casting = false;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ChestThump2)
        {
            ++NumCasts2;
            if (NumCasts2 == 4)
            {
                casting = false;
                NumCasts2 = 0;
            }
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (casting)
        {
            if (NumCasts == 0)
                hints.Add($"Raidwide");
            else if (NumCasts > 0)
                hints.Add($"Raidwide x5");
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        hints.AddPredictedDamage(Raid.WithSlot(false, false, true).Mask(), _activation);
    }
}

class StoolPelt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.StoolPelt, 5f);
class Browbeat(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Browbeat);
class Streak(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Streak, 3f);

class GrassmanStates : StateMachineBuilder
{
    public GrassmanStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Streak>()
            .ActivateOnEnter<Browbeat>()
            .ActivateOnEnter<StoolPelt>()
            .ActivateOnEnter<ChestThump>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8892u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Grassman(WorldState ws, Actor primary) : SimpleBossModule(ws, primary) { }
