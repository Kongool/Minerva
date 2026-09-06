// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Dungeon.D06AetherochemicalResearchFacility.D060ClonedConjurer;

public enum OID : uint
{
    Boss = 0xF56, // R0.6
    ClonedThaumaturge = 0xF57 // R0.6
}

public enum AID : uint
{
    Fire = 966, // ClonedThaumaturge->player, 1.0s cast, single-target
    Aero = 969, // Boss->player, 1.0s cast, single-target
    Tornado = 900, // Boss->player, 5.0s cast, range 6 circle
    Breakga = 2340, // ClonedThaumaturge->player, 4.0s cast, range 5 circle
    Drain = 2339 // ClonedThaumaturge->player, 4.0s cast, single-target
}

class Tornado(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Tornado, 6f);

class D060ClonedConjurerStates : StateMachineBuilder
{
    public D060ClonedConjurerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Tornado>()
            .Raw.Update = () => AllDeadOrDestroyed(D060ClonedConjurer.Trash);
    }
}

[ModuleInfo(CFCID = 38u, NameID = 3838u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D060ClonedConjurer(WorldState ws, Actor primary) : ModuleBase(ws, primary, IsArena1(primary) ? arena1.Center : ShabtiConjurer2Bounds.Arena.Center, IsArena1(primary) ? arena1 : ShabtiConjurer2Bounds.Arena)
{
    private static bool IsArena1(Actor primary) => primary.PosRot.Z > 200;
    private static readonly WPos[] vertices1 = [new(266.39f, 227.51f), new(274.35f, 232.12f), new(274.55f, 232.58f), new(274.59f, 237.89f), new(274.8f, 238.36f),
    new(275.3f, 238.28f), new(278.46f, 236.46f), new(278.89f, 236.76f), new(281.09f, 238.03f), new(281.5f, 241.98f),
    new(281.8f, 242.4f), new(284.99f, 244.22f), new(285.46f, 244.4f), new(288.59f, 242.6f), new(289.1f, 242.61f),
    new(291.76f, 244.15f), new(291.91f, 247.73f), new(291.99f, 248.24f), new(292.41f, 248.51f), new(294.63f, 249.8f),
    new(294.26f, 250.15f), new(285.84f, 255.3f), new(285.33f, 255.5f), new(282.58f, 253.91f), new(282.31f, 249.97f),
    new(282, 249.58f), new(278.86f, 247.78f), new(278.37f, 247.61f), new(277.92f, 247.84f), new(275.27f, 249.37f),
    new(274.76f, 249.41f), new(272.08f, 247.86f), new(271.92f, 243.95f), new(271.62f, 243.54f), new(271.12f, 243.7f),
    new(266.63f, 246.27f), new(266.12f, 246.35f), new(261.58f, 243.76f), new(261.12f, 243.54f), new(260.64f, 243.34f),
    new(258.42f, 242.05f), new(258.01f, 238.19f), new(258.08f, 237.6f), new(258.1f, 232.2f), new(266.15f, 227.56f)];
    private static readonly ArenaBoundsCustom arena1 = new([new PolygonCustom(vertices1)]);

    public static readonly uint[] Trash = [(uint)OID.Boss, (uint)OID.ClonedThaumaturge];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(this, Trash);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            hints.PotentialTargets[i].Priority = 0;
        }
    }
}
