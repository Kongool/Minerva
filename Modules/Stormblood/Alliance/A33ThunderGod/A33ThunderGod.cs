// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A33ThunderGod;

class HallowedBolt(ModuleBase module) : Components.ConcentricAOEs(module, _shapes)
{
    private static readonly AOEShape[] _shapes = [new AOEShapeCircle(15f), new AOEShapeDonut(15f, 30f)];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HallowedBolt1)
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (Sequences.Count != 0)
        {
            var order = spell.Action.ID switch
            {
                (uint)AID.HallowedBolt1 => 0,
                (uint)AID.HallowedBolt2 => 1,
                _ => -1
            };
            AdvanceSequence(order, spell.LocXZ, World.FutureTime(2d));
        }
    }
}

[ModuleInfo(CFCID = 636u, NameID = 7899u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")] //7917
public class A33ThunderGod(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-600, -600), arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Circle(new(-612.5f, -578.4f), 10), new Circle(new(-587.5f, -578.4f), 10), new Circle(new(-575, -600), 10), new Circle(new(-587.5f, -621.5f), 10), new Circle(new(-612.5f, -621.5f), 10), new Circle(new(-625, -600), 10), new Donut(new(-600, -600), 20, 27)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.EphemeralKnight));
    }
}
