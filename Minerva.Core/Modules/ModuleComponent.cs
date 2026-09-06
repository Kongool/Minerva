namespace Minerva;

/// <summary>
/// A self-contained piece of encounter logic (one mechanic, usually). Components are activated and
/// deactivated by the state machine, observe world events, contribute text hints, and draw on the
/// radar. This is the unit modules are assembled from — most are one-liner subclasses of a
/// framework component like <c>SimpleAOEs</c>.
/// </summary>
public abstract class ModuleComponent(ModuleBase module)
{
    /// <summary>
    /// Suggested movements: from, to, and the colour to draw the arrow in.
    /// <para>A component that knows where you ought to be standing can say so as a line rather than as a
    /// zone — "walk from here to there" — which is the right shape for a tower to soak or a spot to take,
    /// where the destination matters and the ground in between does not.</para>
    /// <para>Minerva collects these and does not yet draw them; the radar has no arrow primitive. They are
    /// carried because ported components fill them constantly and the alternative is deleting the call
    /// from each one.</para>
    /// </summary>
    public sealed class MovementHints : List<(WPos From, WPos To, uint Color)>
    {
        public void Add(WPos from, WPos to, uint color) => this.Add((from, to, color));
    }

    /// <summary>
    /// How much a given party member matters to the viewing player right now. Ported modules return it
    /// from their party-drawing overrides; Minerva does not colour the party by it yet, so it is carried
    /// for source compatibility rather than acted on.
    /// </summary>
    public enum PlayerPriority
    {
        Irrelevant,
        Normal,
        Interesting,
        Danger,
        Critical,
    }

    public readonly ModuleBase Module = module;

    /// <summary>
    /// How many times this component's mechanic has resolved. Lives here rather than on
    /// <see cref="Components.CastCounter"/> because ported state machines ask any component for it
    /// (<c>comp.NumCasts &gt; 1</c> is how a fight says "after the second one"), including components that
    /// track their mechanic through tethers or icons rather than casts. A component that never counts
    /// leaves it at 0.
    /// </summary>
    public int NumCasts;

    /// <summary>
    /// How a given party member should be drawn while this component is active, and optionally in what
    /// colour. Ported components override it to highlight whoever is carrying the mechanic.
    /// <para>Minerva does not colour the party by it yet — the radar draws party members uniformly — so the
    /// value is currently collected and not acted on. It exists because BossmodReborn components override
    /// it constantly, and without the base method every one of them fails to compile.</para>
    /// </summary>
    public virtual PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
        => PlayerPriority.Irrelevant;

    /// <summary>
    /// Survive a phase change instead of being torn down with the phase that activated it.
    /// <para>Components are deactivated on phase change because a phase can end at any moment; a few need
    /// to persist across one — a lingering voidzone, a debuff that outlives the mechanic that applied it.
    /// Minerva's phase machine does not tear components down today, so this is carried rather than acted
    /// on, and setting it is harmless either way.</para>
    /// </summary>
    public virtual bool KeepOnPhaseChange { get; set; }

    /// <summary>Where this component thinks the given party member should move. See <see cref="MovementHints"/>.</summary>
    public virtual void AddMovementHints(int slot, Actor actor, MovementHints movementHints)
    {
    }

    public WorldState World => this.Module.World;

    /// <summary>
    /// The arena's geometry, read and written through the module that owns it.
    ///
    /// <para>Deliberately not <c>Arena.Bounds</c>. In BossmodReborn the arena object <i>is</i> the module's
    /// geometry; in Minerva <see cref="Arena"/> is the renderer's drawing surface, which is null until the
    /// radar draws a frame and is re-seeded from the module every frame after. A component assigning
    /// <c>Arena.Bounds</c> therefore had two faults at once: it threw whenever a mid-fight arena change
    /// arrived on a frame the radar was not drawing, and even when it did not throw the change was silently
    /// overwritten on the next draw.</para>
    /// </summary>
    public ArenaBounds Bounds
    {
        get => this.Module.Bounds;
        set => this.Module.Bounds = value;
    }

    /// <summary>The arena's centre — see <see cref="Bounds"/> for why this does not go through the renderer.</summary>
    public WPos Center
    {
        get => this.Module.Center;
        set => this.Module.Center = value;
    }

    public Arena Arena => this.Module.Arena;

