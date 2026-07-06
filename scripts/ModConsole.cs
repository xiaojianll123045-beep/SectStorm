using Godot;
using System.Collections.Generic;

public partial class ModConsole : Control
{
    private static ModConsole _instance;
    private LineEdit _input;
    private RichTextLabel _output;
    private Panel _panel;
    private bool _isOpen;
    private List<string> _history = new();
    private int _historyIdx = -1;

    private struct Command
    {
        public string Name, Desc, Usage;
        public System.Action<string[]> Handler;
    }
    private static List<Command> _commands = new();

    public static void RegisterCommand(string name, string desc, string usage, System.Action<string[]> handler)
    {
        _commands.RemoveAll(c => c.Name == name);
        _commands.Add(new Command { Name = name, Desc = desc, Usage = usage, Handler = handler });
    }

    public static void Init(Node parent)
    {
        if (_instance != null) return;
        _instance = new ModConsole();
        _instance.Name = "ModConsole";
        _instance.SetAnchorsPreset(LayoutPreset.FullRect);
        _instance.Visible = false;
        parent.AddChild(_instance);
        _instance.BuildUI();

        RegisterCommand("help", "显示帮助", "help", (a) => {
            _instance.Print("可用命令:");
            foreach (var c in _commands)
                _instance.Print($"  {c.Name} - {c.Desc}");
        });
        RegisterCommand("clear", "清屏", "clear", (a) => _instance._output.Text = "");
        RegisterCommand("list_mods", "列出已加载的模组", "list_mods", (a) => {
            foreach (var m in ModManager.LoadedMods)
                _instance.Print($"  [{m.Type}] {m.Name} v{m.Version} by {m.Author}");
        });
        RegisterCommand("mod_log", "查看模组日志", "mod_log", (a) => {
            foreach (var l in ModManager.ModLog)
                _instance.Print(l);
        });
    }

    private void BuildUI()
    {
        _panel = new Panel();
        _panel.SetAnchorsPreset(LayoutPreset.FullRect);
        _panel.Size = new Vector2(800, 400);
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.08f, 0.08f, 0.10f, 0.92f);
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);

        _output = new RichTextLabel();
        _output.SetAnchorsPreset(LayoutPreset.TopWide);
        _output.Position = new Vector2(8, 8);
        _output.Size = new Vector2(784, 340);
        _output.AddThemeColorOverride("default_color", new Color(0.7f, 0.9f, 0.7f));
        _output.AddThemeFontSizeOverride("normal_font_size", 12);
        _panel.AddChild(_output);

        _input = new LineEdit();
        _input.SetAnchorsPreset(LayoutPreset.BottomWide);
        _input.Position = new Vector2(8, 356);
        _input.Size = new Vector2(784, 32);
        _input.PlaceholderText = "输入命令... (Enter执行, ↑↓历史)";
        _input.TextSubmitted += OnSubmit;
        _input.GuiInput += (e) =>
        {
            if (e is InputEventKey key && key.Pressed)
            {
                if (key.Keycode == Key.Up)
                {
                    if (_history.Count > 0 && _historyIdx >= 0)
                    {
                        _historyIdx = Mathf.Max(0, _historyIdx - 1);
                        _input.Text = _history[_historyIdx];
                        _input.CaretColumn = _input.Text.Length;
                    }
                }
                else if (key.Keycode == Key.Down)
                {
                    if (_history.Count > 0 && _historyIdx < _history.Count - 1)
                    {
                        _historyIdx++;
                        _input.Text = _history[_historyIdx];
                        _input.CaretColumn = _input.Text.Length;
                    }
                }
            }
        };
        _panel.AddChild(_input);
    }

    private void OnSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _history.Add(text);
        _historyIdx = _history.Count;
        _input.Text = "";

        var parts = text.Trim().Split(' ');
        var cmdName = parts[0].ToLower();
        var args = new string[parts.Length - 1];
        for (int i = 1; i < parts.Length; i++) args[i - 1] = parts[i];

        bool found = false;
        foreach (var c in _commands)
        {
            if (c.Name == cmdName)
            {
                found = true;
                try { c.Handler(args); } catch (System.Exception e) { Print($"错误: {e.Message}"); }
                break;
            }
        }
        if (!found) Print($"未知命令: {cmdName} (输入 help 查看可用命令)");
    }

    private void Print(string msg) => _output.Text += msg + "\n";

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_console"))
        {
            _isOpen = !_isOpen;
            Visible = _isOpen;
            if (_isOpen) _input.GrabFocus();
            GetViewport().SetInputAsHandled();
        }
    }
}
