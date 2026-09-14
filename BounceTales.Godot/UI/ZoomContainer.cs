using Godot;
using System;

namespace BounceTales.Godot.UI;

[Tool, GlobalClass]
public partial class ZoomContainer : Container
{
    public Vector2 ReferenceSize = new(RMIDlet.DefaultScreenWidth, RMIDlet.DefaultScreenHeight);
    
    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
        {
            Vector2 ratio = Size / ReferenceSize;
            float scale = MathF.Min(ratio.X, ratio.Y);
            Rect2 rect = new(Vector2.Zero, Size / scale);
            foreach (Node node in GetChildren())
            {
                if (node is Control control)
                {
                    FitChildInRect(control, rect);
                    control.Scale = Vector2.One * scale;
                }
            }
        }
    }
}
