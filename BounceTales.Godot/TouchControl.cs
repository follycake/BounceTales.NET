using Godot;

namespace BounceTales.Godot;

public partial class TouchControl : Control
{
    [Export] public StringName Action;

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventScreenTouch touch)
        {
            Input.ParseInputEvent(new InputEventAction
            {
                Action = Action,
                Device = touch.Device,
                Pressed = touch.Pressed
            });
        }
    }
}
