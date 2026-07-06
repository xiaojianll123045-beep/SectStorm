using Godot;

public partial class MapCamera : Camera2D
{
    [Export] public float ZoomSpeed = 0.1f;
    [Export] public float MinZoom = 0.1f;
    [Export] public float MaxZoom = 3.0f;
    [Export] public float EdgePanMargin = 20f;
    [Export] public float EdgePanSpeed = 600f;

    private bool _dragging;
    private Vector2 _dragStart;
    private Vector2 _cameraStart;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                float z = Mathf.Clamp(Zoom.X * (1f + ZoomSpeed), MinZoom, MaxZoom);
                Zoom = new Vector2(z, z);
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                float z = Mathf.Clamp(Zoom.X * (1f - ZoomSpeed), MinZoom, MaxZoom);
                Zoom = new Vector2(z, z);
            }
            else if (mb.ButtonIndex == MouseButton.Middle)
            {
                if (mb.Pressed)
                {
                    _dragging = true;
                    _dragStart = GetViewport().GetMousePosition();
                    _cameraStart = Position;
                }
                else
                {
                    _dragging = false;
                }
            }
        }

        if (@event is InputEventMouseMotion mm && _dragging)
        {
            Vector2 delta = (GetViewport().GetMousePosition() - _dragStart) / Zoom;
            Position = _cameraStart - delta;
        }
    }

    public override void _Process(double delta)
    {
        Vector2 dir = Vector2.Zero;
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 screen = GetViewport().GetVisibleRect().Size;

        if (mouse.X < EdgePanMargin) dir.X -= 1f;
        if (mouse.X > screen.X - EdgePanMargin) dir.X += 1f;
        if (mouse.Y < EdgePanMargin) dir.Y -= 1f;
        if (mouse.Y > screen.Y - EdgePanMargin) dir.Y += 1f;

        if (dir != Vector2.Zero)
        {
            Position += dir.Normalized() * EdgePanSpeed * (float)delta / Zoom.Length();
        }
    }
}
