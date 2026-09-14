using Godot;
using System.Collections.Generic;

namespace BounceTales.Godot.UI;

[GlobalClass]
public partial class TouchGrabber : Node
{
    public static readonly Dictionary<int, Vector2> Touches = [];

    public override void _EnterTree()
    {
        Touches.Clear();
    }

    public override void _ExitTree()
    {
        Touches.Clear();
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
                RegisterTouch(touch.Index, touch.Position);
            else
                Touches.Remove(touch.Index);
        }
        if (e is InputEventScreenDrag drag)
            RegisterTouch(drag.Index, drag.Position);
    }

    private static void RegisterTouch(int index, Vector2 position)
    {
        Touches[index] = position;
    }
}
