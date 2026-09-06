// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;
using static Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL1Brionac4thLegionHelldiver.CLL1Brionac4thLegionHelldiver;


namespace Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL1Brionac4thLegionHelldiver;

sealed class ElectricAnvil(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.ElectricAnvil)
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_arena.IsBrionacArena)
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_arena.IsBrionacArena)
            base.AddGlobalHints(hints);
    }
}

sealed class MagitekMissiles(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.MagitekMissiles)
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!_arena.IsBrionacArena)
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (!_arena.IsBrionacArena)
        {
            base.AddGlobalHints(hints);
        }
    }
}

sealed class MRVMissile(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MRVMissile)
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!_arena.IsBrionacArena)
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (!_arena.IsBrionacArena)
            base.AddGlobalHints(hints);
    }
}

sealed class LightningShower(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.LightningShower)
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_arena.IsBrionacArena)
            base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_arena.IsBrionacArena)
            base.AddGlobalHints(hints);
    }
}

sealed class FalseThunder(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.FalseThunder1, (uint)AID.FalseThunder2], new AOEShapeCone(47f, 65f.Degrees()))
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_arena.IsBrionacArena)
            return base.ActiveAOEs(slot, actor);
        else
            return [];
    }
}

sealed class Voltstream(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Voltstream, new AOEShapeRect(40f, 5f), 3)
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_arena.IsBrionacArena)
            return base.ActiveAOEs(slot, actor);
        else
            return [];
    }
}

sealed class SurfaceMissile(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SurfaceMissile, 6f)
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (!_arena.IsBrionacArena)
            return base.ActiveAOEs(slot, actor);
        else
            return [];
    }
}

sealed class CommandSuppressiveFormation(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.CommandSuppressiveFormation, 3f)
{
    private readonly DetermineArena _arena = module.FindComponent<DetermineArena>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (!_arena.IsBrionacArena)
            return base.ActiveAOEs(slot, actor);
        else
            return [];
    }
}

sealed class DetermineArena(ModuleBase module) : ModuleComponent(module)
{
    public bool IsBrionacArena;

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (IsBrionacArena && ArenaBottom.Contains(pc.Position - ArenaCenterBottom))
        {
            IsBrionacArena = false;
            Center = ArenaCenterBottom;
            Bounds = ArenaBottom;
        }
        else if (!IsBrionacArena && ArenaTop.Contains(pc.Position - ArenaCenterTop))
        {
            IsBrionacArena = true;
            Center = ArenaCenterTop;
            Bounds = ArenaTop;
        }
    }
}

sealed class BossHealths(ModuleBase module) : ModuleComponent(module)
{
    private readonly Actor? _bossHellDiver = module.Enemies((uint)OID.FourthLegionHelldiver1)[0];

    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"Top: {Module.PrimaryActor.HPRatio * 100f:f1}%, Bottom: {_bossHellDiver?.HPRatio * 100f:f1}%");
    }
}

[ModuleInfo(CFCID = 735u, NameID = 9436u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class CLL1Brionac4thLegionHelldiver : ModuleBase
{
    public CLL1Brionac4thLegionHelldiver(WorldState ws, Actor primary) : base(ws, primary, ArenaCenterBottom, ArenaBottom)
    {
        ActivateComponent<DetermineArena>();
    }

    public Actor? BossHellDiver;

    protected override void UpdateModule()
    {
        BossHellDiver ??= GetActor((uint)OID.FourthLegionHelldiver1);
    }

    protected override bool CheckPull() => base.CheckPull() || (BossHellDiver?.InCombat ?? false);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        if (Center == ArenaCenterTop)
        {
            Arena.Actor(PrimaryActor);
        }
        else
        {
            Arena.Actors(Enemies((uint)OID.FourthLegionHelldiver3));
            Arena.Actor(BossHellDiver);
        }
        var skyarmors = Enemies((uint)OID.FourthLegionSkyArmor);
        var count = skyarmors.Count;
        for (var i = 0; i < count; ++i)
        {
            var skyarmor = skyarmors[i];
            if (InBounds(skyarmor.Position))
            {
                Arena.Actor(skyarmor);
            }
        }
    }

    public static readonly WPos ArenaCenterBottom = new(80f, -179.41f);
    public static readonly ArenaBoundsRect ArenaBottom = new(29.58f, 24.59f);
    public static readonly WPos ArenaCenterTop = new(new(80f, -222f));
    public static readonly ArenaBoundsRect ArenaTop = new(29.5f, 14.5f);

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        var potHints = CollectionsMarshal.AsSpan(hints.PotentialTargets);
        var center = Center;
        for (var i = 0; i < count; ++i)
        {
            var h = potHints[i];
            var e = h.Actor;
            var enemyPrio = h.Priority;
            var oid = e.OID;
            if (center == ArenaCenterTop)
            {
                if (oid == (uint)OID.MagitekCore)
                    enemyPrio = 1;
                else if (e == PrimaryActor && e.HPRatio - BossHellDiver?.HPRatio < -0.1f)
                    enemyPrio = AIHints.Enemy.PriorityForbidden;
                else if (oid == (uint)OID.FourthLegionSkyArmor && InBounds(e.Position))
                    enemyPrio = 0;
                else if (oid != (uint)OID.Boss)
                    enemyPrio = AIHints.Enemy.PriorityInvincible;
            }
            else
            {
                if (oid == (uint)OID.FourthLegionHelldiver3)
                    enemyPrio = 1;
                else if (e == BossHellDiver && e.HPRatio - PrimaryActor.HPRatio < -0.1f)
                    enemyPrio = AIHints.Enemy.PriorityForbidden;
                else if (oid == (uint)OID.FourthLegionSkyArmor && InBounds(e.Position))
                    enemyPrio = 0;
                else if (oid != (uint)OID.FourthLegionHelldiver1)
                    enemyPrio = AIHints.Enemy.PriorityInvincible;
            }
            h.Priority = enemyPrio;
        }
    }
}
