// Occult Crescent — North Horn. Boss 'Ruin Hound' 0x4D5E (verified from recording: single instance, 10.7M HP,
// a lighter ~52s NM; NameID 14762). Ice/frost themed; 'Ice Pillar' 0x4D5F is the mechanic actor that rushes.
// Duty CFC 1093 — coexists with the other North Horn modules. No BossmodReborn reference; recording-only. WIP.
//
// Cleanup / needs validation:
//   - TheStormWithout was a zero-width Rect(40,0) (draws nothing). Paired with TheStormWithin (a circle), it's
//     almost certainly the DONUT half of an in/out — demoted to CastHint pending inner/outer radii.
//   - Dropped both auto-voidzones: Crescent Anila is ambient wildlife; 0x4D60 'Ruin Hound' is an add (141K HP),
//     not a puddle. (0x4DA0 is a 30-instance Ruin Hound swarm.)
//   - RoaringBlizzard / AgeOfEndlessFrost2 cones and AgeOfEndlessFrost / IcePillar2 shapes need confirming.
//   - Arena is a guessed 16y square at (-90.3, 865.1); refine.
using System;
using Minerva;

namespace Minerva.Dawntrail.Foray.RuinHound;

public enum OID : uint
{
    Boss = 0x4D5E,          // 'Ruin Hound' R6.75 — the boss (single instance, 10.7M HP; verified)
    IcePillar = 0x4D5F,     // 'Ice Pillar' R2.0 — mechanic actor (casts Rush / Ice Pillar)
    RuinHoundSwarm = 0x4DA0, // 'Ruin Hound' R1.0 — 30-instance add swarm
    RuinHoundAdd = 0x4D60,  // 'Ruin Hound' R1.0 — add
    CrescentElftoad = 0x4E1E, // 'Crescent Elftoad' R4.08 — ambient add
    // ambient Crescent Anila wildlife captured by the open-field recording dropped here.
}

public enum AID : uint
{
    Rush = 49759, // Ice Pillar->Self, 3.7s, x10
    IcePillar = 49770, // Ice Pillar->Self, 2.7s, x4
    RoaringBlizzard = 49765, // Ruin Hound->Self, 4.7s, x2
    AgeOfEndlessFrost = 49760, // Ruin Hound->Self, 2.7s, x1
    AgeOfEndlessFrost2 = 49761, // Ruin Hound->Self, 2.7s, x30
    TheStormWithin = 49756, // Ruin Hound->Self, 4.7s, x1
    TheStormWithout = 49757, // Ruin Hound->Self, 4.7s, x1
    IcePillar2 = 49758, // Ruin Hound->Self, 2.7s, x1
}

// --- likely-correct / reasonable-guess AOEs (verify against the recording) ---
sealed class Rush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rush, new AOEShapeRect(80f, 2f));
sealed class IcePillar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IcePillar, new AOEShapeCircle(4f));
sealed class TheStormWithin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheStormWithin, new AOEShapeCircle(10f));
sealed class RoaringBlizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RoaringBlizzard, new AOEShapeCone(50f, 45f.Degrees())); // TODO: confirm shape
sealed class AgeOfEndlessFrost2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AgeOfEndlessFrost2, new AOEShapeCone(40f, 45f.Degrees())); // TODO: confirm shape

// --- shape unknown / bugged in the draft: hint only until the recording shows the real shape ---
sealed class TheStormWithout(ModuleBase module) : Components.CastHint(module, (uint)AID.TheStormWithout, "The Storm Without (donut? radii TBD)"); // was Rect(40,0)
sealed class AgeOfEndlessFrost(ModuleBase module) : Components.CastHint(module, (uint)AID.AgeOfEndlessFrost, "Age of Endless Frost (shape TBD)");
sealed class IcePillar2(ModuleBase module) : Components.CastHint(module, (uint)AID.IcePillar2, "Ice Pillar (shape TBD)");

sealed class RuinHoundStates : StateMachineBuilder
{
    public RuinHoundStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<IcePillar>()
            .ActivateOnEnter<RoaringBlizzard>()
            .ActivateOnEnter<AgeOfEndlessFrost>()
            .ActivateOnEnter<AgeOfEndlessFrost2>()
            .ActivateOnEnter<TheStormWithin>()
            .ActivateOnEnter<TheStormWithout>()
            .ActivateOnEnter<IcePillar2>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 14762u, Maturity = ModuleMaturity.WIP, Contributors = "Minerva extractor (North Horn; no BMR reference)")]
public sealed class RuinHound(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-90.3f, 865.1f), new ArenaBoundsSquare(16f)); // arena guessed — refine
