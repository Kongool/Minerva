// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T09Seiryu;

sealed class HundredTonzeSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HundredTonzeSwing, 16f);
sealed class CoursingRiver(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.CoursingRiverAOE, 25f, true, kind: Kind.DirForward)
{
    private readonly Handprint _aoe = module.FindComponent<Handprint>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_aoe.Casters.Count == 0 && Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            hints.AddForbiddenZone(new SDKnockbackInCircleFixedDirection(Center, 25f * c.Direction.ToDirection(), 19f), c.Activation);
        }
    }
}

sealed class DragonsWake(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DragonsWake2);
sealed class FifthElement(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FifthElement);
sealed class FortuneBladeSigil(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FortuneBladeSigil, new AOEShapeRect(100f, 2f));

sealed class InfirmSoul(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.InfirmSoul, 4f, tankbuster: true);

sealed class SerpentDescending(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Spreadmarker, (uint)AID.SerpentDescending, 5f, 6d);
sealed class YamaKagura(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.YamaKagura, new AOEShapeRect(60f, 3f));
sealed class Handprint(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Handprint1, new AOEShapeCone(40f, 90f.Degrees()));

sealed class ForceOfNature1(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ForceOfNature1, 10f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 10f), Casters.Ref(0).Activation);
        }
    }
}
sealed class ForceOfNature2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ForceOfNature2, 5f);
sealed class KanaboBait(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeCone(45f, 30f.Degrees()), (uint)TetherID.BaitAway, (uint)AID.KanaboVisual2, (uint)OID.IwaNoShiki, 5.9d)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (IsBaitTarget(actor))
        {
            hints.AddForbiddenZone(new SDCircle(Center, 19f), World.FutureTime(ActivationDelay));
        }
    }
}

sealed class KanaboAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Kanabo, new AOEShapeCone(45f, 30f.Degrees()));
sealed class BlueBolt(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.BlueBoltMarker, (uint)AID.BlueBolt, 5.9d, 83f, 2.5f)
{
    public override void Update()
    {
        if (CurrentBaits.Count != 0)
        {
            CurrentBaits.Ref(0).Forbidden = ForbiddenPlayers;
        }
    }
}

sealed class ForbiddenArts(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.ForbiddenArtsMarker, (uint)AID.ForbiddenArtsSecond, 5.2f, 84.4f, 4); // this hits twice
sealed class RedRush(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeRect(82.6f, 2.5f), (uint)TetherID.BaitAway, (uint)AID.RedRush, (uint)OID.AkaNoShiki, 6d)
{
    private readonly BlueBolt _stack = module.FindComponent<BlueBolt>()!;

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID != (uint)OID.AkaNoShiki)
        {
            return;
        }
        base.OnTethered(source, tether);
        var (player, enemy) = DetermineTetherSides(source, tether);
        if (player != null && enemy != null)
        {
            _stack.ForbiddenPlayers.Set(Raid.FindSlot(player.InstanceID));
        }
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID != (uint)OID.AkaNoShiki)
        {
            return;
        }
        base.OnUntethered(source, tether);
        var (player, enemy) = DetermineTetherSides(source, tether);
        if (player != null && enemy != null)
        {
            _stack.ForbiddenPlayers.Clear(Raid.FindSlot(player.InstanceID));
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (IsBaitTarget(actor))
        {
            hints.AddForbiddenZone(Bounds.Radius >= 20f ? new SDInvertedCircle(Center, 5f) : new SDCircle(Center, 18.5f), World.FutureTime(ActivationDelay));
        }
    }
}

sealed class ArenaChange(ModuleBase module) : ModuleComponent(module)
{

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.StrengthOfSpirit) // in phase 2 the arena no longer got a wall and we need to add back the player hitboxradius
        {
            Bounds = Seiryu.Phase2Bounds;
        }
    }
}

public abstract class Seiryu(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, phase1Bounds)
{
    private static readonly WPos arenaCenter = new(100f, 100f);
    private static readonly ArenaBoundsCustom phase1Bounds = new([new Polygon(arenaCenter, 19.5f, 48)]);
    public static readonly ArenaBoundsCustom Phase2Bounds = new([new Polygon(arenaCenter, 20f, 48)]);
    public static readonly ArenaBoundsCustom Phase2WaterBounds = new([new Polygon(arenaCenter, 44.5f, 48)]);
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 637u, CFCID = 637u, NameID = 7922u, PrimaryActorOID = (uint)OID.Seiryu, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class T09Seiryu(WorldState ws, Actor primary) : Seiryu(ws, primary)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.DoroNoShiki));
        Arena.Actors(Enemies((uint)OID.NumaNoShiki));
    }
}
