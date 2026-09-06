// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.DeathUntoDawn.P3;

public enum OID : uint
{
    Boss = 0x3201,
    Helper = 0x233C,
    MoonGana = 0x3219,
    SpiritGana = 0x321A,
    RavanasWill = 0x321B,
}

public enum AID : uint
{
    Explosion = 24046 // 3204->self, 5.0s cast, range 80 width 10 cross
}

public enum SID : uint
{
    Invincibility = 325
}

class AutoGraha(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = hints.PotentialTargets[i];
            if (h.Actor.FindStatus((uint)SID.Invincibility) != null)
                h.Priority = AIHints.Enemy.PriorityInvincible;
        }
    }
}
class DirectionalParry(ModuleBase module) : Components.DirectionalParry(module, [0x3201]);
class Explosion(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion, new AOEShapeCross(80f, 5f), maxCasts: 2);

class LunarRavanaStates : StateMachineBuilder
{
    public LunarRavanaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AutoGraha>()
            .ActivateOnEnter<DirectionalParry>()
            .ActivateOnEnter<Explosion>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69602u, NameID = 10037u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class LunarRavana(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-144f, 83f), new ArenaBoundsCircle(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
    protected override bool CheckPull() => Raid.Player()!.InCombat;
}
