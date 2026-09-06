using System.Numerics;
// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A30Shantoto;

sealed class FlarePlay(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FlarePlay);

sealed class Vidohunir(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.Vidohunir1, 5f);

sealed class EmpiricalResearch(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EmpiricalResearch1, new AOEShapeRect(80f, 6f));

sealed class SuperiorStoneIITelegraph(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SuperiorStoneII1, new AOEShapeRect(21f, 6.5f));

sealed class SuperiorStoneIIArena(ModuleBase module) : ModuleComponent(module)
{
    public sealed class Cutoff(WPos wpos, Angle angle, Vector4 posrot)
    {
        public WPos Position = wpos;
        public Angle Rotation = angle;
        public Vector4 PosRot = posrot;
    }

    private readonly List<Cutoff> cutoffs = [];

    private readonly Rectangle _basearena = new(new(0f, -720f), 24f, 30f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SuperiorStoneII1)
        {
            cutoffs.Add(new(caster.Position, caster.Rotation, caster.PosRot));
            //Service.Log.Debug($"Adding cutoff {caster.Position}, {caster.Rotation}");
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SuperiorStoneII)
        {
            var _rects = new Rectangle[cutoffs.Count];
            for (var i = 0; i < cutoffs.Count; i++)
            {
                var pos = cutoffs[i].Position + cutoffs[i].Rotation.ToDirection() * 10.5f;
                _rects[i] = new Rectangle(pos, 6.1f, 10.5f, cutoffs[i].Rotation); //This functions but could probably be more accurate.
                //Service.Log.Debug($"Adding Rectangle {_rects[i]}");
            }

            var newbounds = new ArenaBoundsCustom([_basearena], _rects);
            Bounds = newbounds;
        }
        if (spell.Action.ID == (uint)AID.GroundbreakingQuake1)
        {
            Bounds = new ArenaBoundsRect(24f, 30f);
            cutoffs.Clear();
        }
    }
}

sealed class GroundBreakingQuake(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GroundbreakingQuake1, new AOEShapeRect(30f, 6f));

/*
 * Use tethers to trace the path from one circumscribed fire to the next.
 * It is kind of like a chasing AOE except the tether isn't attached to a pc, it leads to next AOE donut.
 */
sealed class DiagrammaticDoorway(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _predicted = [];

    /*
     * convert _predicted to ReadOnlySpan. If empty return empty span, else return only 0th index so aoe does not overlap.
     */
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_predicted.Count() == 0 ? _predicted : _predicted[..1]);

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        switch ((uint)(TetherID)tether.ID)
        {
            case (uint)TetherID.Purple:
                // First cast is a long cast, else normal short cast.
                if (_predicted.Count == 0)
                {
                    _predicted.Add(new(new AOEShapeDonut(6, 70), source.Position, default, World.FutureTime(10.1f)));
                }
                _predicted.Add(new(new AOEShapeDonut(6, 70), World.Actors.Find(tether.Target)!.Position, default, _predicted[^1].Activation.AddSeconds(3.1f)));
                break;
            case (uint)TetherID.PurpleCircle:
                {
                    _predicted.Add(new(new AOEShapeDonut(6, 70), World.Actors.Find(tether.Target)!.Position,
                        default, _predicted[^1].Activation.AddSeconds(3.1f)));
                }
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.CircumscribedFire or (uint)AID.CircumscribedFire1)
        {
            NumCasts++;
            if (_predicted.Count > 0)
                _predicted.RemoveAt(0);
        }
    }
}

sealed class LocalizedBlizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LocalizedBlizzard, new AOEShapeCircle(10f));

sealed class ThunderAndError(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.ThunderAndError, 5f);

sealed class SmallSpecimen(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SmallSpecimen, new AOEShapeCircle(6f));

sealed class LargeSpecimen(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LargeSpecimen, new AOEShapeCircle(13f));

sealed class StardustSpecimen(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.StardustSpecimen, 6f);

sealed class Shockwave(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Shockwave);

sealed class FallingRubble(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRubble, new AOEShapeRect(35f, 5f));

sealed class FallingRubble1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRubble1, new AOEShapeCircle(12f));

sealed class FallingRubble2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRubble2, new AOEShapeCircle(8f));

sealed class FallingRubble3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRubble3, new AOEShapeRect(25f, 3f));

sealed class FinalExam(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.FinalExam1, 6f);

[ModuleInfo(CFCID = 1117u, NameID = 14778u, PrimaryActorOID = (uint)OID.ShantottoTheDemon, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team, HerStolenLight (ported from BMR)")]
public sealed class A30Shantoto(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsRect(24f, 30f))
{
    public static readonly WPos ArenaCenter = new(0f, -720f);
}
