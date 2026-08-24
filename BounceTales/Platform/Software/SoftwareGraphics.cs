using System.Numerics;
using BounceTales.Microedition.Lcdui;
using Rectangle = System.Drawing.Rectangle;

namespace BounceTales.Platform.Software;

public class SoftwareGraphics(SoftwareImage image) : Graphics
{
    public override int ClipX => _clipRect.X;
    public override int ClipY => _clipRect.Y;
    public override int ClipWidth => _clipRect.Width;
    public override int ClipHeight => _clipRect.Height;

    private readonly SoftwareImage _image = image;
    private Rectangle _clipRect = new(0, 0, image.Width, image.Height);

    public override void SetClip(int x, int y, int width, int height)
    {
        _clipRect = Rectangle.Intersect(new Rectangle(x, y, width, height), new Rectangle(0, 0, _image.Width, _image.Height));
    }

    public override void DrawPixel(int x, int y, Color32 color)
    {
        if (!_clipRect.Contains(x, y))
            return;
        _image[x, y] = color;
    }

    public override void DrawRegion(Image src, int xSrc, int ySrc, int width, int height, Sprite.Transform transform, int xDst, int yDst, Anchor anchor)
    {
        if (src is SoftwareImage img)
        {
            xDst += AnchorX(anchor, width);
            yDst += AnchorY(anchor, height);

            if (transform != Sprite.Transform.NONE)
            {
                Rectangle srcRect = Rectangle.Intersect(new Rectangle(xSrc, ySrc, width, height), new Rectangle(0, 0, src.Width, src.Height));
                Matrix3x2 matrix = Sprite.GetMatrix(srcRect.Width, srcRect.Height, transform, out width, out height);
                Matrix3x2.Invert(matrix * Matrix3x2.CreateTranslation(xDst, yDst), out matrix);

                Rectangle dstRect = Rectangle.Intersect(new Rectangle(xDst, yDst, width, height), _clipRect);
                for (int y = dstRect.Y; y < dstRect.Bottom; y++)
                {
                    for (int x = dstRect.X; x < dstRect.Right; x++)
                    {
                        Vector2 sample = Vector2.Transform(new Vector2(x, y), matrix);
                        Color32 col = img[xSrc + (int)sample.X, ySrc + (int)sample.Y];
                        _image[x, y] = Color32.AlphaBlend(_image[x, y], col);
                    }
                }
            }
            else
            {
                Rectangle srcRect = Rectangle.Intersect(new Rectangle(xSrc, ySrc, width, height), new Rectangle(0, 0, src.Width, src.Height));
                Rectangle dstRect = Rectangle.Intersect(new Rectangle(xDst, yDst, srcRect.Width, srcRect.Height), _clipRect);
                for (int y = dstRect.Y; y < dstRect.Bottom; y++)
                {
                    for (int x = dstRect.X; x < dstRect.Right; x++)
                        _image[x, y] = Color32.AlphaBlend(_image[x, y], img[xSrc + (x - xDst), ySrc + (y - yDst)]);
                }
            }
        }
    }

    public override void DrawRGB(ReadOnlySpan<Color32> rgbData, int x, int y, int width, int height)
    {
        Rectangle rect = Rectangle.Intersect(new Rectangle(x, y, width, height), _clipRect);
        for (int py = rect.Y; py < rect.Bottom; py++)
        {
            for (int px = rect.X; px < rect.Right; px++)
                _image[px, py] = Color32.AlphaBlend(_image[px, py], rgbData[px - x + (py - y) * width]);
        }
    }

    public override void FillRect(int x, int y, int width, int height, Color32 color)
    {
        Rectangle rect = Rectangle.Intersect(new Rectangle(x, y, width, height), _clipRect);
        for (int py = rect.Y; py < rect.Bottom; py++)
        {
            for (int px = rect.X; px < rect.Right; px++)
                _image[px, py] = Color32.AlphaBlend(_image[px, py], color);
        }
    }

    public override void FillArc(int x, int y, int width, int height, int startAngle, int arcAngle, Color32 color)
    {
        Rectangle bounds = new(x, y, width, height);
        bounds.Intersect(_clipRect);

        float centerX = x + width / 2f;
        float centerY = y + height / 2f;
        float radius = width / 2f;
        float scaleY = (float)width / height;

        int halfArc = arcAngle / 2;
        int centerAngle = PosMod(startAngle + halfArc, 360);

        for (int py = bounds.Y; py < bounds.Bottom; py++)
        {
            for (int px = bounds.X; px < bounds.Right; px++)
            {
                float offsetX = px - centerX;
                float offsetY = (py - centerY) * scaleY;
                if (offsetX * offsetX + offsetY * offsetY > radius * radius)
                    continue;
                int angle = (int)float.RadiansToDegrees(MathF.Atan2(offsetY, offsetX));
                int diff = (angle - centerAngle + 180 + 360) % 360 - 180;
                if (diff > halfArc || diff < -halfArc)
                    continue;
                _image[px, py] = color;
            }
        }
    }

    public override void FillTriangle(Vector2I p1, Vector2I p2, Vector2I p3, Color32 color)
    {
        Vector2I min = Vector2I.Min(Vector2I.Min(p1, p2), p3);
        Vector2I max = Vector2I.Max(Vector2I.Max(p1, p2), p3);
        Rectangle bounds = Rectangle.Intersect(new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y), _clipRect);
        for (int y = bounds.Y; y < bounds.Bottom; y++)
        {
            for (int x = bounds.X; x < bounds.Right; x++)
            {
                if (!InsideTriangle(x, y, p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y))
                    continue;
                _image[x, y] = Color32.AlphaBlend(_image[x, y], color);
            }
        }
    }

    private static int PosMod(int x, int y)
    {
        int value = x % y;
        if (value < 0 && y > 0 || value > 0 && y < 0)
            value += y;
        return value;
    }

    // https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
    private static bool InsideTriangle(int pX, int pY, int p0X, int p0Y, int p1X, int p1Y, int p2X, int p2Y)
    {
        int dX = pX - p2X;
        int dY = pY - p2Y;
        int dX21 = p2X - p1X;
        int dY12 = p1Y - p2Y;
        int D = dY12 * (p0X - p2X) + dX21 * (p0Y - p2Y);
        int s = dY12 * dX + dX21 * dY;
        int t = (p2Y - p0Y) * dX + (p0X - p2X) * dY;
        if (D < 0)
            return s <= 0 && t <= 0 && s + t >= D;
        return s >= 0 && t >= 0 && s + t <= D;
    }
}
