// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.FirstBoard.BoneBishop;

public enum OID : uint
{
    BoneKnight = 0x4B86, // R0.900, x?
    BoneBishop = 0x4B87, // R0.900, x?
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 50784, // BoneKnight->player, no cast, single-target

    Blizzard = 50788, // BoneBishop->player, no cast, single-target
    DeathSpiral = 46867, // BoneBishop->self, 5.0s cast, single-target
    DeathSpiral1 = 46868, // Helper->self, 6.0s cast, range 4-40 donut
    Ossify = 46871, // BoneKnight->self, 8.0s cast, single-target
    ForwardGuard = 46864, // BoneKnight->self, 5.0s cast, single-target
    BlackEruption = 46873, // BoneBishop->self, 5.0+1.0s cast, single-target
    BlackEruption1 = 46874, // Helper->location, 6.0s cast, range 5 circle
    BlackEruption2 = 46900, // Helper->location, 1.5s cast, range 5 circle
    AncientAero = 46869, // BoneBishop->self, 5.0+0.7s cast, single-target
    AncientAero1 = 46870, // Helper->self, 5.7s cast, range 40 width 8 rect
    _Ability_ = 46865, // BoneKnight->self, no cast, single-target
    Tumulus = 46866, // BoneKnight->self, 5.0s cast, range 6 circle
}

public enum SID : uint
{
    PhysicalDamageUp = 2074, // BoneKnight->BoneKnight, extra=0x0
    DirectionalParry = 680, // BoneKnight->BoneKnight, extra=0x1
    _Gen_ = 2552, // BoneKnight->BoneKnight, extra=0x425 : Ossify maybe?
    Rehabilitation = 1263, // BoneKnight->BoneKnight, extra=0x0
}

sealed class DeathSpiral(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DeathSpiral1, new AOEShapeDonut(4f, 40f));

// Puts a shield in front of himself. Player should have pet snarl why they get behind and beat him up.
// As ported, the parry only forbade the Knight while she stood on the shielded side -- Daedalus went to the Bishop -- and
// its "not from this side" cones are bounded and fire at DateTime.MaxValue, which the dodge never reads, so nothing walked
// her round. Korha, 2026-10-25: at 02:16 and 02:26 she stood at his side or front 5-6y off, on the Bishop, for the whole
// guard; at 02:43, behind him by chance, all 14 hits landed from the rear. He faced her familiar every time. Now, while a
// front guard is up or being cast and he is not facing her: stand behind him (a positioning instruction, so the dodge goes
// at once), and once she is there he is the target.
sealed class ForwardGuard(ModuleBase module) : Components.DirectionalParry(module, [(uint)OID.BoneKnight])
{
    /// <summary>How far round from straight behind counts as behind him: his rear quarter, as the game's positionals count
    /// it. 60 degrees let the nearest answer be his flank (Saar, 15:35: 56.6 degrees off, unparried but not behind).</summary>
    private static readonly Angle RearHalfAngle = 45f.Degrees();

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        Actor? knight = null;
        foreach (var (id, state) in ActorStates)
            if ((((state | (state >> 4)) & (int)Side.Front) != 0) && World.Actors.Find(id) is { IsDeadOrDestroyed: false } k)
                knight = k;
        if (knight == null || knight.TargetID == actor.InstanceID)
        {
            // nothing guarded, or he turns with her: no side to go to, so as before -- off him while she is in front
            base.AddAIHints(slot, actor, assignment, hints);
            return;
        }

        var back = (knight.Rotation + 180f.Degrees()).ToDirection();
        var behind = (actor.Position - knight.Position).Normalized().Dot(back) >= RearHalfAngle.Cos();
        hints.SetPriority(knight, behind ? 1 : AIHints.Enemy.PriorityForbidden);
        hints.AddForbiddenZone(new SDInvertedDonutSector(knight.Position, 0f, 40f, knight.Rotation + 180f.Degrees(), RearHalfAngle), DateTime.MaxValue);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ForwardGuard)
        {
            PredictParrySide(caster.InstanceID, Side.Front);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.DirectionalParry)
        {
            UpdateState(actor.InstanceID, 0);
        }
    }
}

