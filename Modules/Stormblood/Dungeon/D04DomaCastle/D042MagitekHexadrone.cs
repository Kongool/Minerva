// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Dungeon.D04DomaCastle.D042MagitekHexadrone;

public enum OID : uint
{
    Boss = 0x1BD0, // R4.24
    HexadroneBit = 0x1BD2, // R0.9
    HexadroneBitHelper = 0x1BD3, // R0.5
    Helper = 0x1BD1
}

public enum AID : uint
{
    AutoAttack = 8501, // Boss->player, no cast, single-target

    CircleOfDeath = 8354, // Boss->self, 3.0s cast, range 4+R circle
    TwoTonzeMagitekMissile = 8355, // Boss->player, no cast, range 6 circle
    MagitekMissilesVisual = 8356, // Boss->self, 7.5s cast, single-target
    MagitekMissiles = 8357, // Helper->location, 8.0s cast, range 6 circle
    MagitekMissilesExplosion = 8358, // Helper->location, no cast, range 60 circle
    ChainMineVisual = 9287, // HexadroneBit->self, no cast, range 50+R width 3 rect
    ChainMine = 8359 // HexadroneBitHelper->player, no cast, single-target
}

public enum TetherID : uint
{
    HexadroneBits = 60 // HexadroneBit->HexadroneBit
}

public enum IconID : uint
{
    Stackmarker = 62 // player
}

class MagitekMissile(ModuleBase module) : Components.CastTowers(module, (uint)AID.MagitekMissiles, 6f);
class CircleOfDeath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CircleOfDeath, 8.24f);
class TwoTonzeMagitekMissile(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.Stackmarker, (uint)AID.TwoTonzeMagitekMissile, 6f, 5.1f, 4, 4);
class ChainMine(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(40f, 2f, 10f);
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.HexadroneBits)
            _aoes.Add(new(rect, source.Position.Quantized(), source.Rotation, World.FutureTime(5.6d)));
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.HexadroneBits)
        {
            var count = _aoes.Count;
            var pos = source.Position;
            for (var i = 0; i < count; ++i)
            {
                if (_aoes[i].Origin.AlmostEqual(pos, 1f))
                {
                    _aoes.RemoveAt(i);
                    return;
                }
            }
        }
    }
}

class D042MagitekHexadroneStates : StateMachineBuilder
{
    public D042MagitekHexadroneStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CircleOfDeath>()
            .ActivateOnEnter<TwoTonzeMagitekMissile>()
            .ActivateOnEnter<ChainMine>()
            .ActivateOnEnter<MagitekMissile>();
    }
}

[ModuleInfo(CFCID = 241u, NameID = 6203u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class D042MagitekHexadrone(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-240f, 130.5f), new ArenaBoundsSquare(19.5f));
