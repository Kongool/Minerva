// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Raid.M11NTheTyrant;

sealed class CrownOfArcadia(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(20, 6, 20);
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var color = Colors.AOE;


        for (var i = 0; i < count; ++i)
        {
            ref var aoe = ref aoes[i];
            if (count > 0)
            {
                aoe.Color = color;
                aoe.Risky = true;
            }
        }
        return aoes;
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID._Weaponskill_CrownOfArcadia)
        {
            _aoes.Add(new(rect, new WPos(74, 100)));
            _aoes.Add(new(rect, new WPos(126, 100)));
        }
    }
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID._Weaponskill_CrownOfArcadia)
        {
            _aoes.Clear();
        }
    }
}





sealed class Smashdown1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_SmashdownScytheAOE, new AOEShapeDonut(5, 60));
sealed class Smashdown2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_SmashdownAxeAOE, 8f);
sealed class Smashdown3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_SmashdownSwordAOE, new AOEShapeCross(40f, 5f));

/**
 * Void Stardust: Marks all players with AoEs that continue hitting them as well as spawning a ground AoE underneath.
 */
sealed class VoidStardust(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.VoidStardustSpread, (uint)AID._Spell_VoidStardust, 4f, 4.7f)
{
    public int _numCasts;
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID._Spell_VoidStardust)
            _numCasts = 0;
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID._Spell_Cometite or AID._Spell_Comet1)
        {
            _numCasts++;
        }
        if (_numCasts >= 16)
        {
            Spreads.RemoveAll(s => s.Target.InstanceID == spell.MainTargetID);
            ++NumFinishedSpreads;
        }
    }
}
sealed class Cometite(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Spell_Cometite, 4f);

sealed class AssaultEvolved1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_AssaultEvolvedSword, new AOEShapeCross(40, 5f));
sealed class AssaultEvolved2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_AssaultEvolvedScythe, new AOEShapeDonut(5, 60));

sealed class AssaultEvolved3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_AssaultEvolvedAxe, 8f);
sealed class DanceOfDomination(ModuleBase module) : Components.RaidwideCast(module, (uint)AID._Weaponskill_DanceOfDomination);
sealed class Explosion1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_Explosion, new AOEShapeRect(60f, 5f));
sealed class Explosion2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_Explosion, new AOEShapeRect(60f, 5f));
sealed class Explosion3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_Explosion, new AOEShapeRect(60f, 3f));

sealed class RawSteelTankBuster(ModuleBase module) : Components.IconSharedTankbuster(module, (uint)IconID.RawSteelSharedTankbuster, (uint)AID._Weaponskill_RawSteel1, 6f);
sealed class RawSteelSpreads(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.RawSteelSpread, (uint)AID._Weaponskill_Impact, 6, 0);
sealed class Charybdistopia(ModuleBase module) : Components.RaidwideCast(module, (uint)AID._Spell_Charybdistopia);


sealed class Maelstrom(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCircle circ = new(5);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var color = Colors.AOE;


        for (var i = 0; i < count; ++i)
        {
            ref var aoe = ref aoes[i];
            if (count > 0)
            {
                aoe.Color = color;
                aoe.Risky = true;
            }
        }
        return aoes;
    }


    public override void OnActorCreated(Actor actor)
    {
        base.OnActorCreated(actor);
        if ((OID)actor.OID is OID.Maelstrom)
        {
            _aoes.Add(new(circ, actor.Position));
        }
    }
    public override void OnActorDestroyed(Actor actor)
    {
        if ((OID)actor.OID is OID.Maelstrom)
        {
            _aoes.Clear();
        }
    }
}

