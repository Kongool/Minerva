namespace Minerva.Components;

/// <summary>
/// Persistent ground hazard: while any actor of the given OIDs exists, a circle of radius
/// <c>Radius</c> around it is dangerous. Draws the puddle and contributes it to the auto-dodge
/// engine as a standing forbidden zone (already active — activation is "now").
/// </summary>
public class Voidzone(ModuleBase module, float radius, uint[] oids) : ModuleComponent(module)
{
    public readonly float Radius = radius;
    public readonly uint[] OIDs = oids;
    private readonly AOEShapeCircle shape = new(radius);

    public Voidzone(ModuleBase module, float radius, uint oid) : this(module, radius, [oid]) { }

    private IEnumerable<Actor> Sources()
    {
        foreach (var a in this.World.Actors)
            if (!a.IsDeadOrDestroyed && Array.IndexOf(this.OIDs, a.OID) >= 0)
                yield return a;
    }

    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        foreach (var a in this.Sources())
            this.Arena.ZoneShape(this.shape, a.Position, default, Colors.AOE);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var a in this.Sources())
        {
            if (actor.Position.InCircle(a.Position, this.Radius))
            {
                hints.Add("Leave the voidzone!");
                return;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, AIHints hints)
    {
        foreach (var a in this.Sources())
            hints.AddForbiddenZone(this.shape, a.Position, default, this.World.CurrentTime);
    }
}
