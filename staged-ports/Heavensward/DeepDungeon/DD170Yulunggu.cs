// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.DD170Yulunggu;

public enum OID : uint
{
    Boss = 0x181E, // R5.750, x1
    Voidzone = 0x1E9998 // R0.500, x0 (spawn during fight), EventObj type
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target

    Douse = 7158, // Boss->self, 2.0s cast, range 8 circle
    Drench = 7160, // Boss->self, no cast, range 10+R ?-degree cone
    Electrogenesis = 7161, // Boss->location, 3.0s cast, range 8 circle
    FangsEnd = 7159 // Boss->player, no cast, single-target
}

class Douse(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 8f, (uint)AID.Douse, GetVoidzones, 0.8f)
{
    public static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.Voidzone);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}
class DousePuddle(ModuleBase module) : ModuleComponent(module)
{
    private readonly Actor[] puddles = Douse.GetVoidzones(module);

    private bool BossInPuddle
    {
        get
        {
            var len = puddles.Length;
            for (var i = 0; i < len; ++i)
            {
                if (Module.PrimaryActor.Position.InCircle(puddles[i].Position, 8f + Module.PrimaryActor.HitboxRadius))
                    return true;
            }
            return false;
        }
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        // indicate on minimap how far boss needs to be pulled
        if (BossInPuddle)
            Arena.ZoneCircleOutline(Module.PrimaryActor.Position, Module.PrimaryActor.HitboxRadius, Colors.Danger);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Module.PrimaryActor.TargetID == actor.InstanceID && BossInPuddle)
            hints.Add("Pull boss out of puddle!");
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Module.PrimaryActor.TargetID == actor.InstanceID && BossInPuddle)
        {
            var primary = Module.PrimaryActor;
            var hitbox = primary.HitboxRadius;
            var effPuddleSize = 8f + hitbox;
            var tankDist = hints.FindEnemy(primary)?.TankDistance ?? 2f;
            // yaquaru tank distance seems to be around 2-2.5y, but from testing, 3y minimum is needed to move it out of the puddle, either because of rasterization shenanigans or netcode
            var effTankDist = hitbox + tankDist + 1f;

            var len = puddles.Length;
            var puddlez = new ShapeDistance[len];
            for (var i = 0; i < len; ++i)
            {
                puddlez[i] = new SDCircle(puddles[i].Position, effPuddleSize + effTankDist);
            }
            var closest = new SDUnion(puddlez);
            hints.GoalZones.Add(p => closest.Distance(p) > 0f ? 1000f : 0f);
        }
    }
}

class Drench(ModuleBase module) : Components.Cleave(module, (uint)AID.Drench, new AOEShapeCone(15.75f, 45f.Degrees()), activeWhileCasting: false);
class Electrogenesis(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Electrogenesis, 8f);

class DD170YulungguStates : StateMachineBuilder
{
    public DD170YulungguStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Douse>()
            .ActivateOnEnter<DousePuddle>()
            .ActivateOnEnter<Drench>()
            .ActivateOnEnter<Electrogenesis>();
    }
}

[ModuleInfo(CFCID = 215u, NameID = 5449u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD170Yulunggu(WorldState ws, Actor primary) : ModuleBase(ws, primary, SharedBounds.ArenaBounds160170180190.Center, SharedBounds.ArenaBounds160170180190);
