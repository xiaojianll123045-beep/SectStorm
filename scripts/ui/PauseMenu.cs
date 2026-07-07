using Godot;
using System.Linq;

public partial class PauseMenu : Control
{
    private bool _open;
    private Control _slotPanel;
    private VBoxContainer _slotBox;

    public override void _Ready()
    {
        Visible = false;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.6f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.Size = new Vector2(260, 340);
        panel.Position = new Vector2(-130, -170);
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.10f, 0.10f, 0.12f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(20, 20);
        vbox.Size = new Vector2(220, 300);
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "暂停";
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        vbox.AddChild(title);

        AddBtn(vbox, "继续游戏", () => { Visible = false; _open = false; });
        AddBtn(vbox, "保存游戏", () => ShowSlots(true));
        AddBtn(vbox, "读取存档", () => ShowSlots(false));
        AddBtn(vbox, "宗门面板", () => { Visible = false; _open = false; EmitSignal(nameof(OpenSectPanel)); });
        AddBtn(vbox, "外交", () => { Visible = false; _open = false; EmitSignal(nameof(OpenDiplomacy)); });
        AddBtn(vbox, "编组部队", () => { Visible = false; _open = false; EmitSignal(nameof(OpenArmyCreator)); });
        AddBtn(vbox, "返回主菜单", () => GetTree().ChangeSceneToFile("res://scenes/menu.tscn"));
        AddBtn(vbox, "退出游戏", () => GetTree().Quit());
    }

    [Signal] public delegate void OpenSectPanelEventHandler();
    [Signal] public delegate void OpenDiplomacyEventHandler();
    [Signal] public delegate void OpenArmyCreatorEventHandler();

    private void AddBtn(Container parent, string text, System.Action action)
    {
        var btn = new Button();
        btn.Text = text;
        btn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
        btn.CustomMinimumSize = new Vector2(0, 26);
        btn.AddThemeFontSizeOverride("font_size", 13);
        btn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
        btn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.85f, 0.4f));
        btn.Pressed += action;
        parent.AddChild(btn);
    }

    public void Toggle()
    {
        _open = !_open;
        Visible = _open;
    }

    // ---- Save/Load Slots ----
    private void ShowSlots(bool saving)
    {
        if (_slotPanel == null) BuildSlotPanel();
        _slotBox.GetChildren().ToList().ForEach(c => c.QueueFree());

        var saves = SaveLoadManager.ListSaves();

        var title = new Label();
        title.Text = saving ? "保存游戏" : "读取存档";
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        _slotBox.AddChild(title);

        // auto-save slot
        AddSlotRow("自动存档", saves.Contains("autosave"), saving, "autosave");

        // 5 manual slots
        for (int i = 1; i <= 5; i++)
        {
            int s = i;
            string slotName = $"slot{s}";
            AddSlotRow($"存档位 {s}", saves.Contains(slotName), saving, slotName);
        }

        var closeBtn = new Button();
        closeBtn.Text = "关闭";
        closeBtn.CustomMinimumSize = new Vector2(0, 24);
        closeBtn.AddThemeFontSizeOverride("font_size", 12);
        closeBtn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
        closeBtn.Pressed += () => _slotPanel.Visible = false;
        _slotBox.AddChild(closeBtn);

        _slotPanel.Visible = true;
    }

    private void AddSlotRow(string label, bool exists, bool saving, string slotName)
    {
        var hbox = new HBoxContainer();
        hbox.CustomMinimumSize = new Vector2(0, 22);

        var lbl = new Label();
        lbl.Text = $"{label} {(exists ? "" : "(空)")}";
        lbl.CustomMinimumSize = new Vector2(100, 0);
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", exists ? new Color(0.85f, 0.85f, 0.90f) : new Color(0.5f, 0.5f, 0.55f));
        hbox.AddChild(lbl);

        if (exists)
        {
            var btn = new Button();
            btn.Text = saving ? "覆盖" : "读取";
            btn.CustomMinimumSize = new Vector2(40, 20);
            btn.AddThemeFontSizeOverride("font_size", 10);
            btn.Pressed += () =>
            {
                var gm = GetNodeOrNull<GameManager>("../../GameManager");
                if (gm == null) return;
                if (saving) SaveLoadManager.SaveGame(gm, slotName);
                else
                {
                    SaveLoadManager.LoadGame(gm, slotName);
                    _slotPanel.Visible = false;
                    Visible = false; _open = false;
                    var mv = GetNodeOrNull<MapView>("../..");
                    mv?.Call("OnLoadGame");
                }
            };
            hbox.AddChild(btn);
        }
        else if (saving)
        {
            var btn = new Button();
            btn.Text = "保存";
            btn.CustomMinimumSize = new Vector2(40, 20);
            btn.AddThemeFontSizeOverride("font_size", 10);
            btn.Pressed += () =>
            {
                var gm = GetNodeOrNull<GameManager>("../../GameManager");
                if (gm != null) SaveLoadManager.SaveGame(gm, slotName);
            };
            hbox.AddChild(btn);
        }

        _slotBox.AddChild(hbox);
    }

    private void BuildSlotPanel()
    {
        _slotPanel = new Control();
        _slotPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _slotPanel.Visible = false;
        AddChild(_slotPanel);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Pass;
        _slotPanel.AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.Size = new Vector2(250, 320);
        panel.Position = new Vector2(-125, -160);
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.10f, 0.10f, 0.12f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        _slotPanel.AddChild(panel);

        _slotBox = new VBoxContainer();
        _slotBox.Position = new Vector2(16, 16);
        _slotBox.Size = new Vector2(220, 290);
        _slotBox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(_slotBox);
    }
}
