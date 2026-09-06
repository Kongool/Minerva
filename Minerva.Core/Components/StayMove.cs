namespace Minerva.Components;

/// <summary>
/// Mechanics that require players to either hold still or keep moving (pyretic / freezing / forced-stop).
/// Subclasses set each player's <see cref="Requirement"/> from status or cast handlers; this base shows
/// the matching hint. Ported from BossmodReborn's StayMove (BSD-3; see THIRD-PARTY-NOTICES.txt); Minerva
/// omits BMR's special-mode AI (it has no movement-forcing auto-dodge modes).
/// </summary>
public class StayMove(ModuleBase module, double maxTimeToShowHint = 1e3d) : ModuleComponent(module)
{
    public enum Requirement { None, Stay, Stay2, Move }

    public readonly struct PlayerState(Requirement requirement, DateTime activation, int priority = 0, DateTime finish = default)
    {
        public readonly Requirement Requirement = requirement;
        public readonly DateTime Activation = activation;
        public readonly DateTime Finish = finish;
        public readonly int Priority = priority;
    }

    public readonly PlayerState[] PlayerStates = new PlayerState[PartyState.MaxSlots];
    public readonly double MaxTimeToShowHint = maxTimeToShowHint;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (slot < 0 || slot >= this.PlayerStates.Length)
            return;
        ref readonly var state = ref this.PlayerStates[slot];
        if (state.Requirement != Requirement.None && state.Activation <= this.World.FutureTime(this.MaxTimeToShowHint))
        {
            var moving = actor.LastFrameMovement != default;
            switch (state.Requirement)
            {
                case Requirement.Stay:
                    hints.Add("Stop everything!", moving || actor.CastInfo != null || actor.TargetID != default);
                    break;
                case Requirement.Stay2:
                    hints.Add("Don't move!", moving);
                    break;
                case Requirement.Move:
                    hints.Add("Move!", !moving);
                    break;
            }
        }
        if (actor.IsDead && state.Requirement != Requirement.None)
            this.PlayerStates[slot] = default;
    }

    /// <summary>
    /// Publish the requirement as a machine-readable special mode, so consumers other than the hint text
    /// can act on it — the auto-dodge (don't steer into a stand-still punisher) and, via the plugin's IPC,
    /// a rotation plugin that must stop casting while "stop everything" is up.
    /// </summary>
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (slot < 0 || slot >= this.PlayerStates.Length)
            return;
        ref readonly var state = ref this.PlayerStates[slot];
        var mode = state.Requirement switch
        {
            Requirement.Stay => AIHints.SpecialMode.Pyretic,    // any action OR movement punishes
            Requirement.Stay2 => AIHints.SpecialMode.NoMovement, // movement punishes, actions are fine
            Requirement.Move => AIHints.SpecialMode.Freezing,    // standing still punishes
            _ => AIHints.SpecialMode.Normal,
        };
        if (mode != AIHints.SpecialMode.Normal)
            hints.AddSpecialMode(mode, state.Activation, state.Finish);
    }

    // set a player's requirement, but only if the new priority is at least the current one
    protected void SetState(int slot, in PlayerState state)
    {
        if (slot >= 0 && slot < this.PlayerStates.Length && this.PlayerStates[slot].Priority <= state.Priority)
            this.PlayerStates[slot] = state;
    }

    protected void ClearState(int slot, int priority = int.MaxValue)
    {
        if (slot >= 0 && slot < this.PlayerStates.Length && this.PlayerStates[slot].Priority <= priority)
            this.PlayerStates[slot] = default;
    }
}
