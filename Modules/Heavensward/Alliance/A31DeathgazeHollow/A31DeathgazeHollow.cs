// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A31DeathgazeHollow;

class DarkII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkII, new AOEShapeCone(50, 30.Degrees()));
class BoltOfDarkness3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BoltOfDarkness3, new AOEShapeRect(31.5f, 10));
class VoidDeath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidDeath, 10);

//yoinked from Sephirot Unreal and stripped down as both mechancics use the same logic and iconID
class VoidAeroII(ModuleBase module) : ModuleComponent(module)
{
    private BitMask _greenTargets;
    private BitMask _purpleTargets;

    private const float _greenRadius = 5;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if ((_greenTargets | _purpleTargets).None())
            return;

        var clippedByGreen = Raid.WithSlot(false, false, true).Exclude(slot).IncludedInMask(_greenTargets).InRadius(actor.Position, _greenRadius).Any();
        hints.Add($"Spread! (debuff: {(_greenTargets[slot] ? "green" : _purpleTargets[slot] ? "purple" : "none")})", clippedByGreen);
    }

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
    {
        return _greenTargets[playerSlot] ? PlayerPriority.Interesting : PlayerPriority.Irrelevant;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var (slot, actor) in Raid.WithSlot(false, true, true).IncludedInMask(_greenTargets))
            Arena.ZoneCircleOutline(actor.Position, _greenRadius, Colors.Safe, slot == pcSlot ? 2 : 1);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.VoidAeroII:
                _greenTargets.Clear(Raid.FindSlot(spell.MainTargetID));
                break;
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        switch (iconID)
        {
            case (uint)IconID.WindSpread:
                _greenTargets.Set(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }
}

class VoidBlizzardIIIAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidBlizzardIIIAOE, new AOEShapeCone(60f, 10f.Degrees()));

class VoidAeroIVKB1(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.VoidAeroIVKB1, 37, kind: Kind.DirLeft, stopAtWall: true);
class VoidAeroIVKB2(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.VoidAeroIVKB2, 37, kind: Kind.DirRight, stopAtWall: true);

class Unknown3(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Unknown3, 20, stopAtWall: true);
class VoidDeathKB2(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.VoidDeathKB2, 15, kind: Kind.TowardsOrigin, stopAtWall: true);
class VoidDeathKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.VoidDeathKB, 40, kind: Kind.TowardsOrigin, stopAtWall: true);

[ModuleInfo(CFCID = 220u, NameID = 5507u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A31DeathgazeHollow(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(300, 410), new ArenaBoundsRect(30, 15))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.VoidSprite));
    }
}