//TODO : There is a fast cast of powerful gust that happens when boss teleports back to center that covers all radar and makes ai have a seizure.
// would be great to get rid of that.
class PowerfulGust(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCone cone = new(60f, 22.5f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var color = Colors.AOE;


        for (var i = 0; i < count; ++i)
        {
            ref var aoe = ref aoes[i];
            if (count > 0)
            {
                aoe.Color = color;
                aoe.Risky = true;
            }
        }
        return aoes;
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID._Spell_PowerfulGust)
        {
            _aoes.Add(new(cone, caster.CastInfo!.LocXZ, caster.CastInfo!.Rotation));

        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count > 0 && (AID)spell.Action.ID is AID._Spell_PowerfulGust)
        {
            _aoes.RemoveAt(0);
        }
    }
}

sealed class OneAndOnly(ModuleBase module) : Components.RaidwideCast(module, (uint)AID._Weaponskill_OneAndOnly);
sealed class CosmicKiss(ModuleBase module) : Components.CastTowers(module, (uint)AID._Ability_CosmicKiss, 4f);
sealed class MassiveMeteor(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.MassiveMeteorStack, (uint)AID._Ability_MassiveMeteor, 6, 0);
sealed class ForegoneFatality(ModuleBase module) : Components.TankbusterTether(module, (uint)AID._Spell_ForegoneFatality, (uint)TetherID._Gen_TankInterceptTether, 6f);

//TODO: Needs to run for the stone behind a stone. Right now it just goes to nearest stone. Could get hit by the second wave.
sealed class DoubleTyrannhilation(ModuleBase module)
    : Components.CastLineOfSightAOE(module, (uint)AID._Weaponskill_DoubleTyrannhilation1, 60f)
{
    public override ReadOnlySpan<Actor> BlockerActors()
    {
        var comets = Module.Enemies((uint)OID.Comet);
        var count = comets.Count;
        if (count == 0)
            return default;

        var blockers = new List<Actor>();
        for (var i = 0; i < count; ++i)
        {
            var c = comets[i];
            if (!c.IsDead)
                blockers.Add(c);
        }

        return CollectionsMarshal.AsSpan(blockers);
    }
}

sealed class HiddenTyrannhilation(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID._Weaponskill_HiddenTyrannhilation, 60f, false)
{
    public override ReadOnlySpan<Actor> BlockerActors()
    {
        var comets = Module.Enemies((uint)OID.Comet);
        var count = comets.Count;
        if (count == 0)
            return default;

        var blockers = new List<Actor>();
        for (var i = 0; i < count; ++i)
        {
            var c = comets[i];
            if (!c.IsDead)
                blockers.Add(c);
        }

        return CollectionsMarshal.AsSpan(blockers);
    }
}

sealed class Flatliner(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(20, 6, 20);
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var color = Colors.AOE;


        for (var i = 0; i < count; ++i)
        {
            ref var aoe = ref aoes[i];
            if (count > 0)
            {
                aoe.Color = color;
                aoe.Risky = true;
            }
        }
        return aoes;
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID._Weaponskill_Flatliner)
        {
            _aoes.Add(new(rect, caster.CastInfo!.LocXZ, caster.CastInfo!.Rotation));
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        // We cheat here instead of drawing two new shapes, we make a danger zone between the rectangles.
        if ((AID)spell.Action.ID is AID._Weaponskill_Flatliner)
        {
            Bounds = new ArenaBoundsRect(26, 20);
        }
        if ((AID)spell.Action.ID is AID._Weaponskill_CrownOfArcadia)
        {
            Bounds = new ArenaBoundsRect(20, 20);
            _aoes.Clear();
        }
    }
}

sealed class FlatlinerKnockUp(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID._Weaponskill_FlatlinerKnockup, 15, true);

sealed class MajesticMeteor(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Spell_MajesticMeteor1, 6f);
sealed class MajesticMeteorain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Spell_MajesticMeteorain, new AOEShapeRect(60f, 5f));
sealed class MammothMeteor(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Spell_MammothMeteor, new AOEShapeCircle(22));
sealed class FireAndFury1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_FireAndFuryCone1, new AOEShapeCone(60f, 45.Degrees()));
class FireAndFury2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID._Weaponskill_FireAndFuryCone2, new AOEShapeCone(60f, 45.Degrees()));

