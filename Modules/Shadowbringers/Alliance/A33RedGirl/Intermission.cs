// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A33RedGirl;

sealed class IntermissionArena(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Rectangle> walls = [];
    private readonly A33RedGirl bossmod = (A33RedGirl)module;
    private PolygonCustom[] baseArena = [];
    private readonly PolygonCustom[] virusArena1 = [new([new(6f, 856f), new(-6f, 856f), new(-6f, 868f), new(-1.5f, 868f), new(-1.5f, 880f),
    new(-8f, 880f), new(-8f, 882f), new(-12f, 882f), new(-12f, 884f), new(-14f, 884f),
    new(-14f, 886f), new(-16f, 886f), new(-16f, 888f), new(-18f, 888f), new(-18f, 892f),
    new(-20f, 892f), new(-20f, 908f), new(-18f, 908f), new(-18f, 912f), new(-16f, 912f),
    new(-16f, 914f), new(-14f, 914f), new(-14f, 916f), new(-12f, 916f), new(-12f, 918f),
    new(-8f, 918f), new(-8f, 920f), new(-1.5f, 920f), new(-1.5f, 932f), new(-6f, 932f),
    new(-6f, 944f), new(6f, 944f), new(6f, 932f), new(1.5f, 932f), new(1.5f, 920f),
    new(8f, 920f), new(8f, 918f), new(12f, 918f), new(12f, 916f), new(14f, 916f),
    new(14f, 914f), new(16f, 914f), new(16f, 912f), new(18f, 912f), new(18f, 908f),
    new(20f, 908f), new(20f, 892f), new(18f, 892f), new(18f, 888f), new(16f, 888f),
    new(16f, 886f), new(14f, 886f), new(14f, 884f), new(12f, 884f), new(12f, 882f),
    new(8f, 882f), new(8f, 880f), new(1.5f, 880f), new(1.5f, 868f), new(6f, 868f)])];

    private PolygonCustom[] GenerateVirusArena(WDir offset)
    {
        var vertices = new WPos[60];
        var o = offset;
        var vertices1 = virusArena1[0].Vertices;
        for (var i = 0; i < 60; ++i)
        {
            vertices[i] = vertices1[i] + o;
        }
        return [new(vertices)];
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID is (uint)OID.WhiteWall or (uint)OID.BlackWall)
        {
            walls.Add(new Rectangle(actor.Position, 2f, 1f));
            if (walls.Count == 8)
            {
                baseArena = bossmod.RedSphere!.PosRot.Z switch
                {
                    900f => virusArena1,
                    400f => GenerateVirusArena(new(0f, -500f)),
                    _ => GenerateVirusArena(new(0f, -1000f))
                };
                ArenaBoundsCustom arena = new(baseArena, [.. walls]);
                Module.Bounds = arena;
                Module.Center = arena.Center;
            }
        }
    }

    public override void OnActorDeath(Actor actor)
    {
        if (actor.OID is (uint)OID.WhiteWall or (uint)OID.BlackWall)
        {
            var count = walls.Count;
            var pos = actor.Position;
            for (var i = 0; i < count; ++i)
            {
                if (walls[i].Center == pos)
                {
                    walls.RemoveAt(i);
                    break;
                }
            }
            Module.Bounds = new ArenaBoundsCustom(baseArena, [.. walls], AdjustForHitboxInwards: true);
        }
    }
}

sealed class WaveWhite(ModuleBase module) : Components.CastHint(module, (uint)AID.WaveWhite, "Be white to avoid damage!");
sealed class WaveBlack(ModuleBase module) : Components.CastHint(module, (uint)AID.WaveBlack, "Be black to avoid damage!");
sealed class BigExplosion(ModuleBase module) : Components.CastHint(module, (uint)AID.BigExplosion, "Pylons explode!", true);


// rebased from QuestBattle.RotationModule -- see FlyFreeMyPretty; the priorities are the value here.
sealed class IntermissionAIModule(ModuleBase module) : ModuleComponent(module)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID is (uint)OID.WhitePylon or (uint)OID.BlackPylon ? 2 : e.Actor.TargetID == actor.InstanceID ? 1 : 0;
        }
        base.AddAIHints(slot, actor, assignment, hints);
    }
}
