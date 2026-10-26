// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.ThirdBoard.CavalierPiece;

public enum OID : uint
{
    CavalierPiece = 0x4C8E,
    Helper = 0x233C,
    BoneBishop = 0x4C90, // R0.750, x0 (spawn during fight)
    BoneFragmentOrb = 0x4C92, // R1.000, x0 (spawn during fight)
    FeintedCavalierPiece = 0x4C8F, // R2.520, x5
}

public enum AID : uint
{
    AutoAttack = 49680, // CavalierPiece->player, no cast, single-target
    AutoAttackBlizzard = 48621, // 4C90->player, no cast, single-target
    SteelripperBoss = 48472, // CavalierPiece->self, 6.0+1.0s cast, single-target
    Steelripper = 48473, // Helper->self, 7.0s cast, range 60 130.000-degree cone
    Doubling = 48460, // CavalierPiece->self, 5.0s cast, single-target
    MenaceActor = 48463, // 4C8F->self, 5.4+0.6s cast, single-target
    Menace = 48464, // Helper->self, 6.0s cast, range 20 circle
    ValfodrActor = 48461, // 4C8F->self, 5.6+0.4s cast, single-target
    Valfodr = 48462, // Helper->self, 6.0s cast, range 60 width 8 rect
    CrushingBlade = 48471, // CavalierPiece->player, 5.0s cast, single-target
    FeintedCavalierTeleport = 50552, // 4C8F->CavalierPiece, no cast, single-target

    Unknown1 = 50551, // 4C92->CavalierPiece, no cast, single-target - most likely the orb teleport
}

public enum IconID : uint
{
    TankBusterKnockBack = 633, // CavalierPiece->player
}

public enum TetherID : uint
{
    DoublingTether = 398, // 4C92->CavalierPiece
}

sealed class Steelripper(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Steelripper, new AOEShapeCone(60.0f, 65.0f.Degrees()));

sealed class MenaceValfodr(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> aoes = [];
    private readonly AOEShapeCircle circle = new(20.0f);
    private readonly AOEShapeRect rectangle = new(60.0f, 4.0f);
    private const float riskyWindow = 7.0f;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FeintedCavalierTeleport)
        {
            if (caster.Position.InRect(Module.Center, 20.0f, 20.0f))
            {
                aoes.Add(new(circle, caster.Position, caster.Rotation, World.FutureTime(12.3d)));
            }

            if (!caster.Position.InRect(Module.Center, 20.0f, 20.0f))
            {
                // Actors can spawn at angles, but will always look forward where ever they spawn, so we just correct it here
                var angleCorrection = (MathF.Round(caster.Rotation.Deg / 90.0f) * 90.0f).Degrees();
                if (angleCorrection.ToDirection().Dot(caster.Position - Module.Center) > 0)
                {
                    angleCorrection += 180.0f.Degrees();
                }

                aoes.Add(new(rectangle, caster.Position, angleCorrection, World.FutureTime(11.7d)));
            }
        }

        if (spell.Action.ID is (uint)AID.Menace or (uint)AID.Valfodr)
        {
            if (aoes.Count > 0)
            {
                aoes.RemoveAt(0);
            }
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = aoes.Count;
        if (count == 0)
        {
            return [];
        }

        var incomingAOEs = CollectionsMarshal.AsSpan(aoes);
        var time = World.CurrentTime;

        for (var i = 0; i < count; i++)
        {
            ref var aoe = ref incomingAOEs[i];
            aoe.Risky = aoe.Activation.AddSeconds(-riskyWindow) <= time;
        }

        return incomingAOEs;
    }
}