/**
 * We create the explosion towers with GenericTowers so we can access the constructor fields for ExplosionKnockUp
 */
sealed class ExplosionTower(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ExplosionKnockUp)
        {
            Towers.Add(new(spell.LocXZ, 4f, 1, 8));
        }
    }
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ExplosionKnockUp)
        {
            Towers.Clear();
        }
    }
}

sealed class ExplosionKnockUp(ModuleBase module) : Components.GenericKnockback(module)
{
    private static readonly AOEShapeCircle circle = new(4f);
    private readonly ExplosionTower _tower = module.FindComponent<ExplosionTower>()!;
    private Knockback[] _kbs = [];

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kbs;

    public override void Update()
    {
        var towers = CollectionsMarshal.AsSpan(_tower.Towers);
        if (towers.Length == 0)
        {
            _kbs = [];
            return;
        }
        _kbs = new Knockback[4];
        for (var i = 0; i < towers.Length; ++i)
        {
            ref var t = ref towers[i];
            _kbs[i] = new(t.Position, 25f, t.Activation, circle);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var towers = CollectionsMarshal.AsSpan(_tower.Towers);
        var len = towers.Length;
        if (len == 0)
        {
            return;
        }
        ref var t0 = ref towers[0];
        if ((t0.Activation - World.CurrentTime).TotalSeconds < 6d)
        {
            for (var i = 0; i < len; ++i)
            {
                ref var t = ref towers[i];
                if (t.IsInside(actor))
                {
                    hints.ActionsToExecute.Push(ActionDefinitions.Armslength, actor, ActionQueue.Priority.High);
                    hints.ActionsToExecute.Push(ActionDefinitions.Surecast, actor, ActionQueue.Priority.High);
                    return;
                }
            }
        }
    }
}

// TODO: doesn't identify which platform will be destroyed in order to jump away.
class ArcadionAvalanche(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(40f, 20f);
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];

        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var color = Colors.AOE;

        for (var i = 0; i < count; ++i)
        {
            ref var aoe = ref aoes[i];
            if (count > 0)
            {
                aoe.Color = color;
                aoe.Risky = true;
            }
        }
        return aoes;
    }
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID.ArcadionAvalanche)
        {
            _aoes.Add(new(rect, caster.CastInfo!.LocXZ, caster.CastInfo!.Rotation));
        }
    }
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID is AID._Weaponskill_CrownOfArcadia)
        {
            _aoes.Clear();
        }
    }
}

// TODO: the platform that gets smashed should be marked dangerous. Then need to show where the aoe will hit on surviving platform.
sealed class ArcadionAvalancheToss(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArcadionAvalancheToss, new AOEShapeRect(40f, 20f));
sealed class HeartbreakKick(ModuleBase module) : Components.CastTowers(module, (uint)AID._Weaponskill_HeartbreakKick, 4, 1, 8)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            Towers.Add(new(spell.LocXZ, Radius, MinSoakers, MaxSoakers, activation: Module.CastFinishAt(spell)));
        if ((uint)(AID)spell.Action.ID is (uint)AID._Spell_MajesticMeteor)
        {
            var pos = spell.LocXZ;
            Towers.RemoveAll(t => t.Position.AlmostEqual(pos, 1));
        }
    }
    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    { }
}

sealed class GreatWallOfFire(ModuleBase module) : Components.IconSharedTankbuster(module, (uint)IconID.WallOfFireTankbuster, (uint)AID._Weaponskill_GreatWallOfFire, new AOEShapeRect(60, 3f));

// =========================
// Module
// =========================

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1072u, CFCID = 1072u, NameID = 14305u, PrimaryActorOID = (uint)OID.Boss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "VeraNala, wen, Topas (ported from BMR)")]
[SkipLocalsInit]
public sealed class M11NTheTyrant(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f));
