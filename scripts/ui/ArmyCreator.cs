using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ArmyCreator : Control
{
    private GameManager _gm;
    private RichTextLabel _info;
    private bool _selectMode;
    private HashSet<int> _selected = new();

    public override void _Ready()
    {
        Visible = false;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.Size = new Vector2(500, 420);
        panel.Position = new Vector2(-250, -210);
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.10f, 0.10f, 0.12f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        AddChild(panel);

        var title = new Label();
        title.Text = "编组部队";
        title.Position = new Vector2(16, 12);
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        panel.AddChild(title);

        _info = new RichTextLabel();
        _info.Position = new Vector2(16, 40);
        _info.Size = new Vector2(468, 300);
        _info.BbcodeEnabled = true;
        _info.AddThemeFontSizeOverride("normal_font_size", 12);
        _info.AddThemeColorOverride("default_color", new Color(0.85f, 0.85f, 0.90f));
        panel.AddChild(_info);

        var createBtn = new Button();
        createBtn.Text = "派出部队";
        createBtn.Position = new Vector2(120, 350);
        createBtn.CustomMinimumSize = new Vector2(120, 30);
        createBtn.AddThemeFontSizeOverride("font_size", 14);
        createBtn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
        createBtn.Pressed += () => CreateArmy();
        panel.AddChild(createBtn);

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.Position = new Vector2(320, 350);
        closeBtn.CustomMinimumSize = new Vector2(80, 28);
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
        closeBtn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.85f, 0.4f));
        closeBtn.Pressed += () => { Visible = false; };
        panel.AddChild(closeBtn);
    }

    public override void _Input(InputEvent @event)
    {
        if (!_selectMode || !Visible) return;
        if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Shift)
        {
            // toggle selection mode hint
        }
    }

    public new void Hide() { Visible = false; _selected.Clear(); }

    public new void Show()
    {
        _gm = GetNodeOrNull<GameManager>("../GameManager");
        Visible = true;
        Refresh();
    }

    private void Refresh()
    {
        if (_gm == null || _gm.State == null) return;
        var sect = _gm.State.PlayerSect;
        if (sect == null) return;

        var disciples = _gm.State.Disciples.Where(d => d.SectId == sect.Id && d.State == "idle" && !InArmy(d.Id)).ToList();
        string txt = $"[b]空闲弟子 ({disciples.Count} 人)[/b]\n";
        txt += "左键点击选择, Shift+点击多选\n\n";

        for (int i = 0; i < disciples.Count; i++)
        {
            var d = disciples[i];
            string prefix = _selected.Contains(d.Id) ? "✓ " : "  ";
            txt += $"{prefix}{d.Name} ({d.Realm}) 战力{d.Combat} 寿元{d.Lifespan}旬\n";
        }

        txt += $"\n已选: {_selected.Count} 人  总战力: {_selected.Sum(id => _gm.State.Disciples.FirstOrDefault(d => d.Id == id)?.Combat ?? 0)}";
        _info.Text = txt;
    }

    private bool InArmy(int discipleId)
    {
        return _gm?.Armies.Any(a => a.DiscipleIds.Contains(discipleId)) ?? false;
    }

    private void CreateArmy()
    {
        if (_gm == null || _selected.Count == 0) return;
        var sect = _gm.State.PlayerSect;
        if (sect == null) return;

        var home = _gm.Locations.FirstOrDefault(l => l.Type == LocationType.Sect && l.OwnerSectId == sect.Id);
        if (home == null) return;

        _gm.CreateArmy(sect.Id, _selected.ToList(), home.Position);
        _selected.Clear();
        Refresh();
        _gm.State.Log($"派出部队 ({_selected.Count}人)");
    }
}
