using BounceTales.Microedition.Lcdui;

namespace BounceTales.Platform.Software;

public class SoftwareImage(Color32[] data, int width, int height) : Image
{
    public override int Width { get; } = width;
    public override int Height { get; } = height;
    public Color32[] Data { get; } = data;

    public ref Color32 this[int i] => ref Data[i];
    public ref Color32 this[int x, int y] => ref Data[y * Width + x];

    public override Graphics GetGraphics()
    {
        return new SoftwareGraphics(this);
    }

    public override void GetRGB(Span<Color32> rgbData)
    {
        Data.CopyTo(rgbData);
    }
}
