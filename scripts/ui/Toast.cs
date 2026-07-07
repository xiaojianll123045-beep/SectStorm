using Godot;
using System.Collections.Generic;

public partial class Toast : Control
{
    private VBoxContainer _box;
    private Queue<string> _queue = new();
    private bool _showing;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.TopWide);
        Position = new Vector2(0, 35);
        MouseFilter = Control.MouseFilterEnum.Ignore;

        _box = new VBoxContainer();
        _box.SetAnchorsPreset(LayoutPreset.TopWide);
        _box.AddThemeConstantOverride("separation", 4);
        _box.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_box);
    }

    public void ShowMessage(string text)
    {
        _queue.Enqueue(text);
        if (!_showing) ShowNext();
    }

    private void ShowNext()
    {
        if (_queue.Count == 0) { _showing = false; return; }
        _showing = true;
        var text = _queue.Dequeue();

        var panel = new Panel();
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.08f, 0.08f, 0.10f, 0.90f);
        style.BorderColor = new Color(0.7f, 0.5f, 0.2f);
        style.BorderWidthTop = 1; style.BorderWidthBottom = 1;
        style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
        panel.AddThemeStyleboxOverride("panel", style);
        panel.Size = new Vector2(400, 28);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _box.AddChild(panel);

        var label = new Label();
        label.Text = text;
        label.Position = new Vector2(8, 4);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        panel.AddChild(label);

        var timer = new Timer();
        timer.OneShot = true;
        timer.WaitTime = 4.0f;
        timer.Timeout += () =>
        {
            panel.QueueFree();
            ShowNext();
        };
        AddChild(timer);
        timer.Start();
    }
}
