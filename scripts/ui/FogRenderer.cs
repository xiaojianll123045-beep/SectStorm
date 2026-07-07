using Godot;

public partial class FogRenderer : Sprite2D
{
    private GameManager _gm;
    private int _mapW, _mapH;
    private int _lastUpdateTurn = -1;

    public void Init(GameManager gm, int mapW, int mapH)
    {
        _gm = gm;
        _mapW = mapW;
        _mapH = mapH;
        Centered = false;
        TextureFilter = TextureFilterEnum.Linear;
        ZIndex = 80;
        if (_gm != null)
            Callable.From(() => Refresh()).CallDeferred();
    }

    public override void _Process(double delta)
    {
        if (_gm == null || _gm.State == null) return;
        int turn = _gm.State.TotalTurns;
        if (_lastUpdateTurn == turn) return;
        if (turn % 3 != 0) return;
        Refresh();
    }

    private void Refresh()
    {
        if (_gm == null || _gm.State == null) return;
        _lastUpdateTurn = _gm.State.TotalTurns;
        int cellSize = 32;
        var fogImg = FogOfWar.GenerateFogImage(_mapW, _mapH, cellSize,
            _gm.State.PlayerSectId, _gm.State, _gm.Locations, _gm.Armies);
        Texture = ImageTexture.CreateFromImage(fogImg);
        Scale = new Vector2(cellSize, cellSize);
    }
}
