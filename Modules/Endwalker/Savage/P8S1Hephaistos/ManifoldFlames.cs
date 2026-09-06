// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S1Hephaistos;

class ManifoldFlames : Components.UniformStackSpread
{
    public ManifoldFlames(ModuleBase module) : base(module, 0, 6)
    {
        AddSpreads(Raid.WithoutSlot(true, true, true));
    }
}

class NestOfFlamevipersCommon(ModuleBase module) : Components.CastCounter(module, (uint)AID.NestOfFlamevipersAOE)
{
    protected BitMask BaitingPlayers;

    private static readonly AOEShapeRect _shape = new(60, 2.5f);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Raid.WithSlot(false, true, true).IncludedInMask(BaitingPlayers).WhereActor(p => p != actor && _shape.Check(actor.Position, Module.PrimaryActor.Position, Angle.FromDirection(p.Position - Module.PrimaryActor.Position))).Any())
            hints.Add("GTFO from baited aoe!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var (_, player) in Raid.WithSlot(false, true, true).IncludedInMask(BaitingPlayers))
            _shape.Outline(Arena, Module.PrimaryActor.Position, Angle.FromDirection(player.Position - Module.PrimaryActor.Position));
    }
}

// variant that happens right after manifold flames and baits to 4 closest players
class NestOfFlamevipersBaited(ModuleBase module) : NestOfFlamevipersCommon(module)
{
    private BitMask _forbiddenPlayers;
    public bool Active => NumCasts == 0 && _forbiddenPlayers.Any();

    public override void Update()
    {
        BaitingPlayers = Active ? Raid.WithSlot(false, true, true).SortedByRange(Module.PrimaryActor.Position).Take(4).Mask() : default;
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (Active)
        {
            var shouldBait = !_forbiddenPlayers[slot];
            hints.Add(shouldBait ? "Move closer to bait" : "GTFO to avoid baits", shouldBait != BaitingPlayers[slot]);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if ((AID)spell.Action.ID == AID.HemitheosFlare)
            _forbiddenPlayers.Set(Raid.FindSlot(spell.MainTargetID));
    }
}

// variant that happens when cast is started and baits to everyone
class NestOfFlamevipersEveryone(ModuleBase module) : NestOfFlamevipersCommon(module)
{
    public bool Active => NumCasts == 0 && BaitingPlayers.Any();

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.NestOfFlamevipers)
            BaitingPlayers = Raid.WithSlot(false, true, true).Mask();
    }
}
