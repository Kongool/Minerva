namespace Minerva;

/// <summary>
/// The shared arena for treasure hunt roulettes: a 19 yalm circle at (100, 100).
/// <para>Every treasure map dungeon uses the same room, so BossmodReborn declares the arena once and its
/// forty-five modules inherit it rather than each restating the same polygon. Ported unchanged for the
/// same reason — without it those forty-five do not compile, and the alternative is forty-five copies of
/// one constant.</para>
/// </summary>
public abstract class THTemplate(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, ArenaCenter, new ArenaBoundsPolygon(BuildContour()))
{
    public static readonly WPos ArenaCenter = new(100f, 100f);

    private const float Radius = 19f;
    private const int Edges = 48;

    private static WDir[] BuildContour()
    {
        var pts = new WDir[Edges];
        for (var i = 0; i < Edges; ++i)
        {
            var a = i * 2f * MathF.PI / Edges;
            pts[i] = new WDir(MathF.Sin(a) * Radius, MathF.Cos(a) * Radius);
        }

        return pts;
    }
}
