// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.DeathUntoDawn.P1;

public enum AID : uint
{
    AntiPersonnelMissile = 24845, // 233C->player/321D, 5.0s cast, range 6 circle
    MRVMissile = 24843, // 233C->location, 8.0s cast, range 12 circle
}

enum OID : uint
{
    Boss = 0x3376
}

class AntiPersonnelMissile(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.AntiPersonnelMissile, 6f);
class MRVMissile(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MRVMissile, 12f, 6);

public class TelotekGammaStates : StateMachineBuilder
{
    public TelotekGammaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()

            .ActivateOnEnter<AntiPersonnelMissile>()
            .ActivateOnEnter<MRVMissile>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69602u, NameID = 10189u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class TelotekGamma(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(default, -180f), new ArenaBoundsCircle(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