    // --- bounds queries ---
    // Pure geometry over Bounds+Center; Arena exposes the same three, but reaching them through the
    // renderer means a null deref whenever the radar is not drawing. CheckPull, AddHints and AddAIHints
    // all run on the game tick, so that is the common case, not the edge case.

    /// <summary>Is <paramref name="p"/> inside the arena floor?</summary>
    public bool InBounds(WPos p) => this.Bounds.Contains(this.Center, p);

    /// <summary>Distance from <paramref name="origin"/> along <paramref name="dir"/> to the arena edge.</summary>
    public float IntersectRayBounds(WPos origin, WDir dir) => this.Bounds.IntersectRay(this.Center, origin, dir);

    /// <summary>The nearest in-bounds point to <paramref name="position"/> (itself, if already inside).</summary>
    public WPos ClampToBounds(WPos position)
    {
        if (this.InBounds(position))
            return position;
        var dir = position - this.Center;
        var dist = dir.Length();
        if (dist < 1e-6f)
            return position;
        dir /= dist;
        return this.Center + dir * MathF.Min(dist, this.IntersectRayBounds(this.Center, dir));
    }


    /// <summary>BossmodReborn's name for <see cref="Arena"/>, used in its drawing code.</summary>
    public Arena MiniArena => this.Arena;

    /// <summary>The player's party — matches BMR's <c>Raid</c> accessor so ported components using
    /// <c>Raid.WithSlot()</c> / <c>Raid.Player()</c> / <c>Raid.FindSlot()</c> compile unchanged.</summary>
    public PartyState Raid => this.Module.World.Party;

    /// <summary>This component's state does not make sense — see <see cref="ModuleBase.ReportError"/>.</summary>
    public void ReportError(string message) => this.Module.ReportError(this, message);

    /// <summary>
    /// The party ordered by how likely each member is to be highest on <paramref name="primaryTarget"/>'s
    /// enmity, with their slot indices. Mechanics that hit "the second on the list" (an off-tank buster,
    /// a cleave behind the tank) resolve through this.
    ///
    /// <para><b>This is a guess, and says so.</b> Minerva does not mirror the game's enmity table, so the
    /// order is: whoever the boss is currently targeting first, then tanks, then damage dealers, then
    /// healers, ties broken by party slot. BossmodReborn falls back to exactly this ordering when it cannot
    /// read the table either. It is right in the ordinary case — the tank holding the boss is first, the
    /// off-tank second — and wrong during a swap, which is the moment a buster is most likely to be in the
    /// air. Treat what it returns as advice, not as fact.</para>
    /// </summary>
    public List<(int slot, Actor actor)> RaidWithSlotByEnmity(Actor primaryTarget, bool allowGuessing = true)
    {
        if (!allowGuessing)
            return [];

        var withSlot = this.Raid.WithSlot();
        var ranked = new List<(int Key, int Slot, Actor Actor)>(withSlot.Length);
        foreach (var (slot, actor) in withSlot)
        {
            var key = primaryTarget.TargetID == actor.InstanceID
                ? 0
                : actor.Role switch
                {
                    Role.Tank => 1,
                    Role.Melee or Role.Ranged => 2,
                    Role.Healer => 3,
                    _ => 4,
                };
            ranked.Add((key, slot, actor));
        }
        ranked.Sort(static (a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Slot.CompareTo(b.Slot));

        var result = new List<(int, Actor)>(ranked.Count);
        foreach (var r in ranked)
            result.Add((r.Slot, r.Actor));
        return result;
    }

    /// <summary>As <see cref="RaidWithSlotByEnmity"/>, without the slot indices.</summary>
    public List<Actor> RaidByEnmity(Actor primaryTarget, bool allowGuessing = true)
    {
        var withSlot = this.RaidWithSlotByEnmity(primaryTarget, allowGuessing);
        var result = new List<Actor>(withSlot.Count);
        foreach (var (_, actor) in withSlot)
            result.Add(actor);
        return result;
    }

    /// <summary>Per-actor advice lines; <c>risk</c> marks danger (rendered prominently).</summary>
    public sealed class TextHints : List<(string text, bool risk)>
    {
        /// <summary>
        /// Add a line, unless the same line is already there.
        /// <para>Each component warns independently, so four overlapping AOEs meant four identical
        /// "GTFO from AOE!" lines stacked above the arena — the same information four times, crowding out
        /// whatever else the fight was trying to say. Repetition carries nothing here: two AOEs on you is
        /// not twice the instruction, it is the same instruction.</para>
        /// <para>Only the text is compared, so a line that appears as both a warning and a note collapses
        /// to whichever arrived first — which is the right way round, since the first is the more urgent
        /// in practice.</para>
        /// </summary>
        public void Add(string text, bool risk = true)
        {
            for (var i = 0; i < this.Count; ++i)
                if (this[i].text == text)
                    return;
            this.Add((text, risk));
        }
    }

    /// <summary>Raid-wide advice lines.</summary>
    public sealed class GlobalHints : List<string>;

    public virtual void Update() { }
    public virtual void AddHints(int slot, Actor actor, TextHints hints) { }
    public virtual void AddGlobalHints(GlobalHints hints) { }

    /// <summary>Contribute danger zones for the auto-dodge engine (see <see cref="AIHints"/>). The
    /// <paramref name="assignment"/> role slot (matching BMR) lets role-based positioning port unchanged.</summary>
    public virtual void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) { }

