// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage14.Act1;

public enum OID : uint
{
    Boss = 0x271D //R=2.0
}

public enum AID : uint
{
    TheLastSong = 14756 // Boss->self, 6.0s cast, range 60 circle
}

sealed class LastSong(ModuleBase module) : Components.CastLineOfSightAOEComplex(module, (uint)AID.TheLastSong, Layouts.Layout2CornersBlockers);

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
            hints.Add("Take cover behind a barricade!");
        }
    }
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("These slimes start casting Final Song after death.\nWhile Final Song is not deadly, it does heavy damage and applies silence\nto you. Take cover! For act 2 the spell Loom is strongly recommended.\nThe slimes are strong against blunt melee damage such as J Kick.");
    }
}

sealed class Stage14Act1States : StateMachineBuilder
{
    public Stage14Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .ActivateOnEnter<LastSong>()
            .ActivateOnEnter<LastSongHint>()
            .Raw.Update = () => AllDeadOrDestroyed((uint)OID.Boss) && !module.FindComponent<LastSongHint>()!.Casting;
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 624u, CFCID = 624u, NameID = 8108u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage14Act1 : ModuleBase
{
    public Stage14Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.Layout2Corners)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }

    protected override bool CheckPull() => IsAnyActorInCombat((uint)OID.Boss);
}
