// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;
using static Minerva.Dawntrail.Raid.BruteAmbombinatorSharedBounds.BruteAmbombinatorSharedBounds;


namespace Minerva.Dawntrail.Raid.M07NBruteAbombinator;

sealed class BrutalImpactRevengeOfTheVines1NeoBombarianSpecial(ModuleBase module) : Components.RaidwideCasts(module,
[(uint)AID.BrutalImpact, (uint)AID.RevengeOfTheVines1, (uint)AID.NeoBombarianSpecial]);

sealed class Powerslam(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Powerslam);
sealed class Slaminator(ModuleBase module) : Components.CastTowers(module, (uint)AID.Slaminator, 8f, 8, 8);

sealed class ElectrogeneticForce(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.ElectrogeneticForce, 6f);
sealed class SporeSac(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SporeSac, 8f);
sealed class Pollen(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pollen, 8f);
sealed class Sporesplosion : Components.SimpleAOEs
{
    public Sporesplosion(ModuleBase module) : base(module, (uint)AID.Sporesplosion, 8f, 12)
    {
        MaxDangerColor = 6;
    }
}

sealed class Explosion : Components.SimpleAOEs
{
    public Explosion(ModuleBase module) : base(module, (uint)AID.Explosion, 25f, 2)
    {
        MaxDangerColor = 1;
    }
}

sealed class ItCameFromTheDirt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ItCameFromTheDirt, 6f);
sealed class CrossingCrosswinds(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CrossingCrosswinds, new AOEShapeCross(50f, 5f));
sealed class CrossingCrosswindsHint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.CrossingCrosswinds, showNameInHint: true);
sealed class TheUnpotted(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheUnpotted, new AOEShapeCone(60f, 15f.Degrees()));
sealed class WindingWildwinds(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WindingWildwinds, new AOEShapeDonut(5f, 60f));
sealed class WindingWildwindsHint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.WindingWildwinds, showNameInHint: true);
sealed class GlowerPower(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GlowerPower, new AOEShapeRect(65f, 7f));

sealed class BrutishSwingCircle2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BrutishSwingCircle2, 12f);
sealed class BrutishSwingDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BrutishSwingDonut, new AOEShapeDonut(9f, 60f));

sealed class BrutishSwingCone(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BrutishSwingCone1, (uint)AID.BrutishSwingCone2], new AOEShapeCone(25f, 90f.Degrees()));
sealed class BrutishSwingDonutSegment(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.BrutishSwingDonutSegment1, (uint)AID.BrutishSwingDonutSegment2], new AOEShapeDonutSector(22f, 88f, 90f.Degrees()));
sealed class LashingLariat(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.LashingLariat1, (uint)AID.LashingLariat2], new AOEShapeRect(70f, 16f));

sealed class NeoBombarianSpecialKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.NeoBombarianSpecial, 58f, true)
{
    private RelSimplifiedComplexPolygon poly;
    private bool polyInit;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Bounds == KnockbackArena) // this doesn't seem to be a regular knockback that ends up in PendingKnockbacks, so forbidden zone must stay longer than cast
        {
            if (!polyInit)
            {
                poly = KnockbackArena.Polygon.Offset(-1f); // shrink polygon by 1 yalm for less suspect kb
                polyInit = true;
            }
            if (Casters.Count == 0)
                return;
            ref readonly var c = ref Casters.Ref(0);
            hints.AddForbiddenZone(new SDKnockbackInComplexPolygonAwayFromOrigin(Center, Module.PrimaryActor.Position, 58f, poly), c.Activation);
        }
    }
}

sealed class PulpSmash(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.PulpSmash, (uint)AID.PulpSmash, 6f, 5.2f, 8, 8);

[ModuleInfo(CFCID = 1023u, NameID = 13756u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M07NBruteAbombinator(WorldState ws, Actor primary) : ModuleBase(ws, primary, FirstCenter, DefaultArena)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.BloomingAbomination));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.BloomingAbomination => 1,
                _ => 0
            };
        }
    }
}
