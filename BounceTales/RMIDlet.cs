using BounceTales.Microedition;
using BounceTales.Platform;
using BounceTales.Platform.Software;

namespace BounceTales;

public sealed class RMIDlet : IDisposable
{
    public const int DefaultScreenWidth = 240;
    public const int DefaultScreenHeight = 320;
    
    public ISystemProvider System { get; set; }
    public IGraphicsProvider Graphics { get; set; }
    public IAudioProvider Audio { get; set; }

    public event Action Paused;

    internal void Initialize()
    {
        System?.Initialize();
        Graphics?.Initialize();
        Audio?.Initialize();
    }

    public void Start()
    {
        if (GameRuntime.MidLet == null)
        {
            System ??= new DefaultSystemProvider();
            Graphics ??= new SoftwareGraphicsProvider();

            GameRuntime.MidLet = this;
            GameRuntime.SetState(GameState.INIT);
        }
        else
            GameRuntime.SetState(GameState.RUN);
    }

    public bool Update()
    {
        return GameRuntime.Update();
    }

    public void Pause()
    {
        GameRuntime.SetState(GameState.PAUSE);
        Paused?.Invoke();
    }

    public void RequestQuit()
    {
        GameRuntime.Quit();
    }

    public void Dispose()
    {
        System?.Dispose();
        Graphics?.Dispose();
        Audio?.Dispose();
        
        GameRuntime.Quit();
        GameRuntime.Shutdown();
    }
}
