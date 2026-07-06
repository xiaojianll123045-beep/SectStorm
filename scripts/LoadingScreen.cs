using Godot;

public partial class LoadingScreen : Control
{
    private Label _tipLabel;
    private ProgressBar _progressBar;
    private Label _statusLabel;
    private Timer _timer;

    private string[] _tips = new[]
    {
        "弟子不是消耗品，但寿元是",
        "灵脉决定下限，弟子决定上限",
        "不要把所有鸡蛋放在一个宗门",
        "外交比战争更划算",
        "凡人王朝更替时，是渗透的好机会",
        "妖兽潮来之前记得开启护山大阵",
    };

    public override void _Ready()
    {
        _tipLabel = GetNode<Label>("VBox/TipLabel");
        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _progressBar = GetNode<ProgressBar>("VBox/ProgressBar");
        _timer = GetNode<Timer>("Timer");

        _tipLabel.Text = _tips[(int)(GD.Randi() % _tips.Length)];

        _timer.Timeout += () =>
        {
            _progressBar.Value = Mathf.Min(_progressBar.Value + 1, _progressBar.MaxValue);
        };
        _timer.Start();
    }

    public void SetStatus(string text)
    {
        _statusLabel.Text = text;
    }

    public void SetProgress(float val)
    {
        _progressBar.Value = val;
    }

    public void Complete()
    {
        _timer.Stop();
    }
}
