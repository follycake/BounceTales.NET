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
        LoadObjectMatrixToTarget(out TmpObjMatrix);
        Matrix.MultMatrices(rootMatrix, TmpObjMatrix, out Matrix.Temp);
        GameRuntime.DrawImageRes(Matrix.Temp.TranslationX >> 16, Matrix.Temp.TranslationY >> 16, 208);
        DebugDraw(graphics, 0xFFBF00, rootMatrix);
    }
}
