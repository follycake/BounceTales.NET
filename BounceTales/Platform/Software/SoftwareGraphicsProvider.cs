using BounceTales.Microedition.Lcdui;

namespace BounceTales.Platform.Software;

public class SoftwareGraphicsProvider : BasicGraphicsProvider
{
    public SoftwareImage ScreenImage => (SoftwareImage)_renderTarget;
    
    public override Image CreateImage(ReadOnlySpan<Color32> data, int width, int height)
    {
        return new SoftwareImage([.. data], width, height);
    }

    public override Image CreateRenderImage(int width, int height)
    {
        return new SoftwareImage(new Color32[width * height], width, height);
    }
}