sealed class CrushingBlade(ModuleBase module) : Components.GenericKnockback(module)
{
    private const float KnockbackDistance = 15.0f;
    private BitMask affectedPlayers;
    private DateTime activation = default;
    private Actor? source = null;
    private DateTime holdUntil;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.TankBusterKnockBack && Raid.FindSlot(targetID) is var slot && slot >= 0)
        {
            affectedPlayers[slot] = true;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.CrushingBlade)
        {
            activation = Module.CastFinishAt(spell);
            source = caster;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        // The shove lands about half a second after the cast resolves, measured from wherever the player is by then
        // (Ymir's Tsunami, 2026-10-25), so its ground is kept a second longer. Saar, 18:00: Bleeding at 60.6, off the floor
        // after the 58.8 Blade.
        if (spell.Action.ID == (uint)AID.CrushingBlade)
        {
            holdUntil = World.FutureTime(1d);
        }
    }

    public override void Update()
    {
        base.Update();
        if (holdUntil != default && World.CurrentTime >= holdUntil)
        {
            affectedPlayers.Reset();
            holdUntil = default;
        }
    }

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (affectedPlayers[slot] && activation != default && source != null)
        {
            return new Knockback[1] { new(source.Position, KnockbackDistance, activation) };
        }

        return [];
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!affectedPlayers[slot] || activation == default || source == null)
        {
            return;
        }

        // Not predicted as tankbuster damage, though it hits for 300-385: the familiar's Cover does not take it (Saar,
        // 2026-10-25, Covered and still hit), and Daedalus spends a worn familiar on Snarl when SecondsUntilTankbuster
        // says one is coming.
        if (IsImmune(slot, activation))
        {
            return;
        }

        // At the shove's own time. Without it the zone read as now, and Saar, 2026-10-25, stood still between two Menace
        // circles: the only ground clear of them was ground the shove would throw her off, but the circles landed 1.8s
        // before it, with time to step back in. Tested a yalm inside the walls: the test is yes or no, so the dodge's
        // margin cannot keep off its edge.
        hints.AddForbiddenZone(new SDKnockbackInAABBRectAwayFromOrigin(Module.Center, source.Position, KnockbackDistance, 19.0f, 19.0f), activation);
    }
}

sealed class CavalierPieceStates : StateMachineBuilder
{
    public CavalierPieceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Steelripper>()
            .ActivateOnEnter<MenaceValfodr>()
            .ActivateOnEnter<CrushingBlade>();
    }
}

[ModuleInfo(CFCID = 1090u, NameID = 14564u, PrimaryActorOID = (uint)OID.CavalierPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class CavalierPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(120f, 0f), new ArenaBoundsSquare(20f))
{
    // Each Bone Bishop that dies leaves a Bone Fragment, and Doubling turns every fragment on the floor into a Feinted
    // Cavalier for the next wave, five at most. Every pull runs one timer: packs at 11/49/84/106/132/156/178s, Doublings at
    // 34/65/99/126/170/198s. So two packs fall between 126 and 170, and a pack split across a Doubling carries its
    // fragments on: every pull on 2026-10-25 had five 20y Menace circles at ~188s, which leave no ground clear. Once three
    // are waiting, the bishops go below the boss until a Doubling takes them, so no wave is above three, and three always
    // leave corners clear. BossmodReborn ranks the bishops first throughout.
    private const int MaxWaitingFragments = 3;
    // Fragments a Doubling has taken: the tether lasts 1.5s, but the fragment stays two seconds more.
    private readonly HashSet<ulong> taken = [];

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var waiting = 0;
        foreach (var f in Enemies((uint)OID.BoneFragmentOrb))
        {
            if (f.IsDestroyed)
            {
                continue;
            }
            if (f.Tether.ID == (uint)TetherID.DoublingTether)
            {
                taken.Add(f.InstanceID);
            }
            else if (!taken.Contains(f.InstanceID))
            {
                ++waiting;
            }
        }
        var bishop = waiting >= MaxWaitingFragments ? 0 : 2;

        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.BoneBishop => bishop,
                (uint)OID.CavalierPiece => 1,
                _ => 0
            };
        }
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.BoneBishop));
    }

    private readonly string[] _prePullHints = [
        "This fight is easy if you kill every pack wave together and before the 4th pack spawn otherwise it starts getting complicated."
    ];

    public override string[] PrePullHints => _prePullHints;
}
