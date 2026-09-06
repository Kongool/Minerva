// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class HellsGate(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> _targets = [];
    private readonly List<Actor> _ironChains = [];

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == 81 && source.Type == ActorType.Player)
            _targets.Add(source);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == 81 && source.Type == ActorType.Player)
        {
            _targets.Remove(source);
            _ironChains.RemoveAll(chain => chain.IsDeadOrDestroyed);
        }
    }

    public override void OnActorCreated(Actor actor)
    {
        if ((OID)actor.OID == OID.IronChain)
            _ironChains.Add(actor);
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (_targets.Count > 0)
        {
            _targets.Remove(actor);
            _ironChains.RemoveAll(chain => chain.IsDeadOrDestroyed);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_targets.Count > 0 && !_targets.Contains(actor))
            hints.Add("Kill the Iron Chain on bound players!");
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var chain in _ironChains)
        {
            Arena.ZoneCircleOutline(chain.Position, 1.5f, Colors.Danger, 1.5f);
            Arena.Actor(chain, Colors.Enemy);
        }
    }
}
