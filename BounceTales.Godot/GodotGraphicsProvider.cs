using BounceTales.Microedition.Lcdui;
using BounceTales.Platform;
using System;
using System.Runtime.InteropServices;
using Gd = Godot;

namespace BounceTales.Godot;

public sealed class GodotGraphicsProvider : IGraphicsProvider
{
    public Game Game { get; }
    
    public int ScreenWidth
    {
        get => (int)(_screen.Width / Scale.X);
        set => Game.GetWindow().Size = new Gd.Vector2I((int)(value * Scale.X), (int)(ScreenHeight * Scale.Y));
    }
    
    public int ScreenHeight
    {
        get => (int)(_screen.Height / Scale.Y);
        set => Game.GetWindow().Size = new Gd.Vector2I((int)(ScreenWidth * Scale.X), (int)(value * Scale.Y));
    }

    public Gd.Vector2 Scale
    {
        get => _canvas.Scale;
        set => _canvas.Scale = value;
    }

    private readonly GodotRenderImage _screen;
    private readonly Gd.Node2D _canvas;
    
    public GodotGraphicsProvider(Game game)
    {
        Game = game;
        _screen = new GodotRenderImage(Game.GetViewport(), Game.GetNode<Gd.Node2D>("%Canvas"), false);
        _canvas = _screen._canvas;
    }

    public void Initialize()
    {
    }
    
    public void FlushGraphics()
    {
    }
    
    public Graphics GetGraphics()
    {
        return _screen.GetGraphics();
    }
    
    public Image CreateImage(ReadOnlySpan<Color32> data, int width, int height)
    {
        using Gd.Image image = Gd.Image.CreateFromData(width, height, false, Gd.Image.Format.Rgba8, MemoryMarshal.AsBytes(data));
        return new GodotImage(Gd.ImageTexture.CreateFromImage(image));
    }
    
    public Image CreateRenderImage(int width, int height)
    {
        Gd.SubViewport viewport = new()
        {
            Size = new Gd.Vector2I(width, height),
            CanvasItemDefaultTextureFilter = Gd.Viewport.DefaultCanvasItemTextureFilter.Nearest,
            RenderTargetUpdateMode = Gd.SubViewport.UpdateMode.Always
        };
        Gd.Node2D canvas = new()
        {
            Name = "Canvas"
        };
        viewport.AddChild(canvas);
        Game.AddChild(viewport);
        return new GodotRenderImage(viewport, canvas, true);
    }
    
    public void Dispose()
    {
        _screen.Dispose();
    }
}
