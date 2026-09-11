using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class EggObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 9;

    public override void Initialize()
    {
        BBox.MinX = LP32.FP32ToLP32(-45f);
        BBox.MaxX = LP32.FP32ToLP32(45f);
        BBox.MinY = LP32.FP32ToLP32(-45f);
        BBox.MaxY = LP32.FP32ToLP32(45f);
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        Matrix.MultMatrices(rootMatrix, tmpObjMatrix, out Matrix temp);
        GameRuntime.DrawImageRes(temp.TranslationX >> 16, temp.TranslationY >> 16, 208);
        DebugDraw(graphics, 0xFFBF00, rootMatrix);
    }
}
