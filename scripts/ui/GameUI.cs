using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameUI : CanvasLayer
{
    private GameManager _gm;
    private MapView _mapView;
    private Label _lingshiLabel, _prestigeLabel, _discipleCountLabel, _dateLabel, _statusLabel;

    // bottom panel
    public Panel _bottomPanel;
    public RichTextLabel _bottomInfo;
    private float _statusTimer;

    // context popup
    private Panel _contextPanel;
    private VBoxContainer _contextBox;
    private bool _contextVisible;

    // event log
    private Panel _logPanel;
    private RichTextLabel _logText;
    private bool _logVisible;

    private GameManager GM
    {
        get
        {
            if (_gm != null) return _gm;
            if (_mapView == null) _mapView = GetParent().GetParent<MapView>();
            _gm = _mapView?.GetNodeOrNull<GameManager>("GameManager");
            return _gm;
        }
    }

    public override void _Ready()
    {
        _mapView = GetParent().GetParent<MapView>();
        _gm = _mapView?.GetNodeOrNull<GameManager>("GameManager");
        BuildTopBar();
        BuildBottomPanel();
        BuildContextMenu();
        BuildLogPanel();
    }

    private void BuildTopBar()
    {
        var bar = new ColorRect();
        bar.Color = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        bar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        bar.CustomMinimumSize = new Vector2(0, 28);
        AddChild(bar);

        _lingshiLabel = new Label { Position = new Vector2(10, 4) };
        _lingshiLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_lingshiLabel);

        _prestigeLabel = new Label { Position = new Vector2(180, 4) };
        _prestigeLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_prestigeLabel);

        _discipleCountLabel = new Label { Position = new Vector2(350, 4) };
        _discipleCountLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_discipleCountLabel);

        _dateLabel = new Label { Position = new Vector2(520, 4) };
        _dateLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_dateLabel);

        _statusLabel = new Label { Position = new Vector2(750, 4) };
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        _statusLabel.Visible = false;
        AddChild(_statusLabel);
    }

    private void BuildBottomPanel()
    {
        _bottomPanel = new Panel();
        _bottomPanel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _bottomPanel.CustomMinimumSize = new Vector2(0, 80);
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        _bottomPanel.AddThemeStyleboxOverride("panel", style);
        AddChild(_bottomPanel);

        _bottomInfo = new RichTextLabel();
        _bottomInfo.Position = new Vector2(8, 4);
        _bottomInfo.Size = new Vector2(1200, 72);
        _bottomInfo.BbcodeEnabled = true;
        _bottomInfo.AddThemeFontSizeOverride("normal_font_size", 12);
        _bottomInfo.AddThemeColorOverride("default_color", new Color(0.85f, 0.85f, 0.90f));
        _bottomPanel.AddChild(_bottomInfo);

        var closeBtn = new Button();
        closeBtn.Text = "✕";
        closeBtn.Position = new Vector2(1240, 4);
        closeBtn.Size = new Vector2(24, 24);
        closeBtn.Pressed += () => _bottomPanel.Visible = false;
        _bottomPanel.AddChild(closeBtn);
    }

    private void BuildContextMenu()
    {
        _contextPanel = new Panel();
        _contextPanel.Visible = false;
        _contextPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        var cs = new StyleBoxFlat();
        cs.BgColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        cs.BorderColor = new Color(0.3f, 0.3f, 0.35f);
        cs.BorderWidthTop = 1; cs.BorderWidthBottom = 1; cs.BorderWidthLeft = 1; cs.BorderWidthRight = 1;
        _contextPanel.AddThemeStyleboxOverride("panel", cs);
        AddChild(_contextPanel);

        _contextBox = new VBoxContainer();
        _contextBox.Position = new Vector2(4, 4);
        _contextBox.AddThemeConstantOverride("separation", 2);
        _contextPanel.AddChild(_contextBox);
    }

    private void BuildLogPanel()
    {
        _logPanel = new Panel();
        _logPanel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _logPanel.Position = new Vector2(-300, 35);
        _logPanel.Size = new Vector2(300, 400);
        _logPanel.Visible = false;
        var ls = new StyleBoxFlat();
        ls.BgColor = new Color(0.08f, 0.08f, 0.10f, 0.92f);
        _logPanel.AddThemeStyleboxOverride("panel", ls);
        AddChild(_logPanel);

        var logTitle = new Label();
        logTitle.Text = "事件日志 (按 L 开关)";
        logTitle.Position = new Vector2(8, 4);
        logTitle.AddThemeFontSizeOverride("font_size", 12);
        logTitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        _logPanel.AddChild(logTitle);

        _logText = new RichTextLabel();
        _logText.Position = new Vector2(4, 24);
        _logText.Size = new Vector2(292, 372);
        _logText.BbcodeEnabled = true;
        _logText.AddThemeFontSizeOverride("normal_font_size", 11);
        _logText.AddThemeColorOverride("default_color", new Color(0.7f, 0.7f, 0.75f));
        _logPanel.AddChild(_logText);
    }

    public override void _Process(double delta)
    {
        if (GM == null || GM.State == null) return;
        UpdateTopBar();
        UpdateLog();
    }

    private void UpdateTopBar()
    {
        var sect = GM.State.PlayerSect;
        if (sect == null) return;
        _lingshiLabel.Text = $"灵石: {(int)sect.Lingshi}";
        _lingshiLabel.AddThemeColorOverride("font_color", Colors.Gold);
        _prestigeLabel.Text = $"声望: {(int)sect.Prestige}";
        _prestigeLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.6f, 0.3f));
        int total = GM.State.Disciples.Count(d => d.SectId == sect.Id);
        _discipleCountLabel.Text = $"弟子: {total}/{sect.MaxDisciples()}";
        _discipleCountLabel.AddThemeColorOverride("font_color", Colors.LightBlue);
        _dateLabel.Text = $"第{GM.State.Year}年 {GM.State.Month}月 ({GM.State.Xun}/36)";
        _dateLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));

        if (GM.AiProcessing)
        {
            _statusLabel.Text = $"AI处理中... ({GM.AiProgress}/{GM.AiTotal})";
            _statusLabel.Visible = true;
        }
        else if (_statusLabel.Visible)
        {
            _statusLabel.Visible = false;
        }
    }

    private void UpdateLog()
    {
        if (_logVisible && GM.State.TurnLog.Count > 0)
        {
            string t = "";
            int start = Mathf.Max(0, GM.State.TurnLog.Count - 30);
            for (int i = start; i < GM.State.TurnLog.Count; i++)
                t += GM.State.TurnLog[i] + "\n";
            _logText.Text = t;
        }
    }

    public void ToggleLog()
    {
        _logVisible = !_logVisible;
        _logPanel.Visible = _logVisible;
    }

    // ---- Bottom Info Panel ----
    public void ShowLocationInfo(MapLocation loc)
    {
        _bottomPanel.Visible = true;
        string text = $"[b]{loc.Name}[/b] ({loc.Type})\n";
        text += $"人口: {loc.Population}  ";
        var ld = GM?.Locations.FirstOrDefault(l => l.Name == loc.Name);
        if (ld != null)
        {
            text += $"繁荣: {(int)ld.Prosperity}  忠诚: {(int)ld.Loyalty}  状态: {ld.Status}";
            text += $"\n所属: {GM.State.GetSect(ld.OwnerSectId)?.Name ?? "无"}";
        }
        text += $"\n位置: ({loc.Position.X:F0}, {loc.Position.Y:F0})";
        _bottomInfo.Text = text;
    }

    public void ShowArmyInfo(ArmyData army)
    {
        _bottomPanel.Visible = true;
        var sect = GM?.State.GetSect(army.SectId);
        string text = $"[b]部队 #{army.Id}[/b]  所属: {sect?.Name ?? "无"}\n";
        text += $"人数: {army.Count}  战力: {army.EffectiveCombat}  状态: {army.Order}\n";
        text += $"位置: ({army.Position.X:F0}, {army.Position.Y:F0})";
        if (army.Order == ArmyOrder.Attacking)
            text += $"  攻击目标: #{army.AttackTargetArmyId}";
        _bottomInfo.Text = text;
    }

    public void ShowSectInfo(SectData sect)
    {
        _bottomPanel.Visible = true;
        int discipleCount = _gm?.State.Disciples.Count(d => d.SectId == sect.Id) ?? 0;
        string text = $"[b]{sect.Name}[/b]  ({(sect.IsPlayer ? "玩家" : "AI")})\n";
        text += $"灵石: {(int)sect.Lingshi}  声望: {(int)sect.Prestige}  灵脉: {(int)sect.SpiritVein}\n";
        text += $"建筑: 议{sect.MeetingHall}修{sect.CultivationRoom}藏{sect.Library}丹{sect.AlchemyRoom}田{sect.SpiritField}阵{sect.ProtectionArray}\n";
        text += $"弟子: {discipleCount}  城市: {sect.ControlledCityIds.Count}  个性: {sect.Personality}";
        _bottomInfo.Text = text;
    }

    public void HideInfo()
    {
        _bottomPanel.Visible = false;
    }

    // ---- Context Menu ----
    public void ShowContextMenu(Vector2 screenPos, List<(string text, System.Action action)> items)
    {
        _contextBox.GetChildren().ToList().ForEach(c => c.QueueFree());
        foreach (var item in items)
        {
            var btn = new Button();
            btn.Text = item.text;
            btn.CustomMinimumSize = new Vector2(120, 24);
            btn.AddThemeFontSizeOverride("font_size", 12);
            btn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
            btn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.85f, 0.4f));
            btn.Pressed += () => { item.action(); _contextPanel.Visible = false; };
            _contextBox.AddChild(btn);
        }
        _contextPanel.Position = screenPos;
        _contextPanel.Size = new Vector2(140, items.Count * 28 + 8);
        _contextPanel.Visible = true;
        _contextVisible = true;
    }

    public void HideContextMenu()
    {
        _contextPanel.Visible = false;
        _contextVisible = false;
    }
}
