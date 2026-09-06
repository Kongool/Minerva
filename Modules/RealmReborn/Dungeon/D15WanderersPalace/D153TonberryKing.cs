// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D15WanderersPalace.D153TonberryKing;

public enum OID : uint
{
    Boss = 0x374, // x1
    Tonberry = 0x3A3, // spawn during fight
    TonberrySlasher = 0x8D5 // spawn during fight
}

public enum AID : uint
{
    AutoAttack = 871, // Boss/Tonberry/TonberrySlasher->player, no cast, single-target

    LateralSlash = 419, // Boss->player, no cast, single-target, tankbuster
    Whetstone = 420, // Boss->self, no cast, single-target, buffs next tankbuster
    SharpenedKnife = 945, // Boss->player, no cast, single-target, buffed tankbuster
    ScourgeOfNym = 1392, // Boss/Tonberry->location, no cast, range 5 circle - cast when no one is in melee range
    ThroatStab = 948, // Tonberry/TonberrySlasher->player, no cast, single-target
    EveryonesGrudge = 947, // Boss->player, 3.0s cast, single-target, buffed by rancor stacks
    RancorRelease = 949 // Tonberry->Boss, 1.0s cast, single-target, gives boss rancor stack on death
}

class D153TonberryKingStates : StateMachineBuilder
{
    public D153TonberryKingStates(ModuleBase module) : base(module)
    {
        TrivialPhase();
    }
}

[ModuleInfo(CFCID = 10u, NameID = 1547u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class D153TonberryKing(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(73, -435), new ArenaBoundsCircle(30))
{
    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        //var rancorStacks = PrimaryActor.FindStatus(SID.Rancor)?.Extra ?? 0;
        foreach (var e in hints.PotentialTargets)
        {
            e.Priority = (OID)e.Actor.OID switch
            {
                //OID.Tonberry => a.HPMP.CurHP > 1 && rancorStacks < 3 ? 0 : AIHints.Enemy.PriorityForbidAI, // note: we don't bother with them - let tank aoe them when they reach center and invuln high stacks...
                OID.Boss => 1,
                _ => 0
            };
        }
    }
}
