// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankA.Maliktender;

public enum OID : uint
{
    Boss = 0x2874, // R=3.06
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Sabotendance = 18019, // Boss->self, 3.5s cast, range 8 circle, stuns players
    TwentyKNeedles = 18022, // Boss->self, 3.5s cast, range 20 width 8 rect
    NineNineNineKNeedles = 18024, // Boss->self, 3.0s cast, range 20 width 8 rect
    Haste = 18020, // Boss->self, 3.0s cast, buff to self, boss will use 990k needles instead of 20k needles
}

public enum SID : uint
{
    Haste = 1962,
    Stun = 149,
}

class Sabotendance(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Sabotendance, 8f);
class TwentyKNeedles(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TwentyKNeedles, new AOEShapeRect(20f, 4f));

class Haste(ModuleBase module) : ModuleComponent(module)
{
    private bool HasteB;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Haste)
            HasteB = true;
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (HasteB)
            hints.Add("Getting hit by the needle attack will instantly kill you from now on!");
    }
}

class NineNineNineKNeedles(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NineNineNineKNeedles, new AOEShapeRect(20f, 4f));

class MaliktenderStates : StateMachineBuilder
{
    public MaliktenderStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Sabotendance>()
            .ActivateOnEnter<TwentyKNeedles>()
            .ActivateOnEnter<NineNineNineKNeedles>()
            .ActivateOnEnter<Haste>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8901u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Maliktender(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