sealed class AncientAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientAero1, new AOEShapeRect(40f, 4f));

sealed class Tumulus(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Tumulus, new AOEShapeCircle(6f));
// Black Eruption: a 5y circle, then 5y circles marching out from it along four arms, 3y a step every 2.5s, as far as the
// floor goes. As ported, the follow-ups were predicted as 28 (7 steps x 4 arms) and one prediction was dropped per
// follow-up cast, oldest first -- but a pull casts 24 to 36 of them (arms stop at the edge, some spots are cast twice), so
// predictions were left drawn for the rest of the fight (Saar, 2026-10-25, "aoe not resolved for a long time"). The arms
// were also aimed by the helper's facing, always axis-aligned, where they follow the Bishop's facing at his cast (22.5
// degrees off at 15:35, 8y wrong at the far end). Now: aimed by the Bishop, nothing predicted off the floor, a real
// follow-up replaces the prediction nearest it, and nothing outlives its moment by more than a second.
sealed class BlackEruption(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCircle _circle = new(5f);
    private Angle armsFacing;

    /// <summary>A real follow-up within this of a prediction is that prediction.</summary>
    private const float SameSpot = 1.5f;

    public override void Update()
    {
        _aoes.RemoveAll(a => a.Activation != default && a.Activation < World.CurrentTime.AddSeconds(-1d));
        _aoes.Sort(static (a, b) => a.Activation.CompareTo(b.Activation));
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        return aoes[..Math.Min(aoes.Length, 4)];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.BlackEruption:
                armsFacing = spell.Rotation;
                break;
            case (uint)AID.BlackEruption1:
                _aoes.Add(new(_circle, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell), actorID: caster.InstanceID));
                break;
            case (uint)AID.BlackEruption2:
                var nearest = -1;
                for (var k = 0; k < _aoes.Count; ++k)
                    if (_aoes[k].ActorID == 0 && (_aoes[k].Origin - spell.LocXZ).Length() <= SameSpot
                        && (nearest < 0 || (_aoes[k].Origin - spell.LocXZ).LengthSq() < (_aoes[nearest].Origin - spell.LocXZ).LengthSq()))
                        nearest = k;
                if (nearest >= 0)
                    _aoes.RemoveAt(nearest);
                _aoes.Add(new(_circle, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell), actorID: caster.InstanceID));
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is not ((uint)AID.BlackEruption1 or (uint)AID.BlackEruption2))
            return;
        ++NumCasts;
        _aoes.RemoveAll(a => a.ActorID == caster.InstanceID && (a.Origin - spell.LocXZ).Length() <= 0.1f);
        if (spell.Action.ID != (uint)AID.BlackEruption1 || caster.IsDeadOrDestroyed)
            return;
        var position = spell.LocXZ;
        for (var i = 1; i <= 7; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                var rot = armsFacing + (j * 90f).Degrees();
                var spot = position + rot.ToDirection() * (3f * i);
                if (Module.Bounds.Contains(Module.Center, spot))
                    _aoes.Add(new(_circle, spot, rot, World.FutureTime(i * 2.5d)));
            }
        }
    }

    public override void OnActorDeath(Actor actor)
    {
        if (actor.OID == (uint)OID.BoneBishop)
            _aoes.Clear();
    }
}

sealed class BoneBishopStates : StateMachineBuilder
{
    public BoneBishopStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DeathSpiral>()
            .ActivateOnEnter<ForwardGuard>()
            .ActivateOnEnter<BlackEruption>()
            .ActivateOnEnter<AncientAero>()
            .ActivateOnEnter<Tumulus>()
            .Raw.Update = () => AllDeadOrDestroyed([(uint)OID.BoneKnight, (uint)OID.BoneBishop]);
    }
}

[ModuleInfo(CFCID = 1088u, NameID = 14532u, PrimaryActorOID = (uint)OID.BoneBishop, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "wen (ported from BMR)")]
public sealed class BoneBishop(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, -420f), new ArenaBoundsCircle(20f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.BoneKnight));
    }
}
