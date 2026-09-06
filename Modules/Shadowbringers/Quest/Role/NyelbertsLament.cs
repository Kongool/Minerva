// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Quest.Role.NyelbertsLament;

public enum OID : uint
{
    Boss = 0x2977,

    BovianBull = 0x2976,
    LooseBoulder = 0x2978, // R2.4
    Helper = 0x233C
}

public enum AID : uint
{
    FallingRock = 16595, // Helper->location, 3.0s cast, range 4 circle
    ZoomTargetSelect = 16599, // Helper->player, no cast, single-target
    ZoomIn = 16598, // Helper->self, no cast, range 42 width 8 rect
    TwoThousandMinaSlash = 16601, // Bovian->self/player, 5.0s cast, range 40 ?-degree cone
}

public enum SID : uint
{
    WingedShield = 1900
}

class TwoThousandMinaSlash : Components.GenericLineOfSightAOE
{
    private readonly List<Actor> _casters = [];

    public TwoThousandMinaSlash(ModuleBase module) : base(module, (uint)AID.TwoThousandMinaSlash, 40, false)
    {
        Refresh();
    }

    public Actor? ActiveCaster => _casters.MinBy(c => c.CastInfo!.RemainingTime);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _casters.Add(caster);
            Refresh();
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _casters.Remove(caster);
            Refresh();
        }
    }

    private void Refresh()
    {
        var blockers = Module.Enemies((uint)OID.LooseBoulder);

        Modify(ActiveCaster?.CastInfo?.LocXZ, blockers.Select(b => (b.Position, b.HitboxRadius)), Module.CastFinishAt(ActiveCaster?.CastInfo));
        Safezones.Clear();
        AddSafezone(NextExplosion, default);
    }
}

class FallingRock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FallingRock, 4f);
class ZoomIn(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.ZoomTargetSelect, (uint)AID.ZoomIn, 5.1d, 42)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (ActiveBaits.Count != 0)
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 3f), ActiveBaits[0].Activation);
    }
}

class PassageOfArms(ModuleBase module) : ModuleComponent(module)
{
    private ActorCastInfo? EnrageCast => Module.PrimaryActor.CastInfo is { Action.ID: 16604 } castInfo ? castInfo : null;
    private Actor? Paladin => World.Actors.FirstOrDefault(x => x.FindStatus((uint)SID.WingedShield) != null);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (EnrageCast != null && Paladin != null)
            hints.AddForbiddenZone(new SDInvertedCone(Paladin.Position, 8f, Paladin.Rotation + 180f.Degrees(), 60f.Degrees()), Module.CastFinishAt(EnrageCast));
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (EnrageCast != null && Paladin != null)
            Arena.ZoneCone(Paladin.Position, 0f, 8f, Paladin.Rotation + 180f.Degrees(), 60f.Degrees(), Colors.SafeFromAOE);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (EnrageCast != null && Paladin != null && !actor.Position.InCircleCone(Paladin.Position, 8f, Paladin.Rotation + 180f.Degrees(), 60f.Degrees()))
            hints.Add("Hide behind tank!");
    }
}

class BovianStates : StateMachineBuilder
{
    public BovianStates(ModuleBase module) : base(module)
    {
        TrivialPhase()

            .ActivateOnEnter<FallingRock>()
            .ActivateOnEnter<ZoomIn>()
            .ActivateOnEnter<TwoThousandMinaSlash>()
            .ActivateOnEnter<PassageOfArms>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 69162u, NameID = 8363u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Bovian(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-440, -691), new ArenaBoundsCircle(20))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}
