// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Quantum.Q1FinalVerse;

[SkipLocalsInit]
sealed class TerrorEyeVoidTrapBallOfFire(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TerrorEye, (uint)AID.BallOfFire, (uint)AID.VoidTrap], 6f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1063u, CFCID = 1063u, NameID = 14037u, PrimaryActorOID = (uint)OID.EminentGrief, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = " (ported from BMR)")]
[SkipLocalsInit]
public sealed class Q1FinalVerse : ModuleBase
{
    public Q1FinalVerse(WorldState ws, Actor primary) : base(ws, primary, ArenaCenter, new ArenaBoundsCustom([new Rectangle(ArenaCenter, 20f, 15f)], AdjustForHitboxOutwards: true))
    {
        ActivateComponent<LightAndDark>();
        FindComponent<LightAndDark>()!.AddAOE();
        vodorigas = Enemies((uint)OID.VodorigaMinion);
        bloodguards = Enemies((uint)OID.BloodguardMinion);
        fonts = Enemies((uint)OID.ArcaneFont);
    }

    public static readonly WPos ArenaCenter = new(-600f, -300f);
    public Actor? BossEater;
    private readonly List<Actor> vodorigas;
    private readonly List<Actor> bloodguards;
    private readonly List<Actor> fonts;

    protected override bool CheckPull()
    {
        BossEater ??= GetActor((uint)OID.DevouredEater);
        return base.CheckPull();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(vodorigas);
        Arena.Actors(bloodguards);
        Arena.Actors(fonts);
    }
}
