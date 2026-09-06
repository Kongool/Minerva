// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage14.Act2;

public enum OID : uint
{
    Boss = 0x271E //R=2.0
}

public enum AID : uint
{
    Syrup = 14757, // Boss->player, no cast, range 4 circle, applies heavy to player
    TheLastSong = 14756 // Boss->self, 6.0s cast, range 60 circle, heavy dmg, applies silence to player
}

sealed class LastSong(ModuleBase module) : Components.CastLineOfSightAOEComplex(module, (uint)AID.TheLastSong, Layouts.LayoutBigQuadBlockers);
sealed class LastSongHint(ModuleBase module) : ModuleComponent(module)
{
    public bool Casting;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TheLastSong)
        {
            Casting = true;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TheLastSong)
        {
            Casting = false;
        }
    }
    public override void AddGlobalHints(GlobalHints hints)
    {
        if (Casting)
        {
            hints.Add("Use the cube to take cover!");
        }
    }
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Same as first act, but the slimes will apply heavy to you.\nUse Loom to get out of line of sight as soon as Final Song gets casted.");
    }
}

sealed class Stage14Act2States : StateMachineBuilder
{
    public Stage14Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .ActivateOnEnter<LastSong>()
            .ActivateOnEnter<LastSongHint>()
            .Raw.Update = () => AllDeadOrDestroyed((uint)OID.Boss) && !module.FindComponent<LastSongHint>()!.Casting;
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 624u, CFCID = 624u, NameID = 8108u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage14Act2 : ModuleBase
{
    public Stage14Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.LayoutBigQuad)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }

    protected override bool CheckPull() => IsAnyActorInCombat((uint)OID.Boss);
}
