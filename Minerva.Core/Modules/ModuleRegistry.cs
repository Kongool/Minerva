using System.Linq;
using System.Reflection;

namespace Minerva;

/// <summary>
/// Discovers every <see cref="ModuleBase"/> subclass tagged with <see cref="ModuleInfoAttribute"/>
/// via reflection and indexes it by duty (CFC id). No central list to maintain — a new boss file
/// with a <c>[ModuleInfo]</c> attribute is picked up automatically. The manager queries this to
/// decide which module to run for the current duty + boss.
/// </summary>
public sealed class ModuleRegistry
{
    public sealed class Info(Type moduleType, ModuleInfoAttribute attr, uint primaryOID)
    {
        public readonly Type ModuleType = moduleType;
        public readonly ModuleInfoAttribute Attr = attr;
        public readonly uint PrimaryActorOID = primaryOID;

        public ModuleBase Create(WorldState ws, Actor primary)
        {
            var m = (ModuleBase)Activator.CreateInstance(this.ModuleType, ws, primary)!;
            m.BuildStates();
            return m;
        }
    }

    // CFC id -> modules registered for that duty
    private readonly Dictionary<uint, List<Info>> byCFC = [];
    public IReadOnlyDictionary<uint, List<Info>> ByCFC => this.byCFC;
    public int Count { get; private set; }

    /// <summary>Build a registry from the given assemblies (defaults to the one defining modules).</summary>
    public static ModuleRegistry Build(params Assembly[] assemblies) => Build(null, assemblies);

