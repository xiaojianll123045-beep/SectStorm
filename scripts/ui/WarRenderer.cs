using Godot;
using System.Linq;

public partial class WarRenderer : Node2D
{
    private GameManager _gm;

    public override void _Ready()
    {
        ZIndex = 60;
    }

    public override void _Process(double delta)
    {
        if (_gm == null) _gm = GetNodeOrNull<GameManager>("../GameManager");
        if (_gm == null) _gm = GetNodeOrNull<GameManager>("GameManager");
        if (_gm == null) return;

        foreach (var c in GetChildren())
            c.QueueFree();

        foreach (var war in _gm.Wars)
        {
            if (war.Ended) continue;
            var atkSect = _gm.State.GetSect(war.AttackerSectId);
            var defSect = _gm.State.GetSect(war.DefenderSectId);
            if (atkSect == null || defSect == null) continue;

            var atkHome = _gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == atkSect.Id);
            var defHome = _gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == defSect.Id);
            if (atkHome == null || defHome == null) continue;

            Vector2 a = atkHome.Position;
            Vector2 b = defHome.Position;
            Vector2 dir = (b - a).Normalized();
            float dist = (b - a).Length();

            int count = Mathf.Max(1, (int)(dist / 600f));
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                Vector2 pos = a + dir * dist * t;
                Vector2 perp = new Vector2(-dir.Y, dir.X) * 20f;

                var arrow = new Node2D();
                arrow.Position = pos + perp * ((i % 2 == 0) ? 1 : -1);
                arrow.Rotation = dir.Angle();
                AddChild(arrow);

                // shaft (white outline + red fill)
                DrawArrowShaft(arrow, 60f, 6f, 16f);
            }
        }
    }

    private void DrawArrowShaft(Node2D parent, float length, float thickness, float headSize)
    {
        // white outline (slightly larger)
        DrawSingleArrow(parent, length + 2, thickness + 2, headSize + 2, new Color(1, 1, 1, 0.9f));
        // red fill
        DrawSingleArrow(parent, length, thickness, headSize, new Color(0.9f, 0.1f, 0.1f, 0.85f));
    }

    private void DrawSingleArrow(Node2D parent, float len, float thick, float head, Color color)
    {
        // shaft as a thick ColorRect
        var shaft = new ColorRect();
        shaft.Size = new Vector2(len, thick);
        shaft.Color = color;
        shaft.Position = new Vector2(0, -thick / 2f);
        parent.AddChild(shaft);

        // arrowhead as polygon
        var headPoly = new Polygon2D();
        headPoly.Polygon = new Vector2[] {
            new Vector2(len, 0),
            new Vector2(len - head, -head / 2),
            new Vector2(len - head, head / 2),
        };
        headPoly.Color = color;
        parent.AddChild(headPoly);
    }
}
