// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankA.Supay;

public enum OID : uint
{
    Boss = 0x2839 // R=3.6
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    BlasphemousHowl = 17858, // Boss->players, 3.0s cast, range 8 circle, spread, applies terror
    PetroEyes = 17856, // Boss->self, 3.0s cast, range 40 circle, gaze, inflicts petrification
    Beakaxe = 17857 // Boss->player, no cast, single-target, instantlyy kills petrified players
}

class BlasphemousHowl(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.BlasphemousHowl, 8f)
{
    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (IsBaitTarget(actor))
        {
            hints.Add("Bait away + look away!");
        }
    }
}

class PetroEyes(ModuleBase module) : Components.CastGaze(module, (uint)AID.PetroEyes);

class SupayStates : StateMachineBuilder
{
    public SupayStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PetroEyes>()
            .ActivateOnEnter<BlasphemousHowl>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8891u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Supay(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
