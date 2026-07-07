using Godot;
using System;
using System.Collections.Generic;

public partial class EventPopup : Control
{
    private Label _titleLabel;
    private RichTextLabel _descLabel;
    private HBoxContainer _btnBox;
    private Action _onClose;

    public override void _Ready()
    {
        Visible = false;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.6f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Pass;
        AddChild(overlay);

        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.Size = new Vector2(450, 250);
        panel.Position = new Vector2(-225, -125);
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.10f, 0.10f, 0.12f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        AddChild(panel);

        _titleLabel = new Label();
        _titleLabel.Position = new Vector2(16, 12);
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.2f));
        panel.AddChild(_titleLabel);

        _descLabel = new RichTextLabel();
        _descLabel.Position = new Vector2(16, 44);
        _descLabel.Size = new Vector2(418, 130);
        _descLabel.BbcodeEnabled = true;
        _descLabel.AddThemeFontSizeOverride("normal_font_size", 13);
        _descLabel.AddThemeColorOverride("default_color", new Color(0.85f, 0.85f, 0.90f));
        panel.AddChild(_descLabel);

        _btnBox = new HBoxContainer();
        _btnBox.Position = new Vector2(16, 190);
        _btnBox.Size = new Vector2(418, 40);
        _btnBox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(_btnBox);
    }

    public void ShowEvent(string title, string desc, List<(string text, Action cb)> options)
    {
        _titleLabel.Text = title;
        _descLabel.Text = desc;

        foreach (var c in _btnBox.GetChildren())
            c.QueueFree();

        foreach (var opt in options)
        {
            var btn = new Button();
            btn.Text = opt.text;
            btn.CustomMinimumSize = new Vector2(100, 28);
            btn.AddThemeFontSizeOverride("font_size", 13);
            btn.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.90f));
            btn.AddThemeColorOverride("font_hover_color", new Color(1f, 0.85f, 0.4f));
            var cb = opt.cb;
            btn.Pressed += () => { cb?.Invoke(); Visible = false; };
            _btnBox.AddChild(btn);
        }

        _onClose = () => { };
        Visible = true;
    }
}
