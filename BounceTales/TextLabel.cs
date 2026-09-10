using BounceTales.Ext.Rsc;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class TextLabel
{
    public static int F0a => GameRuntime.CurrentWidth >>> 5;
    //private static readonly char[] cArr = ['\n', ' ', '-'];

    internal string rawText;
    private readonly bool f2a;
    private readonly string[] lines;

    public int TextBlockWidth;
    public int TextBlockHeight;

    private readonly int iconImageId;
    private readonly int flags;
    private readonly int fontId;
    private readonly int shadowType;
    private int f10h;
    private int f11i;
    private int lineCount;
    private readonly int iconHeight;
    private readonly int iconWidth;

    public TextLabel(string text, int maxWidth, int fontId, int flags, int iconImageId)
    {
        this.flags = flags;
        rawText = text;
        this.fontId = fontId;
        switch (this.flags & 3)
        {
            case 0:
                shadowType = 1;
                break;
            case 1:
                shadowType = 2;
                break;
            case 2:
                shadowType = 3;
                break;
        }
        if (iconImageId >= 0)
        {
            this.iconImageId = iconImageId;
            iconWidth = GameRuntime.GetImageMapParam(iconImageId, ImageMap.Param.WIDTH) + F0a;
            iconHeight = GameRuntime.GetImageMapParam(iconImageId, ImageMap.Param.HEIGHT);
        }
        else
        {
            this.iconImageId = -1;
            iconWidth = 0;
            iconHeight = 0;
        }
        f2a = maxWidth > iconWidth && (this.flags & 128) != 0 && (this.flags & 48) != 32;
        bool dynamicWidth = (this.flags & 256) != 0;
        rawText = text ?? "";
        lines = GameRuntime.PrepareStringLines(rawText, maxWidth, this.fontId, f2a, iconWidth, iconHeight, dynamicWidth);
        ParseDrawParams(lines[0]);
    }

    private void ParseDrawParams(string str)
    {
        int[] gapIndices = new int[4];
        if (str != null)
        {
            int i = 0;
            for (int scanIdx = 0; scanIdx < str.Length; scanIdx++)
            {
                if (str[scanIdx] == ' ')
                {
                    gapIndices[i] = scanIdx;
                    i++;
                }
            }
            if (!int.TryParse(str[..gapIndices[0]], out lineCount))
                lineCount = 0;
            if (!int.TryParse(str[(gapIndices[0] + 1)..gapIndices[1]], out f10h))
                f10h = 0;
            if (!int.TryParse(str[(gapIndices[1] + 1)..gapIndices[2]], out f11i))
                f11i = 0;
            if (!int.TryParse(str[(gapIndices[2] + 1)..gapIndices[3]], out TextBlockWidth))
                TextBlockWidth = 0;
            if (!int.TryParse(str[(gapIndices[3] + 1)..], out TextBlockHeight))
                TextBlockHeight = 0;
        }
        else
        {
            lineCount = 0;
            f10h = 0;
            f11i = 0;
            TextBlockWidth = 0;
            TextBlockHeight = 0;
        }
    }

    public void Draw(int startX, int startY, int textColor, int shadowColor)
    {
        int iconXOffset;
        Graphics.Anchor iconAnchor;
        int i7;
        int i8;
        //int i9;
        int i10 = lineCount;
        GameRuntime.SetTextStyle(fontId, shadowType);
        GameRuntime.SetTextColor(0, textColor);
        GameRuntime.SetTextColor(1, shadowColor);
        int localLineCount = i10 > lineCount ? lineCount : i10;
        if ((flags & 48) == 32) // icon centered in label
        {
            iconXOffset = TextBlockWidth / 2;
            iconAnchor = Graphics.Anchor.HCENTER | Graphics.Anchor.TOP;
        }
        else
        {
            int i12 = (flags & 12) == 8 ? (TextBlockWidth - f11i) / 2 : 0;
            if ((flags & 48) == 0)
            {
                iconAnchor = Graphics.Anchor.TOP | Graphics.Anchor.LEFT;
                iconXOffset = i12;
            }
            else
            {
                iconXOffset = TextBlockWidth - i12;
                iconAnchor = Graphics.Anchor.TOP | Graphics.Anchor.RIGHT;
            }
        }
        bool z = (flags & 48) == 0;
        int i13 = 0;
        int i14 = 0;
        switch (flags & 12)
        {
            case 0:
                i14 = 20;
                i13 = 20;
                if (!z)
                {
                    i7 = 0;
                    i8 = 0;
                    break;
                }
                i7 = 0;
                i8 = iconWidth;
                break;
            case 4:
                i14 = 24;
                int i15 = TextBlockWidth;
                i13 = 24;
                if (!z)
                {
                    i7 = i15;
                    i8 = TextBlockWidth - iconWidth;
                    break;
                }
                i7 = i15;
                i8 = TextBlockWidth;
                break;
            case 8:
                i14 = 17;
                int i16 = TextBlockWidth / 2;
                if (!z)
                {
                    i13 = 24;
                    i7 = i16;
                    i8 = TextBlockWidth - (TextBlockWidth - f11i) / 2 - iconWidth;
                    break;
                }
                i13 = 20;
                i7 = i16;
                i8 = (TextBlockWidth - f11i) / 2 + iconWidth;
                break;
            default:
                i7 = 0;
                i8 = 0;
                break;
        }
        if (iconImageId >= 0)
            GameRuntime.DrawImageResAnchored(startX + iconXOffset, startY, iconImageId, iconAnchor);
        if (!f2a)
            startY += iconHeight;
        int yoffs = GameRuntime.GetFontHeight(fontId) * 0;
        int lineIndex = 0;
        while (lineIndex < localLineCount)
        {
            if (lines[lineIndex + 1] != null)
            {
                if (lineIndex < f10h)
                    GameRuntime.DrawText(lines[lineIndex + 1], 0, lines[lineIndex + 1].Length, startX + i8, startY + yoffs, i13);
                else
                    GameRuntime.DrawText(lines[lineIndex + 1], 0, lines[lineIndex + 1].Length, startX + i7, startY + yoffs, i14);
                yoffs += GameRuntime.GetFontHeight(fontId);
            }
            lineIndex++;
        }
    }
}
