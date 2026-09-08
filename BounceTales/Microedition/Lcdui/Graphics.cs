namespace BounceTales.Microedition.Lcdui;

public abstract class Graphics
{
    [Flags]
    public enum Anchor
    {
        HCENTER = 1,
        VCENTER = 2,
        LEFT = 4,
        RIGHT = 8,
        TOP = 16,
        BOTTOM = 32,
        BASELINE = 64
    }

    public abstract int ClipX { get; }
    public abstract int ClipY { get; }
    public abstract int ClipWidth { get; }
    public abstract int ClipHeight { get; }

    public Font Font { get; private set; }
    public Color32 Color { get; private set; }

    // Ignores alpha
    // TODO: Reduce number of calls to SetColor(int RGB). Translate hex to the Color32 constructor.
    public void SetColor(int RGB)
    {
        Color = Color32.FromRGB(RGB);
    }

    public void SetColor(Color32 color)
    {
        Color = color;
    }

    public void SetFont(Font font)
    {
        Font = font;
    }

    public abstract void SetClip(int x, int y, int width, int height);

    public abstract void DrawPixel(int x, int y, Color32 color);

    public abstract void DrawRegion(Image src, int xSrc, int ySrc, int width, int height, Sprite.Transform transform, int xDst, int yDst, Anchor anchor);

    public abstract void DrawRGB(ReadOnlySpan<Color32> rgbData, int x, int y, int width, int height);

    public virtual void DrawSubstring(string str, int offset, int len, int x, int y, Anchor anchor) => DrawString(str.Substring(offset, len), x, y, anchor);

    public virtual void DrawString(string str, int x, int y, Anchor anchor)
    {
        x += AnchorX(anchor, Font.StringWidth(str));
        y += AnchorY(anchor, Font.GetHeight());
        foreach (char c in str)
        {
            Font.DrawChar(this, x, y, c);
            x += Font.CharWidth(c);
        }
    }

    public virtual void DrawRect(int x, int y, int width, int height)
    {
        FillRect(x, y, width, 1);
        FillRect(x, y + height - 1, width, 1);
        FillRect(x, y, 1, height);
        FillRect(x + width - 1, y, 1, height);
    }

    public abstract void FillRect(int x, int y, int width, int height, Color32 color);

    public abstract void FillArc(int x, int y, int width, int height, int startAngle, int arcAngle, Color32 color);

    public abstract void FillTriangle(Vector2I p1, Vector2I p2, Vector2I p3, Color32 color);

    public virtual void FillQuad(Vector2I p1, Vector2I p2, Vector2I p3, Vector2I p4, Color32 color)
    {
        FillTriangle(p3, p2, p1, color);
        FillTriangle(p1, p4, p3, color);
    }

    public void DrawPixel(int x, int y) => DrawPixel(x, y, Color);
    public void FillRect(int x, int y, int width, int height) => FillRect(x, y, width, height, Color);
    public void FillArc(int x, int y, int width, int height, int startAngle, int arcAngle) => FillArc(x, y, width, height, startAngle, arcAngle, Color);
    public void FillTriangle(Vector2I p1, Vector2I p2, Vector2I p3) => FillTriangle(p1, p2, p3, Color);

    protected static int AnchorX(Anchor anchor, int width)
    {
        if ((anchor & Anchor.HCENTER) != 0)
            return -(width / 2);
        if ((anchor & Anchor.RIGHT) != 0)
            return -width;
        return 0;
    }

    protected static int AnchorY(Anchor anchor, int height)
    {
        if ((anchor & Anchor.VCENTER) != 0)
            return -(height / 2);
        if ((anchor & Anchor.BOTTOM) != 0)
            return -height;
        if ((anchor & Anchor.BASELINE) != 0)
            return height;
        return 0;
    }
}
