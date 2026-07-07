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
        if (_gm == null) return;

        // clear old arrows
        foreach (var c in GetChildren())
            c.QueueFree();

        float arrowLen = 20f;
        float headSize = 8f;

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

            // draw multiple arrows along the line
            int count = Mathf.Max(2, (int)(dist / 300f));
            for (int i = 1; i < count; i++)
            {
                float t = i / (float)count;
                Vector2 pos = a + dir * dist * t;
                // alternate offset to avoid overlap
                Vector2 perp = new Vector2(-dir.Y, dir.X) * (i % 2 == 0 ? 15f : -15f);

                var arrow = new Node2D();
                arrow.Position = pos + perp;
                AddChild(arrow);

                // shaft
                var shaft = new ColorRect();
                shaft.Size = new Vector2(arrowLen, 2);
                shaft.Color = new Color(0.9f, 0.15f, 0.15f, 0.8f);
                shaft.Position = new Vector2(0, -1);
                shaft.Rotation = dir.Angle();
                arrow.AddChild(shaft);

                // arrow head
                float angle = dir.Angle();
                var head = new Polygon2D();
                head.Polygon = new Vector2[] {
                    new Vector2(arrowLen, 0),
                    new Vector2(arrowLen - headSize, -headSize / 2),
                    new Vector2(arrowLen - headSize, headSize / 2),
                };
                head.Color = new Color(0.9f, 0.15f, 0.15f, 0.8f);
                head.Position = new Vector2(0, 0);
                head.Rotation = angle;
                arrow.AddChild(head);
            }
        }
    }
}
