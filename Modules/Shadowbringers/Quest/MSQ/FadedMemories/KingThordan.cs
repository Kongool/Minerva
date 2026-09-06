// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.MSQ.FadedMemories.KingThordan;

public enum OID : uint
{
    Boss = 0x2F1D, // 3.8
    SerGrinnaux = 0x2F1A, // R2.2
    SerZephirin = 0x2F1C, // R2.2
    SerCharibert = 0x2F1B // R2.2
}

public enum AID : uint
{
    AutoAttack1 = 6497, // SerGrinnaux/SerZephirin->player, no cast, single-target
    AutoAttack2 = 21399, // Boss->player, no cast, single-target

    Fire = 20613, // SerCharibert->player, 1.0s cast, single-target
    SacredCross = 21089, // SerZephirin->self, 5.0s cast, range 80 circle
    AltarCandle = 21088, // SerCharibert->player, no cast, single-target
    AscalonsMight = 21091, // Boss->self, no cast, range 8 90-degree cone
    TheDragonsGaze = 21090, // Boss->self, 4.0s cast, range 80 circle
    HyperdimensionalSlash = 21086 // SerGrinnaux->self, 3.5s cast, range 45 width 8 rect
}

public enum SID : uint
{
    Invincibility = 671
}

class DragonsGaze(ModuleBase module) : Components.CastGaze(module, (uint)AID.TheDragonsGaze);
class HyperdimensionalSlash(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HyperdimensionalSlash, new AOEShapeRect(45f, 4f));

class KingThordanStates : StateMachineBuilder
{
    public KingThordanStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DragonsGaze>()
            .ActivateOnEnter<HyperdimensionalSlash>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69311u, NameID = 3632u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class KingThordan(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-247f, 321f), new ArenaBoundsCircle(20f))
{
    public static readonly uint[] all = [(uint)OID.Boss, (uint)OID.SerGrinnaux, (uint)OID.SerZephirin, (uint)OID.SerCharibert];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, all);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = hints.PotentialTargets[i];
            h.Priority = h.Actor.FindStatus((uint)SID.Invincibility) == null ? 1 : AIHints.Enemy.PriorityInvincible;
        }
    }
}
