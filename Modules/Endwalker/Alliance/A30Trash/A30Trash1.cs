// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A30Trash1;

public enum OID : uint
{
    Serpent = 0x4010, // R3.45
    Triton = 0x4011, // R1.95
    DivineSprite = 0x4012, // R1.6
    WaterSprite = 0x4085 // R0.8
}

public enum AID : uint
{
    AutoAttack = 870, // Serpent/Triton->player, no cast, single-target

    WaterIII = 35438, // Serpent->location, 4.0s cast, range 8 circle
    PelagicCleaver1 = 35439, // Triton->self, 5.0s cast, range 40 60-degree cone
    PelagicCleaver2 = 35852, // Triton->self, 5.0s cast, range 40 60-degree cone
    WaterAutoAttack = 35469, // WaterSprite/DivineSprite->player, no cast, single-target, auto attack
    WaterFlood = 35442, // WaterSprite->self, 3.0s cast, range 6 circle
    WaterBurst = 35443, // WaterSprite->self, no cast, range 40 circle, raidwide when Water Sprite dies
    DivineFlood = 35440, // DivineSprite->self, 3.0s cast, range 6 circle
    DivineBurst = 35441 // DivineSprite->self, no cast, range 40 circle, raidwide when Divine Sprite dies
}

class WaterIII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WaterIII, 8f);

class PelagicCleaver(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.PelagicCleaver1, (uint)AID.PelagicCleaver2], new AOEShapeCone(40f, 30f.Degrees()));
class PelagicCleaver1Hint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.PelagicCleaver1);
class PelagicCleaver2Hint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.PelagicCleaver2);

class WaterDivineFlood(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.WaterFlood, (uint)AID.DivineFlood], 6f);

public class A30Trash1States : StateMachineBuilder
{
    public A30Trash1States(A30Trash1 module) : base(module)
    {
        // as soon as the last serpent dies, other adds are spawned; serpents are destroyed a bit later
        TrivialPhase()
            .ActivateOnEnter<WaterIII>()
            .Raw.Update = () =>
            {
                var enemies = module.Enemies((uint)OID.Serpent);
                var count = enemies.Count;
                for (var i = 0; i < count; ++i)
                {
                    if (!enemies[i].IsDeadOrDestroyed)
                        return false;
                }
                return true;
            };
        TrivialPhase(1u)
            .ActivateOnEnter<PelagicCleaver>()
            .ActivateOnEnter<PelagicCleaver1Hint>()
            .ActivateOnEnter<PelagicCleaver2Hint>()
            .ActivateOnEnter<WaterDivineFlood>()
            .Raw.Update = () => AllDeadOrDestroyed(A30Trash1.Trash) && module.Enemies((uint)OID.Serpent).Count == 0;
    }
}

[ModuleInfo(CFCID = 962u, NameID = 12478u, PrimaryActorOID = (uint)OID.Serpent, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, LTS (ported from BMR)")]
public class A30Trash1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-800f, -800f), new ArenaBoundsCircle(20f))
{
    public static readonly uint[] Trash = [(uint)OID.Triton, (uint)OID.DivineSprite, (uint)OID.WaterSprite];

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Serpent));
        Arena.Actors(this, Trash);
    }
}
