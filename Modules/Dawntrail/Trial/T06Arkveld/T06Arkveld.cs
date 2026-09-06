// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T06Arkveld;

// -----------------
// Mechanics (Normal)
// -----------------
// This is a Normal-mode port of the already-working Extreme module.
// We intentionally keep the logic simple

// Raidwides
sealed class Roar(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.Roar, (uint)AID.Roar1]);
sealed class ForgedFury(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.ForgedFury1, (uint)AID.ForgedFury2, (uint)AID.ForgedFury3]);

// Spreads / stacks
sealed class WildEnergy(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.WildEnergy, 6f);
// Normal uses the generic stack marker icon (100) for this mechanic.
sealed class ChainbladeCharge(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID._Gen_Icon_com_share2i, (uint)AID.ChainbladeCharge, 6f, 8.4d, PartyState.MaxPartySize, PartyState.MaxPartySize);

// Simple telegraphed AOEs
sealed class ChainbladeBlowLines(ModuleBase module) : Components.SimpleAOEGroups(module,
    [(uint)AID.ChainbladeBlow1, (uint)AID.ChainbladeBlow2, (uint)AID.ChainbladeBlow4, (uint)AID.ChainbladeBlow5, (uint)AID.ChainbladeBlow7, (uint)AID.ChainbladeBlow8, (uint)AID.ChainbladeBlow10, (uint)AID.ChainbladeBlow11],
    new AOEShapeRect(40f, 2f));

sealed class WyvernsRadianceCleave(ModuleBase module) : Components.SimpleAOEGroups(module,
    [(uint)AID.WyvernsRadiance, (uint)AID.WyvernsRadiance1, (uint)AID.WyvernsRadiance16, (uint)AID.WyvernsRadiance17],
    new AOEShapeRect(80f, 14f));

sealed class GuardianResonanceRect(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GuardianResonance, new AOEShapeRect(40f, 8f));

// Charges
sealed class Rush(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Rush, 6f);

// Concentric circles/donuts after charge
sealed class Concentric1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyvernsRadiance6, 8f);
sealed class Concentric2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyvernsRadiance7, new AOEShapeDonut(8f, 14f));
sealed class Concentric3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyvernsRadiance8, new AOEShapeDonut(14f, 20f));
sealed class Concentric4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyvernsRadiance9, new AOEShapeDonut(20f, 26f));

// Half-room cleave
sealed class WyvernsOuroblade(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.WyvernsOuroblade1, (uint)AID.WyvernsOuroblade3], new AOEShapeCone(40f, 90f.Degrees()));

// Rear line AOE
sealed class SteeltailThrust(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.SteeltailThrust, (uint)AID.SteeltailThrust1], new AOEShapeRect(60f, 3f));

// Towers (Aetheric Resonance): small (r2) and large (r4) versions.
// We keep soaker rules generic (1 player); damageType hints differentiate them.
sealed class ResonanceTowerSmall(ModuleBase module) : Components.CastTowers(module, (uint)AID.GuardianResonance2, 2f, 1, 1);
// Tank-only tower: add AI forbidden zone for non-tanks so the helper won't step in.
// Tank-only tower: forbid non-tanks as soakers (player hints + soaker counting) and add AI forbidden zone for non-tanks.
sealed class ResonanceTowerLarge(ModuleBase module)
    : Components.CastTowers(module, (uint)AID.GuardianResonance3, 4f, 1, 1)
{
    public override void Update()
    {
        base.Update();

        // Build a mask of party slots that are NOT tanks -> forbidden soakers
        BitMask forbid = default;
        foreach (var (slot, actor) in Module.Raid.WithSlot(excludeAlliance: true, excludeNPCs: true))
        {
            if (actor.Role != Role.Tank)
                forbid.Set(slot);
        }

        // Apply to all active towers (Tower is a struct; must assign back)
        for (var i = 0; i < Towers.Count; ++i)
        {
            var t = Towers[i];
            t.ForbiddenSoakers = forbid;
            Towers[i] = t;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);

        // also keep the AI from walking in if it's not a tank
        if (actor.Role == Role.Tank)
            return;

        foreach (var t in Towers)
            hints.AddForbiddenZone(new SDCircle(t.Position, Radius), t.Activation);
    }
}

// Crystal detonations (spawned from cracked crystals).
sealed class CrackedCrystalSmall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyvernsRadiance10, new AOEShapeCircle(6f));
sealed class CrackedCrystalLarge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WyvernsRadiance11, new AOEShapeCircle(12f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1043u, CFCID = 1043u, NameID = 14237u, PrimaryActorOID = (uint)OID.GuardianArkveld, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "JoeSparkx (ported from BMR)")]
[SkipLocalsInit]
public sealed class GuardianArkveld(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsCircle(20f));