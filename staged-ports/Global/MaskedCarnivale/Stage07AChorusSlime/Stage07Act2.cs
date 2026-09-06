// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage07.Act2;

public enum OID : uint
{
    Boss = 0x2705, //R=1.6
    Sprite = 0x2704 //R=0.8
}

public enum AID : uint
{
    Detonation = 14696, // Boss->self, no cast, range 6+R circle
    Blizzard = 14709 // Sprite->player, 1.0s cast, single-target
}

sealed class SlimeExplosion(ModuleBase module) : Components.GenericStackSpread(module)
{
    private readonly List<Actor> slimes = [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.Boss)
        {
            slimes.Add(actor);
        }
    }

    public override void OnActorDeath(Actor actor)
    {
        if (actor.OID == (uint)OID.Boss)
        {
            slimes.Remove(actor);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var count = slimes.Count;
        for (var i = 0; i < count; ++i)
        {
            Arena.ZoneCircleOutline(slimes[i].Position, 7.6f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var count = slimes.Count;
        for (var i = 0; i < count; ++i)
        {
            if (actor.Position.InCircle(slimes[i].Position, 7.6f))
            {
                hints.Add("In slime explosion radius!");
                return;
            }
        }
    }
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Pull or push the Lava Slimes to the Ice Sprites and then hit the slimes\nfrom a distance to set of the explosions.");
    }
}

sealed class Stage07Act2States : StateMachineBuilder
{
    public Stage07Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .Raw.Update = () => AllDeadOrDestroyed(Stage07Act2.Trash);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, CFCID = 617u, NameID = 8094u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage07Act2 : ModuleBase
{
    public Stage07Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.Layout4Quads)
    {
        ActivateComponent<Hints>();
        ActivateComponent<SlimeExplosion>();
    }
    public static readonly uint[] Trash = [(uint)OID.Boss, (uint)OID.Sprite];

    protected override bool CheckPull() => IsAnyActorInCombat(Trash);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
        Arena.Actors(Enemies((uint)OID.Sprite));
    }
}
