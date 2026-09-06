// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A22UltimaOmega;

sealed class IonEffluxCitadelBusterHyperPulseChemicalBomb(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.IonEfflux, (uint)AID.CitadelBuster,
(uint)AID.HyperPulse, (uint)AID.ChemicalBomb]);
sealed class Antimatter(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Antimatter);
sealed class AntiPersonnelMissile(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.AntiPersonnelMissile, 6f);

sealed class ChemicalBombHyperPulse : Components.SimpleAOEGroups
{
    public ChemicalBombHyperPulse(ModuleBase module) : base(module, [(uint)AID.ChemicalBomb, (uint)AID.HyperPulse], 25f, 2)
    {
        MaxDangerColor = 1;
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1058u, CFCID = 1058u, NameID = 14231u, PrimaryActorOID = (uint)OID.UltimaTheFeared, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class A22UltimaOmega(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter1, new ArenaBoundsRect(20f, 23.5f))
{
    public static readonly WPos ArenaCenter1 = new(800f, 800f);
    public static readonly WPos ArenaCenter2 = new(735f, 800f);
    public Actor? Omega;

    protected override void UpdateModule()
    {
        Omega ??= GetActor((uint)OID.OmegaTheOne);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(Omega);
    }
}
