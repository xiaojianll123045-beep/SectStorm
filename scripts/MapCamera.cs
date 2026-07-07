using Godot;

public partial class MapCamera : Camera2D
{
    [Export] public float ZoomSpeed = 0.1f;
    [Export] public float MinZoom = 0.1f;
    [Export] public float MaxZoom = 3.0f;
    [Export] public float KeyPanSpeed = 800f;

    public float WorldW = 16384;
    public float WorldH = 16384;

    private bool _middleDrag, _rightDrag, _leftDrag;
    private Vector2 _midDragStart, _midCamStart;
    private Vector2 _rDragStart, _rCamStart;
    private Vector2 _lDragStart, _lCamStart;
    private bool _leftWasDrag;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
            {
                float z = Mathf.Clamp(Zoom.X * (1f + ZoomSpeed), MinZoom, MaxZoom);
                Zoom = new Vector2(z, z);
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown)
            {
                float z = Mathf.Clamp(Zoom.X * (1f - ZoomSpeed), MinZoom, MaxZoom);
                Zoom = new Vector2(z, z);
            }
        }

        // left drag
        if (@event is InputEventMouseButton lb && lb.ButtonIndex == MouseButton.Left)
        {
            if (lb.Pressed) { _leftDrag = true; _leftWasDrag = false; _lDragStart = GetViewport().GetMousePosition(); _lCamStart = Position; }
            else { _leftDrag = false; if (!_leftWasDrag) _leftWasDrag = false; }
        }
        if (@event is InputEventMouseMotion lm && _leftDrag)
        {
            Vector2 delta = (GetViewport().GetMousePosition() - _lDragStart);
            if (delta.Length() > 8f) _leftWasDrag = true;
            if (_leftWasDrag)
            {
                delta /= Zoom;
                Position = _lCamStart - delta;
            }
        }

        // middle drag
        if (@event is InputEventMouseButton mb2 && mb2.ButtonIndex == MouseButton.Middle)
        {
            if (mb2.Pressed) { _middleDrag = true; _midDragStart = GetViewport().GetMousePosition(); _midCamStart = Position; }
            else _middleDrag = false;
        }
        if (@event is InputEventMouseMotion mm && _middleDrag)
        {
            Vector2 delta = (GetViewport().GetMousePosition() - _midDragStart) / Zoom;
            Position = _midCamStart - delta;
        }

        // right drag
        if (@event is InputEventMouseButton mb3 && mb3.ButtonIndex == MouseButton.Right)
        {
            if (mb3.Pressed) { _rightDrag = true; _rDragStart = GetViewport().GetMousePosition(); _rCamStart = Position; }
            else _rightDrag = false;
        }
        if (@event is InputEventMouseMotion mm2 && _rightDrag)
        {
            Vector2 delta = (GetViewport().GetMousePosition() - _rDragStart) / Zoom;
            Position = _rCamStart - delta;
        }
    }

    public bool WasLeftDrag() { bool r = _leftWasDrag; _leftWasDrag = false; return r; }

    public override void _Process(double delta)
    {
        Vector2 dir = Vector2.Zero;
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 screen = GetViewport().GetVisibleRect().Size;

        if (mouse.X < 20f) dir.X -= 1f;
        if (mouse.X > screen.X - 20f) dir.X += 1f;
        if (mouse.Y < 20f) dir.Y -= 1f;
        if (mouse.Y > screen.Y - 20f) dir.Y += 1f;

        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) dir.Y -= 1f;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) dir.Y += 1f;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) dir.X -= 1f;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) dir.X += 1f;

        if (dir != Vector2.Zero)
        {
            float speed = Input.IsKeyPressed(Key.Shift) ? KeyPanSpeed * 2.5f : KeyPanSpeed;
            Position += dir.Normalized() * speed * (float)delta / Zoom.Length();
        }

        // wrap
        var p = Position;
        bool wrapped = false;
        if (p.X < 0) { p.X += WorldW; wrapped = true; }
        if (p.X >= WorldW) { p.X -= WorldW; wrapped = true; }
        if (p.Y < 0) { p.Y += WorldH; wrapped = true; }
        if (p.Y >= WorldH) { p.Y -= WorldH; wrapped = true; }
        if (wrapped) Position = p;
    }
}
