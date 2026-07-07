using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameUI : CanvasLayer
{
    private MapView _mapView;
    private GameManager _gm;

    // top bar
    private Label _lingshiLabel;
    private Label _prestigeLabel;
    private Label _discipleCountLabel;
    private Label _dateLabel;
    private Label _statusLabel;
    private float _statusTimer;

    public override void _Ready()
    {
        _mapView = GetParent().GetParent<MapView>();
        _gm = _mapView?.GetNodeOrNull<GameManager>("GameManager");
        if (_gm == null) _gm = _mapView?.GetNodeOrNull<GameManager>("GameManager");

        BuildTopBar();
        BuildContextMenu();
    }

    private void BuildTopBar()
    {
        var bar = new ColorRect();
        bar.Color = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        bar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        bar.CustomMinimumSize = new Vector2(0, 28);
        AddChild(bar);

        var font = new Label();
        font.AddThemeFontSizeOverride("font_size", 12);

        _lingshiLabel = new Label();
        _lingshiLabel.Position = new Vector2(10, 4);
        AddChild(_lingshiLabel);

        _prestigeLabel = new Label();
        _prestigeLabel.Position = new Vector2(200, 4);
        AddChild(_prestigeLabel);

        _discipleCountLabel = new Label();
        _discipleCountLabel.Position = new Vector2(400, 4);
        AddChild(_discipleCountLabel);

        _dateLabel = new Label();
        _dateLabel.Position = new Vector2(600, 4);

        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(800, 4);
        _statusLabel.Visible = false;
        _statusLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f));
        AddChild(_statusLabel);
        AddChild(_dateLabel);
    }

    private Control _contextMenu;
    private PopupMenu _popup;

    private void BuildContextMenu()
    {
        _contextMenu = new Control();
        _contextMenu.Visible = false;
        _contextMenu.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_contextMenu);

        _popup = new PopupMenu();
        _popup.Visible = false;
        _popup.PopupHide += () => _contextMenu.Visible = false;
        _contextMenu.AddChild(_popup);
    }

    public override void _Process(double delta)
    {
        if (_gm == null || _gm.State == null) return;
        UpdateTopBar();
        if (_gm.AiProcessing)
        {
            _statusTimer = 5f;
            _statusLabel.Text = $"AI处理中... ({_gm.AiProgress}/{_gm.AiTotal})";
            _statusLabel.Visible = true;
        }
        else if (_statusTimer > 0)
        {
            _statusTimer -= (float)delta;
            if (_statusTimer <= 0) _statusLabel.Visible = false;
        }
    }

    private void UpdateTopBar()
    {
        var sect = _gm.State.PlayerSect;
        if (sect == null) return;
        _lingshiLabel.Text = $"灵石: {(int)sect.Lingshi}";
        _lingshiLabel.AddThemeColorOverride("font_color", Colors.Gold);
        _prestigeLabel.Text = $"声望: {(int)sect.Prestige}";
        _prestigeLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.6f, 0.3f));
        int totalDisc = _gm.State.Disciples.Count(d => d.SectId == sect.Id);
        _discipleCountLabel.Text = $"弟子: {totalDisc}/{sect.MaxDisciples()}";
        _discipleCountLabel.AddThemeColorOverride("font_color", Colors.LightBlue);
        _dateLabel.Text = $"第{_gm.State.Year}年 {_gm.State.Month}月 ({_gm.State.Xun}/36)";
        _dateLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
    }

    public void ShowContextMenu(Vector2 screenPos, List<(string text, System.Action action)> items)
    {
        _popup.Clear();
        foreach (var item in items)
        {
            var id = _popup.ItemCount;
            _popup.AddItem(item.text, id);
            _popup.SetItemAccelerator(id, 0);
        }
        _popup.Position = (Vector2I)screenPos;
        _popup.Visible = true;
        _contextMenu.Visible = true;
        _popup.IdPressed += (long idx) => {
            int i = (int)idx;
            if (i >= 0 && i < items.Count)
                items[i].action();
            _popup.Visible = false;
            _contextMenu.Visible = false;
        };
    }

    public void HideContextMenu()
    {
        _popup.Visible = false;
        _contextMenu.Visible = false;
    }
}
