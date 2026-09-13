using BounceTales.Microedition.Lcdui;
using System;
using System.Collections.Generic;
using System.Numerics;
using Gd = Godot;

namespace BounceTales.Godot;

public class GodotGraphics : Graphics
{
    public static readonly Gd.StringName ClipRect = "clip_rect";
    
    public override int ClipX => _clipRect.Position.X;
    public override int ClipY => _clipRect.Position.Y;
    public override int ClipWidth => _clipRect.Size.X;
    public override int ClipHeight => _clipRect.Size.Y;

    private readonly GodotRenderImage _image;
    private readonly Gd.Node2D _canvas;
    private readonly List<Gd.Rid> _canvasItems = [];
    private Gd.Rid _canvasItem;
    private Gd.Rect2I _clipRect;
    
    public GodotGraphics(GodotRenderImage image)
    {
        _image = image;
        _canvas = _image._canvas;
        _clipRect = new Gd.Rect2I(0, 0, image.Width, image.Height);
        Clear();
    }

    public void Clear()
    {
        foreach (Gd.Rid canvasItem in _canvasItems)
            Gd.RenderingServer.FreeRid(canvasItem);
        _canvasItems.Clear();
        _canvasItem = default;
        _clipRect = default;
    }

    public void Begin()
    {
        if (_canvasItems.Count <= 0)
            SetClip(0, 0, _image.Width, _image.Height);
    }

    public static Gd.Color GdColor(Color32 color) => Gd.Color.Color8(color.R, color.G, color.B, color.A);
    
    public override void SetClip(int x, int y, int width, int height)
    {
        Gd.Rect2I newRect = new(x, y, width, height);
        if (_clipRect != newRect)
        {
            _clipRect = newRect;
            _canvasItem = Gd.RenderingServer.CanvasItemCreate();
            Gd.RenderingServer.CanvasItemSetParent(_canvasItem, _canvas.GetCanvasItem());
            Gd.RenderingServer.CanvasItemSetMaterial(_canvasItem, Game.ClipMaterial.GetRid());
            Gd.RenderingServer.CanvasItemSetInstanceShaderParameter(_canvasItem, ClipRect, new Gd.Vector4(x * _canvas.Scale.X, y * _canvas.Scale.Y, width * _canvas.Scale.X, height * _canvas.Scale.Y));
            _canvasItems.Add(_canvasItem);
        }
    }
    
    public override void DrawPixel(int x, int y, Color32 color)
    {
        FillRect(x, y, 1, 1, color);
    }
    
    public override void DrawRegion(Image src, int xSrc, int ySrc, int width, int height, Sprite.Transform transform, int xDst, int yDst, Anchor anchor)
    {
        if (src is GodotImage image)
        {
            Begin();
            Matrix3x2 matrix = Sprite.GetMatrix(width, height, transform, out _, out _);
            matrix *= Matrix3x2.CreateTranslation(xDst, yDst);
            Gd.RenderingServer.CanvasItemAddSetTransform(_canvasItem, new Gd.Transform2D(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32));
            Gd.RenderingServer.CanvasItemAddTextureRectRegion(_canvasItem, new Gd.Rect2(0f, 0f, width, height), image._texture.GetRid(), new Gd.Rect2(xSrc, ySrc, width, height));
            Gd.RenderingServer.CanvasItemAddSetTransform(_canvasItem, Gd.Transform2D.Identity);
        }
    }

    public override void DrawRGB(ReadOnlySpan<Color32> rgbData, int x, int y, int width, int height)
    {
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
                DrawPixel(x + px, y + py, rgbData[px + width * py]);
        }
    }

    public override void FillRect(int x, int y, int width, int height, Color32 color)
    {
        Begin();
        Gd.RenderingServer.CanvasItemAddRect(_canvasItem, new Gd.Rect2(x, y, width, height).Abs(), GdColor(color));
    }
    
    public override void FillArc(int x, int y, int width, int height, int startAngle, int arcAngle, Color32 color)
    {
        Begin();
        const int resolution = 32;
        
        Span<int> indices = stackalloc int[resolution * 3];
        Span<Gd.Vector2> points = stackalloc Gd.Vector2[resolution + 1];
        
        Gd.Vector2 radius = new(width * 0.5f, height * 0.5f);
        Gd.Vector2 center = new Gd.Vector2(x, y) + radius;

        int lastPoint = points.Length - 1;
        points[lastPoint] = center;
        
        for (int i = 0; i < resolution; i++)
        {
            points[i] = center + Gd.Vector2.FromAngle(float.DegreesToRadians(startAngle + i / (resolution - 1f) * arcAngle)) * radius;
            indices[i * 3] = lastPoint;
            indices[i * 3 + 1] = i;
            indices[i * 3 + 2] = (i + 1) % resolution;
        }
        
        Gd.Color col = GdColor(color);
        Gd.RenderingServer.CanvasItemAddTriangleArray(_canvasItem, indices, points, new ReadOnlySpan<Gd.Color>(ref col), null, null, null, default, resolution);
    }
    
    public override void FillTriangle(Vector2I p1, Vector2I p2, Vector2I p3, Color32 color)
    {
        Begin();
        Span<Gd.Vector2> points = stackalloc Gd.Vector2[3];
        points[0] = new Gd.Vector2(p1.X, p1.Y);
        points[1] = new Gd.Vector2(p2.X, p2.Y);
        points[2] = new Gd.Vector2(p3.X, p3.Y);
        Gd.Color col = GdColor(color);
        Gd.RenderingServer.CanvasItemAddPrimitive(_canvasItem, points, new ReadOnlySpan<Gd.Color>(ref col), null, default);
    }
}
