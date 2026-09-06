// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage24.Act3;

public enum OID : uint
{
    Boss = 0x2739, //R=3.0
    ArenaMagus = 0x273A, //R=1.0
    VacuumWave = 0x273B //R=1.0
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target

    PageTear = 15324, // Boss->self, 3.5s cast, range 6+R 90-degree cone
    MagicHammer = 15327, // Boss->location, 3.0s cast, range 8 circle
    GaleCut = 15323, // Boss->self, 3.0s cast, single-target
    HeadDown = 15325, // Boss->player, 5.0s cast, width 8 rect charge, knockback 10, source forward
    VacuumBlade = 15328, // VacuumWave->self, 3.0s cast, range 3 circle
    BoneShaker = 15326, // Boss->self, 3.0s cast, range 50+R circle, raidwide + adds
    Fire = 14266, // ArenaMagus->player, 1.0s cast, single-target
    SelfDetonate = 15329 // ArenaMagus->player, 3.0s cast, single-target
}

sealed class MagicHammer(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagicHammer, 8f);
sealed class PageTear(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PageTear, new AOEShapeCone(8f, 45f.Degrees()));
sealed class VacuumBlade(ModuleBase module) : Components.Voidzone(module, 3f, GetVoidzones)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.VacuumWave);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (!z.IsDead)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

sealed class HeadDown(ModuleBase module) : Components.BaitAwayChargeCast(module, (uint)AID.HeadDown, 4f);

sealed class HeadDownKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.HeadDown, 10f, kind: Kind.DirForward)
{
    private readonly VacuumBlade _aoe = module.FindComponent<VacuumBlade>()!;

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            if (aoes[i].Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }
}

sealed class BoneShaker(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BoneShaker, "Adds + Raidwide");

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        var magi = Module.Enemies((uint)OID.ArenaMagus);
        var count = magi.Count;
        if (count == 0)
            return;
        for (var i = 0; i < count; ++i)
        {
            var magus = magi[i];
            if (!magus.IsDead)
            {
                hints.Add($"Kill {magus.Name} fast or wipe!\nUse ranged physical attacks.");
                return;
            }
        }
    }
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"{Module.PrimaryActor.Name} spawns two adds when casting Boneshaker. These should be a\npriority or they will explode and wipe you. To kill them without touching\nthe electric field use a ranged physical attack such as Fire Angon.\nYou can start the Final Sting combination at about 50% health left.\n(Off-guard->Bristle->Moonflute->Final Sting)");
    }
}

sealed class Stage24Act3States : StateMachineBuilder
{
    public Stage24Act3States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .ActivateOnEnter<PageTear>()
            .ActivateOnEnter<MagicHammer>()
            .ActivateOnEnter<VacuumBlade>()
            .ActivateOnEnter<HeadDown>()
            .ActivateOnEnter<HeadDownKB>()
            .ActivateOnEnter<BoneShaker>()
            .ActivateOnEnter<Hints2>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 634u, CFCID = 634u, NameID = 8125u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage24Act3 : ModuleBase
{
    public Stage24Act3(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleSmall)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.ArenaMagus));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.ArenaMagus => 1, // TODO: ideally Magus should only be attacked with ranged physical abilities
                _ => 0
            };
        }
    }
}
