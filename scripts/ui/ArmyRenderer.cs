using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ArmyRenderer : Node2D
{
    private GameManager _gm;
    private Dictionary<int, Sprite2D> _armySprites = new();
    private Dictionary<int, Label> _armyLabels = new();
    private Dictionary<int, Area2D> _armyAreas = new();

    private Texture2D _playerDot;
    private Texture2D _stackDot;
    private Texture2D _enemyDot;
    private Texture2D _allyDot;
    private Texture2D _neutralDot;

    private Font _font;

    public override void _Ready()
    {
        _gm = GetNodeOrNull<GameManager>("../GameManager");
        MakeTextures();
    }

    private void MakeTextures()
    {
        _playerDot = MakeCircle(8, new Color(0.2f, 0.8f, 0.2f));
        _stackDot = MakeCircle(9, new Color(0.9f, 0.7f, 0.1f));
        _enemyDot = MakeCircle(8, new Color(0.8f, 0.2f, 0.2f));
        _allyDot = MakeCircle(8, new Color(0.3f, 0.6f, 1.0f));
        _neutralDot = MakeCircle(8, new Color(0.5f, 0.5f, 0.5f));
    }

    private Texture2D MakeCircle(int r, Color c)
    {
        int d = r * 2;
        var img = Image.CreateEmpty(d, d, false, Image.Format.Rgba8);
        Color b = c.Lerp(Colors.White, 0.4f);
        for (int y = 0; y < d; y++)
            for (int x = 0; x < d; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= r)
                    img.SetPixel(x, y, dist > r - 2f ? b : c);
            }
        return ImageTexture.CreateFromImage(img);
    }

    public override void _Process(double delta)
    {
        if (_gm == null) return;
        SyncArmies();
    }

    private void SyncArmies()
    {
        int playerSectId = _gm.State.PlayerSectId;
        var validIds = new HashSet<int>();

        foreach (var army in _gm.Armies)
        {
            if (!army.IsAlive) continue;
            validIds.Add(army.Id);

            if (!_armySprites.ContainsKey(army.Id))
                CreateArmySprite(army, playerSectId);

            UpdateArmySprite(army, playerSectId);
        }

        // remove dead armies
        var toRemove = new List<int>();
        foreach (var id in _armySprites.Keys)
            if (!validIds.Contains(id)) toRemove.Add(id);
        foreach (var id in toRemove)
        {
            _armySprites[id].QueueFree();
            _armySprites.Remove(id);
            if (_armyLabels.TryGetValue(id, out var lbl)) { lbl.QueueFree(); _armyLabels.Remove(id); }
            if (_armyAreas.TryGetValue(id, out var area)) { area.QueueFree(); _armyAreas.Remove(id); }
        }
    }

    private Texture2D GetDot(ArmyData army, int playerSectId)
    {
        if (army.SectId == playerSectId)
        {
            // check if stacked with other player armies
            int count = _gm.Armies.Count(a => a.SectId == playerSectId && a.IsAlive &&
                (a.Position - army.Position).LengthSquared() < 400f);
            return count > 1 ? _stackDot : _playerDot;
        }
        var rel = _gm.State.GetRelation(playerSectId, army.SectId);
        if (rel == null) return _neutralDot;
        return rel.State switch
        {
            RelationState.Ally => _allyDot,
            RelationState.War or RelationState.Hostile => _enemyDot,
            _ => _neutralDot
        };
    }

    private void CreateArmySprite(ArmyData army, int playerSectId)
    {
        var spr = new Sprite2D();
        spr.Texture = GetDot(army, playerSectId);
        spr.Centered = true;
        spr.ZIndex = 50;
        AddChild(spr);
        _armySprites[army.Id] = spr;

        var lbl = new Label();
        lbl.AddThemeFontSizeOverride("font_size", 8);
        lbl.Position = new Vector2(-10, 8);
        lbl.AddThemeColorOverride("font_color", Colors.White);
        spr.AddChild(lbl);
        _armyLabels[army.Id] = lbl;

        var area = new Area2D();
        area.ZIndex = 50;
        var shape = new CollisionShape2D();
        shape.Shape = new CircleShape2D { Radius = 12f };
        area.AddChild(shape);
        area.InputEvent += (Node _, InputEvent ev, long __) =>
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
                OnArmyRightClicked(army);
        };
        spr.AddChild(area);
        _armyAreas[army.Id] = area;
    }

    private void UpdateArmySprite(ArmyData army, int playerSectId)
    {
        if (!_armySprites.TryGetValue(army.Id, out var spr)) return;
        spr.Position = army.Position;
        spr.Texture = GetDot(army, playerSectId);

        if (_armyLabels.TryGetValue(army.Id, out var lbl))
        {
            int count = _gm.Armies.Count(a => a.SectId == army.SectId && a.IsAlive &&
                (a.Position - army.Position).LengthSquared() < 400f);
            if (count > 1)
                lbl.Text = $"{count}队|{_gm.Armies.Where(a => (a.Position - army.Position).LengthSquared() < 400f).Sum(a => a.Count)}人";
            else
                lbl.Text = $"#{army.Id}";
        }
    }

    private void OnArmyRightClicked(ArmyData army)
    {
        var ui = GetNodeOrNull<GameUI>("../CanvasLayer/GameUI");
        if (ui == null) return;

        var screenPos = GetViewport().GetMousePosition();
        var items = new List<(string, System.Action)>();

        if (army.SectId == _gm.State.PlayerSectId)
        {
            items.Add(("移动", () => StartMove(army)));
            items.Add(("进攻", () => StartAttack(army)));
            items.Add(("解散", () => _gm.DisbandArmy(army)));
        }
        else
        {
            items.Add(("查看", () => GD.Print($"查看 {army.Name}")));
        }
        ui.ShowContextMenu(screenPos, items);
    }

    private void StartMove(ArmyData army) { GD.Print($"[UI] 选择 {army.Name} 移动目标"); }
    private void StartAttack(ArmyData army) { GD.Print($"[UI] 选择 {army.Name} 攻击目标"); }
}
