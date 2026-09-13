using BounceTales.Microedition.Lcdui;

namespace BounceTales.Platform;

public interface IGraphicsProvider : IProvider
{
    int ScreenWidth { get; }
    int ScreenHeight { get; }

    Graphics GetGraphics();
    void FlushGraphics();

    Image CreateImage(ReadOnlySpan<Color32> data, int width, int height);
    Image CreateRenderImage(int width, int height);
}

public abstract class BasicGraphicsProvider : IGraphicsProvider
{
    public int ScreenWidth { get; set; } = RMIDlet.DefaultScreenWidth;
    public int ScreenHeight { get; set; } = RMIDlet.DefaultScreenHeight;

    protected Image _renderTarget;
    protected Graphics _graphics;

    public virtual void Initialize()
    {
        _renderTarget = CreateRenderImage(ScreenWidth, ScreenHeight);
        _graphics = _renderTarget.GetGraphics();
    }

    public virtual Graphics GetGraphics()
    {
        return _graphics;
    }

    public virtual void FlushGraphics()
    {
        int w = ScreenWidth;
        int h = ScreenHeight;
        if (_renderTarget.Width != w || _renderTarget.Height != h)
        {
            _renderTarget?.Dispose();
            _renderTarget = CreateRenderImage(w, h);
            _graphics = _renderTarget.GetGraphics();
        }
    }

    public abstract Image CreateImage(ReadOnlySpan<Color32> data, int width, int height);
    public abstract Image CreateRenderImage(int width, int height);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _renderTarget?.Dispose();
        _renderTarget = null;
        _graphics = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BasicGraphicsProvider()
    {
        Dispose(false);
    }
}
