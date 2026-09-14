using Godot;

namespace BounceTales.Godot.UI;

public partial class TouchControl : Control
{
    [Export] public StringName Action;
    private bool _lastPressed;

    public override void _Process(double delta)
    {
        Rect2 globalRect = GetGlobalRect();
        bool pressed = false;
        foreach (Vector2 pos in TouchGrabber.Touches.Values)
        {
            if (globalRect.HasPoint(pos))
            {
                pressed = true;
                break;
            }
        }
        if (pressed && !_lastPressed)
        {
            Input.ParseInputEvent(new InputEventAction
            {
                Action = Action,
                Pressed = true
            });
        }
        if (!pressed && _lastPressed)
        {
            Input.ParseInputEvent(new InputEventAction
            {
                Action = Action,
                Pressed = false
            });
        }
        _lastPressed = pressed;
    }
}
