using BounceTales.Microedition.Lcdui;

namespace BounceTales.Platform.Software;

public class SoftwareGraphicsProvider : BasicGraphicsProvider
{
    public override Image CreateImage(ReadOnlySpan<Color> data, int width, int height)
    {
        return new SoftwareImage(data.ToArray(), width, height);
    }

    public override Image CreateRenderImage(int width, int height)
    {
        return new SoftwareImage(new Color[width * height], width, height);
    }
}