    /// <summary>
    /// Build the registry, resolving quest solo duties to the duty they run in.
    /// <para>A <see cref="ModuleGroup.Quest"/> module is authored with the <b>quest</b> id (BossmodReborn's
    /// convention, and what the game shows in its journal), but activation looks modules up by the
    /// ContentFinderCondition the game reports while the duty runs -- a different number nobody writes by
    /// hand. <paramref name="questDutyCFC"/> is that translation; the plugin supplies it from game data
    /// (ContentFinderCondition rows of link type 5 point at a QuestBattle row, which names the quest). Without
    /// it a quest module stays keyed on the quest id and never activates, which is how every MSQ port
    /// behaved before this.</para>
    /// </summary>
    public static ModuleRegistry Build(Func<uint, uint>? questDutyCFC, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = [typeof(ModuleRegistry).Assembly];

        var reg = new ModuleRegistry();
        foreach (var asm in assemblies)
            foreach (var type in LoadableTypes(asm))
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(ModuleBase)))
                    continue;
                var attr = type.GetCustomAttribute<ModuleInfoAttribute>();
                if (attr == null)
                    continue;

                var primaryOID = attr.PrimaryActorOID != 0 ? attr.PrimaryActorOID : InferBossOID(type);
                var info = new Info(type, attr, primaryOID);
                var key = attr.CFCID;
                if (attr.Group == ModuleGroup.Quest && questDutyCFC != null)
                {
                    // the quest id lives in GroupID, or in CFCID where older ports put it
                    var quest = attr.GroupID != 0 ? attr.GroupID : attr.CFCID;
                    var duty = questDutyCFC(quest);
                    if (duty != 0)
                        key = duty;
                }
                if (!reg.byCFC.TryGetValue(key, out var list))
                    reg.byCFC[key] = list = [];
                list.Add(info);
                reg.Count++;
            }
        return reg;
    }

    /// <summary>
    /// Types that actually loaded. The plugin assembly also contains Dalamud-dependent types (the entry
    /// point, services, windows); outside the game those fail to load and <c>GetTypes()</c> throws. Boss
    /// modules only reference Minerva.Core, so they load fine — keeping the survivors lets the registry be
    /// built headlessly (e.g. by the offline replay validator).
    /// </summary>
    // internal rather than private: ZoneModuleRegistry scans the same assemblies and needs the
    // same partial-load tolerance, and duplicating the reflection guard would let the two drift.
    internal static IEnumerable<Type> LoadableTypes(Assembly asm)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }

    /// <summary>Modules registered for a duty, or empty.</summary>
    public IReadOnlyList<Info> ForCFC(uint cfcID) => this.byCFC.TryGetValue(cfcID, out var list) ? list : [];

    /// <summary>
    /// Modules whose state machine will never bind, so they activate with no components at all.
    /// <para>The binding is by name — <c>&lt;ModuleClass&gt;States</c>. BossmodReborn names its states class
    /// after the file and binds it explicitly, so a module class <c>DemiMedusa</c> can sit in
    /// <c>ScaleModel.cs</c> next to <c>ScaleModelStates</c>; the port drops the explicit binding and the
    /// convention then finds nothing. The module still registers and still activates. It simply draws
    /// nothing, which reads as a fight nobody has covered rather than as a broken module.</para>
    /// </summary>
    public List<string> UnboundStates()
    {
        var missing = new List<string>();
        foreach (var (_, list) in this.byCFC)
            foreach (var info in list)
            {
                var t = info.ModuleType;
                if (t.Assembly.GetType(t.FullName + "States") == null)
                    missing.Add(t.Name);
            }

        missing.Sort();
        return missing;
    }

    /// <summary>
    /// Bosses claimed by more than one module, as (CFC, boss OID, module names).
    /// <para>Two modules for one boss is never intentional and never visible: the registry silently picks
    /// whichever it reaches first, so the fight gets one module's mechanics and the other's arena is simply
    /// never seen. It happens when the same fight is added twice under different names — BossmodReborn
    /// names Occult Crescent modules after the FATE and Minerva named a hand-written one after the boss,
    /// which is enough for a search by either name to miss the other.</para>
    /// </summary>
    public List<(uint CFCID, uint BossOID, List<string> Modules)> Collisions()
    {
        var seen = new Dictionary<(uint, uint), List<string>>();
        foreach (var (cfc, list) in this.byCFC)
            foreach (var info in list)
            {
                var key = (cfc, info.PrimaryActorOID);
                if (!seen.TryGetValue(key, out var names))
                    seen[key] = names = [];
                names.Add(info.ModuleType.Name);
            }

        var result = new List<(uint, uint, List<string>)>();
        foreach (var ((cfc, oid), names) in seen)
            if (names.Count > 1)
                result.Add((cfc, oid, names));
        return result;
    }

    /// <summary>
    /// Choose which module to activate when several of a duty's bosses are standing around.
    /// <para>Taking the first candidate whose boss is present is fine in a dungeon, where the bosses are in
    /// separate rooms. It is wrong in Occult Crescent, where every boss in the zone reports the same CFC
    /// and you can fight one within sight of another: a recording of a Machetaur pull activated Metamorph,
    /// which was idle nearby with 115 million HP and had cast nothing all fight.</para>
    /// <para>Ranked: the boss the player has targeted beats one that is merely fighting, which beats one
    /// that is merely there, and among equals the nearer wins. The player's target is the strongest signal
    /// available — "in combat" cannot tell your fight from someone else's, and in that same recording
    /// Metamorph was in combat from the first frame because another party was killing it.</para>
    /// <para>The fallback to a present-but-idle boss is deliberate: activating on arrival is what puts the
    /// radar up before the pull, and requiring combat would delay it to first damage.</para>
    /// </summary>
    public static (Info Info, Actor Boss)? PickActivation(IReadOnlyList<Info> candidates, IEnumerable<Actor> actors, WPos player, ulong playerTarget = 0)
    {
        (Info, Actor)? best = null;
        var bestScore = (Targeted: false, Engaged: false, DistSq: float.MaxValue);

        foreach (var actor in actors)
        {
            for (var i = 0; i < candidates.Count; ++i)
            {
                var info = candidates[i];
                if (actor.OID != info.PrimaryActorOID || actor.IsDestroyed)
                    continue;

                // don't immediately re-activate on the corpse we just finished
                if (info.Attr.PrimaryActorDeathEndsEncounter && actor.IsDead)
                    continue;

                var score = (Targeted: playerTarget != 0 && actor.InstanceID == playerTarget,
                    Engaged: actor.InCombat, DistSq: (actor.Position - player).LengthSq());
                if (best == null || Better(score, bestScore))
                {
                    best = (info, actor);
                    bestScore = score;
                }
            }
        }

        return best;

        static bool Better((bool Targeted, bool Engaged, float DistSq) a, (bool Targeted, bool Engaged, float DistSq) b)
            => a.Targeted != b.Targeted ? a.Targeted
             : a.Engaged != b.Engaged ? a.Engaged
             : a.DistSq < b.DistSq;
    }

    /// <summary>
    /// Which actor OID a module claims, when its <see cref="ModuleInfoAttribute.PrimaryActorOID"/> is unset.
    /// <para>Without this the module registers under OID 0, and every other module in the same duty that also
    /// inferred nothing collides with it — the registry activates whichever it reaches first and the rest
    /// silently never draw. Most of a three-boss dungeon can go dark this way.</para>
    /// <para>Three conventions, in order of how much they can be trusted. Hand-written Minerva modules name
    /// the member <c>Boss</c>. Ported BossmodReborn modules name it after the boss instead, which is why a
    /// name match against the module's own type name comes next. Failing both, BMR declares the boss first in
    /// the enum — measured across the ported tree, the first member agrees with an explicit <c>Boss</c> in
    /// 450 of 452 cases, and both exceptions declare <c>Boss</c> so they never reach this fallback. Note that
    /// <c>Enum.GetNames</c> orders by value, not declaration, so the fallback has to read the fields.</para>
    /// </summary>
    private static uint InferBossOID(Type moduleType)
    {
        var oidType = moduleType.Assembly.GetType(moduleType.Namespace + ".OID");
        if (oidType == null || !oidType.IsEnum)
            return 0;

        if (Enum.TryParse(oidType, "Boss", out var boss))
            return Convert.ToUInt32(boss);

        var fields = oidType.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var f in fields)
            if (moduleType.Name.EndsWith(f.Name, StringComparison.OrdinalIgnoreCase))
                return Convert.ToUInt32(f.GetRawConstantValue());

        foreach (var f in fields)
        {
            var v = Convert.ToUInt32(f.GetRawConstantValue());
            if (v != 0)
                return v;
        }
        return 0;
    }
}
