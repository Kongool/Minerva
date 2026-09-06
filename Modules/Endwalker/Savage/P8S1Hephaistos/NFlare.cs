// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S1Hephaistos;

// component dealing with tetra/octaflare mechanics (conceptual or not)
class TetraOctaFlareCommon(ModuleBase module) : Components.UniformStackSpread(module, 3, 6, 2, 2)
{
    public enum Concept { None, Tetra, Octa }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.EmergentOctaflare or AID.EmergentTetraflare)
        {
            Stacks.Clear();
            Spreads.Clear();
        }
    }

    protected void SetupMasks(Concept concept)
    {
        switch (concept)
        {
            case Concept.Tetra:
                // note that targets are either all dps or all tanks/healers, it seems to be unknown until actual cast, so for simplicity assume it will target tanks/healers (not that it matters much in practice)
                AddStacks(Raid.WithoutSlot(false, true, true).Where(a => a.Role is Role.Tank or Role.Healer));
                break;
            case Concept.Octa:
                AddSpreads(Raid.WithoutSlot(false, true, true));
                break;
        }
    }
}

class TetraOctaFlareImmediate(ModuleBase module) : TetraOctaFlareCommon(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.Octaflare:
                SetupMasks(Concept.Octa);
                break;
            case AID.Tetraflare:
                SetupMasks(Concept.Tetra);
                break;
        }
    }
}

class TetraOctaFlareConceptual(ModuleBase module) : TetraOctaFlareCommon(module)
{
    private Concept _concept;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_concept != Concept.None)
            hints.Add(_concept == Concept.Tetra ? "Prepare to stack in pairs" : "Prepare to spread");
    }

    public void Show()
    {
        SetupMasks(_concept);
        _concept = Concept.None;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.ConceptualOctaflare:
                _concept = Concept.Octa;
                break;
            case AID.ConceptualTetraflare:
                _concept = Concept.Tetra;
                break;
        }
    }
}
