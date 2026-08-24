using BounceTales.Microedition.Lcdui;
using System.Numerics;
using Rectangle = System.Drawing.Rectangle;
using Rl = Raylib_cs;

namespace BounceTales.Raylib;

public class RaylibGraphics(RaylibRenderImage image) : Graphics
{
    private static RaylibGraphics _current;

    public override int ClipX => _clipRect.X;
    public override int ClipY => _clipRect.Y;
    public override int ClipWidth => _clipRect.Width;
    public override int ClipHeight => _clipRect.Height;

    internal int _scale = 1;
    private readonly RaylibRenderImage _image = image;
    private Rectangle _clipRect = new(0, 0, image.Width, image.Height);

    internal static void EndDrawing()
    {
        _current?.End();
    }

    // TODO: We actually know when a Graphics object is set as the current one. Look at GameRuntime.SetGraphics(), we could make it more efficient with that.
    private void Begin()
    {
        if (_current != this)
        {
            _current?.End();
            Rl.Raylib.BeginTextureMode(_image._renderTexture);
            Rl.Rlgl.SetBlendFactorsSeparate(Rl.Rlgl.SRC_ALPHA, Rl.Rlgl.ONE_MINUS_SRC_ALPHA, Rl.Rlgl.ONE, Rl.Rlgl.ONE, Rl.Rlgl.FUNC_ADD, Rl.Rlgl.MAX);
            Rl.Raylib.BeginBlendMode(Rl.BlendMode.CustomSeparate);
            Rl.Raylib.BeginScissorMode(ClipX * _scale, ClipY * _scale, ClipWidth * _scale, ClipHeight * _scale);
            Rl.Rlgl.PushMatrix();
            Rl.Rlgl.Scalef(_scale, _scale, 1f);
            _current = this;
        }
    }

    private void End()
    {
        if (_current == this)
        {
            Rl.Rlgl.PopMatrix();
            Rl.Raylib.EndScissorMode();
            Rl.Raylib.EndBlendMode();
            Rl.Raylib.EndTextureMode();
            _current = null;
        }
    }

    private static Rl.Color RlColor(Color32 color)
    {
        return new Rl.Color(color.R, color.G, color.B, color.A);
    }

    public override void SetClip(int x, int y, int width, int height)
    {
        //End();
        _clipRect = Rectangle.Intersect(new Rectangle(x, y, width, height), new Rectangle(0, 0, _image.Width, _image.Height));
        if (_current == this)
            Rl.Raylib.BeginScissorMode(ClipX * _scale, ClipY * _scale, ClipWidth * _scale, ClipHeight * _scale);
    }

    public override void DrawPixel(int x, int y, Color32 color)
    {
        Begin();
        Rl.Raylib.DrawPixel(x, y, RlColor(color));
    }

    public override void DrawRegion(Image src, int xSrc, int ySrc, int width, int height, Sprite.Transform transform, int xDst, int yDst, Anchor anchor)
    {
        if (src is RaylibImage img)
        {
            xDst += AnchorX(anchor, width);
            yDst += AnchorY(anchor, height);

            Begin();
            if (transform != Sprite.Transform.NONE)
            {
                Matrix3x2 matrix = Sprite.GetMatrix(width, height, transform, out _, out _);
                matrix *= Matrix3x2.CreateTranslation(xDst, yDst);
                matrix *= Matrix3x2.CreateScale(_scale);
                Rl.Rlgl.PushMatrix();
                Rl.Rlgl.LoadIdentity();
                Rl.Rlgl.MultMatrixf(Matrix4x4.Transpose(new Matrix4x4(matrix)));
                Rl.Raylib.DrawTextureRec(img._texture, new(xSrc, ySrc, width, height), Vector2.Zero, Rl.Color.White);
                Rl.Rlgl.PopMatrix();
            }
            else
                Rl.Raylib.DrawTextureRec(img._texture, new(xSrc, ySrc, width, height), new(xDst, yDst), Rl.Color.White);
        }
    }

    public override void DrawRGB(ReadOnlySpan<Color32> rgbData, int x, int y, int width, int height)
    {
        using RaylibImage image = new(rgbData, width, height);
        DrawRegion(image, 0, 0, width, height, Sprite.Transform.NONE, x, y, Anchor.TOP | Anchor.LEFT);
        End();
    }

    public override void DrawRect(int x, int y, int width, int height)
    {
        Begin();
        Rl.Raylib.DrawRectangleLines(x, y, width, height, RlColor(Color));
    }

    public override void FillRect(int x, int y, int width, int height, Color32 color)
    {
        Begin();
        Rl.Raylib.DrawRectangle(x, y, width, height, RlColor(color));
    }

    public override void FillArc(int x, int y, int width, int height, int startAngle, int arcAngle, Color32 color)
    {
        float centerX = x + width / 2f;
        float centerY = y + height / 2f;
        float radius = width / 2f;
        float scaleY = (float)height / width;

        Begin();
        Rl.Rlgl.PushMatrix();
        Rl.Rlgl.Translatef(centerX, centerY, 0f);
        Rl.Rlgl.Scalef(1f, scaleY, 1f);
        Rl.Raylib.DrawCircleSector(Vector2.Zero, radius, startAngle, startAngle + arcAngle, 16, RlColor(color));
        Rl.Rlgl.PopMatrix();
    }

    public override void FillTriangle(Vector2I p1, Vector2I p2, Vector2I p3, Color32 color)
    {
        Begin();
        Rl.Raylib.DrawTriangle(p1, p2, p3, RlColor(color));
    }
}
