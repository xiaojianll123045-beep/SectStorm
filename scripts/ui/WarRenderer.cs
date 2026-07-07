using Godot;
using System.Collections.Generic;
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

        List<WarData> warsSnapshot;
        lock (_gm.Wars) { warsSnapshot = _gm.Wars.Where(w => !w.Ended).ToList(); }
        foreach (var war in warsSnapshot)
        {
            if (war.Ended) continue;
            var atkSect = _gm.State.GetSect(war.AttackerSectId);
            var defSect = _gm.State.GetSect(war.DefenderSectId);
            if (atkSect == null || defSect == null) continue;

            var atkHome = _gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == atkSect.Id);
            var defHome = _gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == defSect.Id);
            if (atkHome == null || defHome == null) continue;

            Vector2 from = atkHome.Position;
            Vector2 to = defHome.Position;
            Vector2 dir = (to - from).Normalized();

            var arrow = new Node2D();
            arrow.Position = from;
            arrow.Rotation = dir.Angle();
            AddChild(arrow);

            float arrowLen = (to - from).Length();
            DrawArrow(arrow, arrowLen, 6f, 24f);
        }
    }

    private void DrawArrow(Node2D parent, float length, float thick, float headSize)
    {
        // white outline
        DrawSingle(parent, length + 3, thick + 3, headSize + 3, new Color(1, 1, 1, 0.9f));
        // red fill
        DrawSingle(parent, length, thick, headSize, new Color(0.9f, 0.1f, 0.1f, 0.85f));
    }

    private void DrawSingle(Node2D parent, float len, float thick, float head, Color color)
    {
        var shaft = new ColorRect();
        shaft.Size = new Vector2(len - head, thick);
        shaft.Color = color;
        shaft.Position = new Vector2(0, -thick / 2f);
        parent.AddChild(shaft);

        var headPoly = new Polygon2D();
        headPoly.Polygon = new Vector2[] {
            new Vector2(len - head, 0),
            new Vector2(len - head - head, -head / 2),
            new Vector2(len - head - head, head / 2),
        };
        headPoly.Color = color;
        parent.AddChild(headPoly);
    }
}
