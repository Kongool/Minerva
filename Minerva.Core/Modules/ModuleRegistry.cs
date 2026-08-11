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
    public static ModuleRegistry Build(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = [typeof(ModuleRegistry).Assembly];

        var reg = new ModuleRegistry();
        foreach (var asm in assemblies)
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(ModuleBase)))
                    continue;
                var attr = type.GetCustomAttribute<ModuleInfoAttribute>();
                if (attr == null)
                    continue;

                var primaryOID = attr.PrimaryActorOID != 0 ? attr.PrimaryActorOID : InferBossOID(type);
                var info = new Info(type, attr, primaryOID);
                if (!reg.byCFC.TryGetValue(attr.CFCID, out var list))
                    reg.byCFC[attr.CFCID] = list = [];
                list.Add(info);
                reg.Count++;
            }
        return reg;
    }

    /// <summary>Modules registered for a duty, or empty.</summary>
    public IReadOnlyList<Info> ForCFC(uint cfcID) => this.byCFC.TryGetValue(cfcID, out var list) ? list : [];

    // convention: a sibling `OID` enum with a `Boss` member names the primary actor
    private static uint InferBossOID(Type moduleType)
    {
        var oidType = moduleType.Assembly.GetType(moduleType.Namespace + ".OID");
        if (oidType != null && oidType.IsEnum && Enum.TryParse(oidType, "Boss", out var boss))
            return Convert.ToUInt32(boss);
        return 0;
    }
}
