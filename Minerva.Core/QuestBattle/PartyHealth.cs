namespace Minerva.QuestBattle;

public static class QuestAllies
{
    /// <summary>
    /// Everyone on our side a kit may heal: the party, then the duty's allied NPCs -- the refugees in As the Heavens
    /// Burn, Nashmeira in Gamboling for Gil. BossmodReborn keeps those in party slots past the alliance; Minerva's
    /// party is the player's own eight (<see cref="PartyState.WithoutSlot"/>), so they are found among the actors.
    /// </summary>
    public static List<Actor> PartyAndAllies(this WorldState ws, bool includeDead = false)
    {
        var result = new List<Actor>(ws.Party.WithoutSlot(includeDead));
        foreach (var a in ws.Actors)
        {
            if (!a.IsAlly || a.Type is ActorType.Player or ActorType.Pet or ActorType.Chocobo || a.IsDestroyed || !includeDead && a.IsDead
                || a.HPMP.MaxHP == 0 || result.Contains(a))
                continue;
            result.Add(a);
        }
        return result;
    }
}

/// <summary>
/// Who on our side most needs a heal, for the healer kits a quest battle hands you (Alphinaud in As the Heavens Burn).
/// Ported from BossmodReborn's <c>TrackPartyHealth</c> (BSD-3; see THIRD-PARTY-NOTICES.txt), reduced to what the kits
/// read: the lowest member, whether an area heal pays, and an estimate of HP to come. That estimate is HP now, less a
/// little per enemy attacking the member and 30% for a predicted hit -- Minerva tracks no incoming heals, so it errs
/// toward healing.
/// </summary>
public sealed class TrackPartyHealth(WorldState world)
{
    public record struct PartyMemberState(Actor Actor, float CurrentHPRatio, float PredictedHPRatio, float AttackerStrength);

    public sealed record class PartyHealthState(int LowestCurrent, int LowestPredicted, int Count, float AvgCurrent, float StdDevCurrent, float AvgPredicted, float StdDevPredicted);

    /// <summary>Spread of HP ratios above which one member is worth a single-target heal over an area heal.</summary>
    public const float AOEBreakpointHPVariance = 0.25f;

    public readonly List<PartyMemberState> Members = [];
    public PartyHealthState PartyHealth = new(-1, -1, 0, 1f, 0f, 1f, 0f);

    public void Update(AIHints hints)
    {
        this.Members.Clear();
        foreach (var a in world.PartyAndAllies())
            this.Members.Add(new PartyMemberState(a, a.HPRatio, a.HPRatio, 0f));

        for (var i = 0; i < this.Members.Count; ++i)
        {
            var m = this.Members[i];
            foreach (var e in hints.PotentialTargets)
                if (e.Actor.TargetID == m.Actor.InstanceID)
                    m.AttackerStrength += e.AttackStrength;
            if (m.PredictedHPRatio < 0.99f)
                m.PredictedHPRatio -= m.AttackerStrength;
            var slot = world.Party.FindSlot(m.Actor.InstanceID);
            if (slot >= 0)
                foreach (var predicted in hints.PredictedDamage)
                    if (predicted.players[slot])
                        m.PredictedHPRatio -= 0.30f;
            this.Members[i] = m;
        }

        this.PartyHealth = this.Calculate(static _ => true);
    }

    public (Actor Target, PartyMemberState State)? BestSTHealTarget
        => this.PartyHealth.LowestCurrent >= 0 && (this.PartyHealth.StdDevCurrent > AOEBreakpointHPVariance || this.PartyHealth.Count == 1)
            ? (this.Members[this.PartyHealth.LowestCurrent].Actor, this.Members[this.PartyHealth.LowestCurrent]) : null;

    public (Actor Target, PartyMemberState State)? BestSTHealTargetPredicted
        => this.PartyHealth.LowestPredicted >= 0 && (this.PartyHealth.StdDevPredicted > AOEBreakpointHPVariance || this.PartyHealth.Count == 1)
            ? (this.Members[this.PartyHealth.LowestPredicted].Actor, this.Members[this.PartyHealth.LowestPredicted]) : null;

    public bool ShouldHealInArea(WPos center, float radius, float hpThreshold)
    {
        var st = this.Calculate(a => a.Position.InCircle(center, radius));
        return st.Count > 1 && st.StdDevCurrent <= AOEBreakpointHPVariance && st.AvgCurrent <= hpThreshold;
    }

    public bool PredictShouldHealInArea(WPos center, float radius, float hpThreshold)
    {
        var st = this.Calculate(a => a.Position.InCircle(center, radius));
        return st.Count > 1 && st.StdDevPredicted <= AOEBreakpointHPVariance && st.AvgPredicted <= hpThreshold;
    }

    private PartyHealthState Calculate(Func<Actor, bool> filter)
    {
        int count = 0, lowCur = -1, lowPred = -1;
        float minCur = float.MaxValue, minPred = float.MaxValue, meanCur = 0, m2Cur = 0, meanPred = 0, m2Pred = 0;
        for (var i = 0; i < this.Members.Count; ++i)
        {
            var m = this.Members[i];
            if (m.Actor.IsDead || !m.Actor.IsTargetable || !filter(m.Actor))
                continue;
            ++count;
            if (m.CurrentHPRatio < minCur)
            {
                minCur = m.CurrentHPRatio;
                lowCur = i;
            }
            if (m.PredictedHPRatio < minPred)
            {
                minPred = m.PredictedHPRatio;
                lowPred = i;
            }
            var d = m.CurrentHPRatio - meanCur;
            meanCur += d / count;
            m2Cur += d * (m.CurrentHPRatio - meanCur);
            var dp = m.PredictedHPRatio - meanPred;
            meanPred += dp / count;
            m2Pred += dp * (m.PredictedHPRatio - meanPred);
        }
        return count == 0
            ? new(-1, -1, 0, 1f, 0f, 1f, 0f)
            : new(lowCur, lowPred, count, meanCur, MathF.Sqrt(m2Cur / count), meanPred, MathF.Sqrt(m2Pred / count));
    }
}