    /// <summary>Draw danger zones / fills (called before the boundary).</summary>
    public virtual void DrawArenaBackground(int pcSlot, Actor pc) { }

    /// <summary>Draw actors / markers / tethers (called after the boundary).</summary>
    public virtual void DrawArenaForeground(int pcSlot, Actor pc) { }

    // world event handlers (dispatched by the module)
    public virtual void OnActorCreated(Actor actor) { }
    public virtual void OnActorDestroyed(Actor actor) { }
    public virtual void OnActorDeath(Actor actor) { }
    public virtual void OnCastStarted(Actor caster, ActorCastInfo cast) { }
    public virtual void OnCastFinished(Actor caster, ActorCastInfo cast) { }
    public virtual void OnEventCast(Actor caster, ActorCastEvent cast) { }
    // note: matches BMR's signatures so ported modules compile unchanged. On a status change the
    // struct is passed by ref; on lose it still carries the lost status's details (see OpStatus).
    public virtual void OnStatusGain(Actor actor, ref ActorStatus status) { }
    public virtual void OnStatusLose(Actor actor, ref ActorStatus status) { }
    public virtual void OnTethered(Actor source, in ActorTetherInfo tether) { }
    public virtual void OnUntethered(Actor source, in ActorTetherInfo tether) { }
    public virtual void OnMapEffect(byte index, uint state) { }
    public virtual void OnEventIcon(Actor actor, uint iconID, ulong targetID) { }

    // Additional BMR event hooks, declared so ported components that override them compile. Minerva's
    // world sync does not raise all of these yet, so overrides may not fire until the sync is extended
    // (tracked in the BMR-porting notes); they are safe no-ops until then.
    public virtual void OnActorTargetable(Actor actor) { }
    public virtual void OnActorUntargetable(Actor actor) { }
    public virtual void OnActorRenderflagsChange(Actor actor, int renderflags) { }
    public virtual void OnEventVFX(Actor actor, uint vfxID, ulong targetID) { }
    public virtual void OnActorEState(Actor actor, ushort state) { }
    public virtual void OnActorEAnim(Actor actor, uint state) { }
    public virtual void OnActorPlayActionTimelineEvent(Actor actor, ushort id) { }

    /// <summary>
    /// A batch of action-timeline events that arrived together on one frame.
    ///
    /// <para>The batched form matters where the ORDER of simultaneous events is the mechanic — several
    /// actors starting the same animation on the same tick, and which came first decides who is first in
    /// the sequence. <see cref="OnActorPlayActionTimelineEvent"/> delivers them one at a time and loses
    /// that. Minerva's sync does not raise this yet, so an override is a safe no-op until it does.</para>
    /// </summary>
    public virtual void OnActorPlayActionTimelineSync(Actor actor, List<(ulong InstanceID, ushort ID)> events) { }
    public virtual void OnActorNpcYell(Actor actor, ushort id) { }
    public virtual void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2) { }
    public virtual void OnActorEventStateChange(Actor actor, byte value) { }
    public virtual void OnLegacyMapEffect(byte seq, byte param, byte[] data) { }
    public virtual void OnEventDirectorUpdate(uint updateID, uint param1, uint param2, uint param3, uint param4) { }
}
