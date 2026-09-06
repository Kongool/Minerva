// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;
using static Minerva.Dawntrail.Raid.BruteAmbombinatorSharedBounds.BruteAmbombinatorSharedBounds;


namespace Minerva.Dawntrail.Savage.M07SBruteAbombinator;

sealed class BrutalImpact(ModuleBase module) : Components.CastCounter(module, (uint)AID.BrutalImpact);
sealed class RevengeOfTheVines2(ModuleBase module) : Components.CastCounter(module, (uint)AID.RevengeOfTheVines2);
sealed class Slaminator(ModuleBase module) : Components.CastTowers(module, (uint)AID.Slaminator, 8f, 8, 8);

sealed class Explosion : Components.SimpleAOEs
{
    public Explosion(ModuleBase module) : base(module, (uint)AID.Explosion, 25f, 2)
    {
        MaxDangerColor = 1;
    }
}

sealed class Sporesplosion : Components.SimpleAOEs
{
    public Sporesplosion(ModuleBase module) : base(module, (uint)AID.Sporesplosion, 8f, 12)
    {
        MaxDangerColor = 6;
    }
}

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
            ref readonly var c = ref Casters.Ref(0);
            hints.AddForbiddenZone(new SDKnockbackInComplexPolygonAwayFromOrigin(Center, Module.PrimaryActor.Position, 58f, poly), c.Activation);
        }
    }
}

[ModuleInfo(CFCID = 1024u, NameID = 13756u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M07SBruteAbombinator(WorldState ws, Actor primary) : ModuleBase(ws, primary, FirstCenter, DefaultArena)
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
