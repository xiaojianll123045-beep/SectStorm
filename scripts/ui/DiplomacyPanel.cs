using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DiplomacyPanel : Control
{
    private GameManager _gm;
    private VBoxContainer _listBox;
    private bool _open;

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
        title.Text = "外交";
        title.Position = new Vector2(16, 12);
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        panel.AddChild(title);

        var scroll = new ScrollContainer();
        scroll.Position = new Vector2(8, 40);
        scroll.Size = new Vector2(484, 340);
        panel.AddChild(scroll);

        _listBox = new VBoxContainer();
        _listBox.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
        _listBox.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_listBox);

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.Position = new Vector2(400, 384);
        closeBtn.CustomMinimumSize = new Vector2(80, 28);
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
        closeBtn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.85f, 0.4f));
        closeBtn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.18f, 0.18f, 0.22f)));
        closeBtn.Pressed += () => { Visible = false; _open = false; };
        panel.AddChild(closeBtn);
    }

    private StyleBoxFlat MakeBtnStyle(Color bg)
    {
        var s = new StyleBoxFlat();
        s.BgColor = bg;
        s.CornerRadiusTopLeft = 4; s.CornerRadiusTopRight = 4;
        s.CornerRadiusBottomLeft = 4; s.CornerRadiusBottomRight = 4;
        return s;
    }

    public void Toggle()
    {
        if (!_open) Refresh();
        _open = !_open;
        Visible = _open;
    }

    private void Refresh()
    {
        if (_gm == null) _gm = GetNodeOrNull<GameManager>("../GameManager");
        if (_gm == null || _gm.State == null) return;
        var player = _gm.State.PlayerSect;
        if (player == null) return;

        foreach (var c in _listBox.GetChildren().ToList())
            c.QueueFree();

        foreach (var other in _gm.State.Sects.Where(s => s.IsAlive && s.Id != player.Id).OrderBy(s => s.Id))
        {
            var rel = _gm.State.GetRelation(player.Id, other.Id);
            int favor = rel?.Favor ?? 0;
            var state = rel?.State ?? RelationState.Neutral;

            var hbox = new HBoxContainer();
            hbox.CustomMinimumSize = new Vector2(0, 28);
            hbox.AddThemeConstantOverride("separation", 6);

            var nameLabel = new Label();
            nameLabel.Text = other.Name;
            nameLabel.CustomMinimumSize = new Vector2(120, 0);
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
            hbox.AddChild(nameLabel);

            Color stateColor = state switch
            {
                RelationState.Ally => Colors.Green,
                RelationState.War => Colors.Red,
                RelationState.Hostile => new Color(1f, 0.5f, 0f),
                RelationState.Subordinate => Colors.Yellow,
                _ => new Color(0.6f, 0.6f, 0.7f)
            };
            var stateLabel = new Label();
            stateLabel.Text = state.ToString();
            stateLabel.CustomMinimumSize = new Vector2(80, 0);
            stateLabel.AddThemeFontSizeOverride("font_size", 12);
            stateLabel.AddThemeColorOverride("font_color", stateColor);
            hbox.AddChild(stateLabel);

            var favorLabel = new Label();
            favorLabel.Text = $"好感:{favor}";
            favorLabel.CustomMinimumSize = new Vector2(60, 0);
            favorLabel.AddThemeFontSizeOverride("font_size", 12);
            favorLabel.AddThemeColorOverride("font_color", favor >= 0 ? Colors.Green : Colors.Red);
            hbox.AddChild(favorLabel);

            if (state != RelationState.War)
            {
                var warBtn = new Button();
                warBtn.Text = "宣战";
                warBtn.CustomMinimumSize = new Vector2(50, 24);
                warBtn.AddThemeFontSizeOverride("font_size", 11);
                warBtn.AddThemeColorOverride("font_color", new Color(1f, 0.4f, 0.4f));
                warBtn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.3f, 0.1f, 0.1f)));
                warBtn.Pressed += () =>
                {
                    _gm.DeclareWar(player.Id, other.Id);
                    Refresh();
                };
                hbox.AddChild(warBtn);
            }

            if (state == RelationState.War)
            {
                var peaceBtn = new Button();
                peaceBtn.Text = "议和";
                peaceBtn.CustomMinimumSize = new Vector2(50, 24);
                peaceBtn.AddThemeFontSizeOverride("font_size", 11);
                peaceBtn.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.4f));
                peaceBtn.AddThemeStyleboxOverride("normal", MakeBtnStyle(new Color(0.1f, 0.3f, 0.1f)));
                peaceBtn.Pressed += () =>
                {
                    var war = _gm.Wars.FirstOrDefault(w => (w.AttackerSectId == player.Id && w.DefenderSectId == other.Id) || (w.AttackerSectId == other.Id && w.DefenderSectId == player.Id));
                    if (war != null && !war.Ended) { war.Ended = true; _gm.State.Log($"{player.Name} 与 {other.Name} 议和"); }
                    Refresh();
                };
                hbox.AddChild(peaceBtn);
            }

            _listBox.AddChild(hbox);
        }
    }
}
