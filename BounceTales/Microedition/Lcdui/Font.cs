namespace BounceTales.Microedition.Lcdui;

public class Font
{
    public enum Style
    {
        PLAIN = 0,
        BOLD = 1,
        ITALIC = 2,
        UNDERLINED = 4
    }

    public enum Size
    {
        SMALL = 8,
        MEDIUM = 0,
        LARGE = 16
    }

    public enum Face
    {
        SYSTEM = 0,
        MONOSPACE = 32,
        PROPORTIONAL = 64
    }

    private readonly Face _face;
    private readonly Style _style;
    private readonly Size _size;

    protected Font(Face face, Style style, Size size)
    {
        _face = face;
        _style = style;
        _size = size;
    }

    public static Font GetFont(Face face, Style style, Size size)
    {
        return new Font(face, style, size);
    }

    public int GetHeight()
    {
        return 14;
    }

    public int CharWidth(char ch)
    {
        return 8;
    }

    public int StringWidth(string str)
    {
        if (str == null)
            return 0;
        return str.Length * 8;
    }

    public int SubstringWidth(string str, int offset, int len)
    {
        if (str == null)
            return 0;
        return Math.Min(str.Length, len) * 8;
    }

    internal void DrawChar(Graphics g, int posX, int posY, char c)
    {
        int charIndex = Array.IndexOf(PixelFontData.CP437Lookup, c);
        if (charIndex < 0)
            return;
        for (int y = 0; y < 14; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int finalX = posX + x;
                int finalY = posY + y;
                if (_style == Style.ITALIC)
                    finalX -= y / 4;
                int set = PixelFontData.Font8x14[charIndex, y] & 1 << x;
                if (set != 0)
                {
                    g.DrawPixel(finalX, finalY);
                    if (_style == Style.BOLD)
                        g.DrawPixel(finalX + 1, finalY);
                }
            }
        }
    }
}
