// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex8Seiryu;

sealed class RedRush(ModuleBase module) : Components.BaitAwayTethers(module, new AOEShapeRect(82.6f, 2.5f), (uint)TetherID.RedRush, (uint)AID.RedRush, activationDelay: 6d)
{
    private readonly BlueBolt _stack = module.FindComponent<BlueBolt>()!;

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID == (uint)OID.AkaNoShiki)
        {
            _stack.ForbiddenPlayers.Set(Raid.FindSlot(tether.Target));
        }
        base.OnTethered(source, tether);
    }
}

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

sealed class BlueBoltStretch(ModuleBase module) : Components.StretchTetherSingle(module, (uint)TetherID.BlueBolt, 25f, activationDelay: 5.9d);

sealed class RedRushKnockback(ModuleBase module) : Components.GenericKnockback(module)
{
    private readonly Knockback[][] _kbs = new Knockback[8][];

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kbs[slot] ?? [];

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID == (uint)OID.AkaNoShiki && Raid.FindSlot(tether.Target) is var slot)
        {
            _kbs[slot] = [new(source.Position.Quantized(), 18f, World.FutureTime(6d))];
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_kbs[slot] != null)
        {
            ref readonly var kb = ref _kbs[slot][0];
            var act = kb.Activation;
            if (!IsImmune(slot, act))
            {
                // circle intentionally slightly smaller to prevent sus knockback
                hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(Center, kb.Origin, 18f, 19f), act);
            }
        }
    }
}

sealed class Kanabo(ModuleBase module) : Components.TankbusterTether(module, (uint)AID.Kanabo, (uint)TetherID.Kanabo, new AOEShapeCone(45f, 30f.Degrees()), 6.2d)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        if (TetheredPlayers[slot])
        {
            hints.AddForbiddenZone(new SDCircle(Center, Bounds.Radius >= 20f ? 19f : 18.5f), activation);
        }
    }
}

sealed class YamaKagura(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.YamaKagura, new AOEShapeRect(60f, 3f));
sealed class HundredTonzeSwing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HundredTonzeSwing, 16f);
sealed class Stoneskin(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.Stoneskin, true, showNameInHint: true);
sealed class Adds(ModuleBase module) : Components.AddsMulti(module, [(uint)OID.NumaNoShiki, (uint)OID.DoroNoShiki])
{
    public bool Started;

    public override void OnActorTargetable(Actor actor)
    {
        if (actor.OID == (uint)OID.NumaNoShiki)
        {
            Started = true;
        }
    }
}

sealed class StrengthOfSpirit(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.StrengthOfSpirit); // not really a raidwide yet, but raidwide is after a cutscene, so we want to heal up before
