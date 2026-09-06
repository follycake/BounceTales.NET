using System.Numerics;

namespace BounceTales.Microedition.Lcdui;

public static class Sprite
{
    // Displays as clockwise visually
    public enum Transform
    {
        NONE = 0,
        ROT90 = 5,
        ROT180 = 3,
        ROT270 = 6,
        MIRROR = 2,
        MIRROR_ROT90 = 7,
        MIRROR_ROT180 = 1,
        MIRROR_ROT270 = 4
    }

    // https://github.com/hex007/freej2me/blob/master/src/org/recompile/mobile/PlatformImage.java
    public static Matrix3x2 GetMatrix(int width, int height, Transform transform, out int outWidth, out int outHeight)
    {
        Matrix3x2 matrix = Matrix3x2.Identity;
        outWidth = width;
        outHeight = height;
        switch (transform)
        {
            case Transform.ROT90:
                matrix *= Matrix3x2.CreateRotation(MathF.PI / 2f);
                matrix *= Matrix3x2.CreateTranslation(height, 0f);
                outWidth = height;
                outHeight = width;
                break;
            case Transform.ROT180:
                matrix *= Matrix3x2.CreateRotation(MathF.PI);
                matrix *= Matrix3x2.CreateTranslation(width, height);
                break;
            case Transform.ROT270:
                matrix *= Matrix3x2.CreateRotation(MathF.PI * 3f / 2f);
                matrix *= Matrix3x2.CreateTranslation(0f, width);
                outWidth = height;
                outHeight = width;
                break;
            case Transform.MIRROR:
                matrix *= Matrix3x2.CreateScale(-1f, 1f);
                matrix *= Matrix3x2.CreateTranslation(width, 0f);
                break;
            case Transform.MIRROR_ROT90:
                matrix *= Matrix3x2.CreateScale(-1f, 1f);
                matrix *= Matrix3x2.CreateTranslation(width, 0f);
                matrix *= Matrix3x2.CreateRotation(MathF.PI / 2f);
                matrix *= Matrix3x2.CreateTranslation(height, 0f);
                outWidth = height;
                outHeight = width;
                break;
            case Transform.MIRROR_ROT180:
                matrix *= Matrix3x2.CreateRotation(MathF.PI);
                matrix *= Matrix3x2.CreateTranslation(width, height);
                matrix *= Matrix3x2.CreateScale(-1f, 1f);
                matrix *= Matrix3x2.CreateTranslation(width, 0f);
                break;
            case Transform.MIRROR_ROT270:
                matrix *= Matrix3x2.CreateScale(-1f, 1f);
                matrix *= Matrix3x2.CreateTranslation(width, 0f);
                matrix *= Matrix3x2.CreateRotation(MathF.PI * 3f / 2f);
                matrix *= Matrix3x2.CreateTranslation(0f, width);
                outWidth = height;
                outHeight = width;
                break;
        }
        return matrix;
    }
}
