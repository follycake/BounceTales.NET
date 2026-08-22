using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class CannonObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 7;

    private static readonly int[] CANNON_FRAME_LENGTHS = [500, 100, 300];
    private static readonly int CANNON_TOTAL_FRAMES = CANNON_FRAME_LENGTHS[0] + CANNON_FRAME_LENGTHS[1] + CANNON_FRAME_LENGTHS[2];
    private static readonly int FIRING_FRAME_LENGTH = CANNON_FRAME_LENGTHS[2];

    // Parameters - preset
    private sbyte power;

    // Parameters - calculated	
    private bool isFacingRight;
    internal AABB loadAABB;

    // State
    private int animCountdown;
    private short bounceObjId;
    private int reloadCooldown;

    public override int ReadData(byte[] data, int dataPos)
    {
        dataPos = base.ReadData(data, dataPos);
        bounceObjId = ReadShort(data, dataPos);
        dataPos += 2;
        power = (sbyte)data[dataPos++];
        reloadCooldown = 0;
        isFacingRight = LocalObjectMatrix.M00 >= 0;
        animCountdown = 0;
        return dataPos;
    }

    public override void Initialize()
    {
        BBox.MinX = -120 << 16;
        BBox.MaxX = 120 << 16;
        BBox.MinY = -120 << 16;
        BBox.MaxY = 120 << 16;
        loadAABB.MinX = -40 << 16;
        loadAABB.MaxX = 40 << 16;
        loadAABB.MinY = -40 << 16;
        loadAABB.MaxY = 40 << 16;
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        base.Draw(graphics, rootMatrix);
        if (animCountdown > 0)
        {
            int currentCannonFrame = CANNON_TOTAL_FRAMES - animCountdown;
            int frameStartFrames = 0;
            int cannonModelFrame = 0;
            for (int cannonFrmIdx = 0; cannonFrmIdx < 3 && currentCannonFrame >= CANNON_FRAME_LENGTHS[cannonFrmIdx] + frameStartFrames; cannonFrmIdx++)
            {
                frameStartFrames += CANNON_FRAME_LENGTHS[cannonFrmIdx];
                cannonModelFrame++;
            }
            float weight = (currentCannonFrame - frameStartFrames) / (float)CANNON_FRAME_LENGTHS[cannonModelFrame];
            float weightInv = 1.0f - weight;
            for (int i = 0; i < 3; i++)
            {
                GeometryObject lerpL = (GeometryObject)BounceGame.CannonModels[cannonModelFrame * 3 + i];
                GeometryObject morph = (GeometryObject)BounceGame.CannonModels[i + 9];
                GeometryObject lerpR = cannonModelFrame == 2 ? (GeometryObject)BounceGame.CannonModels[i] : (GeometryObject)BounceGame.CannonModels[(cannonModelFrame + 1) * 3 + i];
                for (int vertIdx = 0; vertIdx < morph.XCoordBuffer.Length; vertIdx++)
                {
                    morph.XCoordBuffer[vertIdx] = (int)(lerpL.XCoordBuffer[vertIdx] * weightInv + lerpR.XCoordBuffer[vertIdx] * weight);
                    morph.YCoordBuffer[vertIdx] = (int)(lerpL.YCoordBuffer[vertIdx] * weightInv + lerpR.YCoordBuffer[vertIdx] * weight);
                }
            }
        }
        LoadObjectMatrixToTarget(out TmpObjMatrix);
        Matrix.MultMatrices(rootMatrix, TmpObjMatrix, out Matrix.Temp);
        int imageX = Matrix.Temp.TranslationX >> 16;
        int imageY = Matrix.Temp.TranslationY >> 16;
        for (int i = 0; i < 3; i++)
        {
            GameObject model = BounceGame.CannonModels[i + 9];
            model.LocalObjectMatrix.TranslationX = LocalObjectMatrix.TranslationX;
            model.LocalObjectMatrix.TranslationY = LocalObjectMatrix.TranslationY;
            model.LocalObjectMatrix.M00 = LocalObjectMatrix.M00;
            model.LocalObjectMatrix.M10 = LocalObjectMatrix.M10;
            model.LocalObjectMatrix.M01 = LocalObjectMatrix.M01;
            model.LocalObjectMatrix.M11 = LocalObjectMatrix.M11;
            model.SetIsDirtyRecursive();
            model.Draw(graphics, rootMatrix);
        }
        GameRuntime.DrawImageRes(imageX, imageY, 48);
    }

    public override void OnPlayerContact()
    {
        if (reloadCooldown == 0 && BounceGame.GetControllerState() == BounceGame.Controller.NORMAL)
        {
            BounceGame.SetControllerState(BounceGame.Controller.CANNON);
            BounceGame.CurrentCannon = this;
            BounceObject bounce = (BounceObject)GetObjectRoot().SearchByObjId(bounceObjId);
            LoadObjectMatrixToTarget(out TmpObjMatrix);
            bounce.SetPosXY(TmpObjMatrix.TranslationX, TmpObjMatrix.TranslationY + 2293760);
            bounce.EnablePhysics = false;
            bounce.IsVisible = false;
        }
    }

    public override void UpdatePhysics()
    {
        base.UpdatePhysics();
        reloadCooldown -= GameRuntime.UpdateDelta;
        if (reloadCooldown < 0)
            reloadCooldown = 0;
        if (animCountdown > 0)
        {
            animCountdown -= GameRuntime.UpdateDelta;
            if (animCountdown <= FIRING_FRAME_LENGTH && BounceGame.GetControllerState() == BounceGame.Controller.CANNON)
            {
                BounceObject bounce = (BounceObject)GetObjectRoot().SearchByObjId(bounceObjId);
                bounce.LastXVelocity = 0.0f;
                bounce.LastYVelocity = 0.0f;
                bounce.CurXVelocity = power * LocalObjectMatrix.M00 >> 12;
                bounce.CurYVelocity = power * LocalObjectMatrix.M10 >> 12;
                bounce.IsGrounded = false;
                bounce.ReqSkipAccelStretch = true;
                bounce.EnablePhysics = true;
                bounce.IsVisible = true;
                bounce.TorqueX = 0.0f;
                bounce.TorqueY = 0.0f;
                BounceGame.SetControllerState(BounceGame.Controller.NORMAL);
                reloadCooldown = 500;
                LoadObjectMatrixToTarget(out TmpObjMatrix);
                Vector2I head = TmpObjMatrix.MulVector(120 << 16, 0);
                bounce.LocalObjectMatrix.Translation = head;
                BounceGame.CannonParticle.EmitBlast(10, head.X, head.Y, 800, 200, LocalObjectMatrix.M00, LocalObjectMatrix.M10, 30, 800, 200);
            }
            if (animCountdown <= 0)
            {
                animCountdown = 0;
                for (int meshIdx = 0; meshIdx < 3; meshIdx++)
                {
                    GeometryObject morph = (GeometryObject)BounceGame.CannonModels[meshIdx + 9];
                    GeometryObject baseVerts = (GeometryObject)BounceGame.CannonModels[meshIdx];
                    for (int vertIdx = 0; vertIdx < morph.XCoordBuffer.Length; vertIdx++)
                    {
                        morph.XCoordBuffer[vertIdx] = baseVerts.XCoordBuffer[vertIdx];
                        morph.YCoordBuffer[vertIdx] = baseVerts.YCoordBuffer[vertIdx];
                    }
                }
            }
        }
    }

    public void RotateUp()
    {
        if (animCountdown == 0)
        {
            Matrix.Temp.SetRotation(GameRuntime.UpdateDelta * 0.001f * 3.0f);
            Matrix.Temp.TranslationX = 0;
            Matrix.Temp.TranslationY = 0;
            LocalObjectMatrix.Mul(Matrix.Temp);
            if (isFacingRight && LocalObjectMatrix.M00 < 0)
            {
                // set rotation to 90 degrees, scaleX to 1
                LocalObjectMatrix.M00 = 0;
                LocalObjectMatrix.M10 = LP32.ONE;
                LocalObjectMatrix.M01 = -LP32.ONE;
                LocalObjectMatrix.M11 = 0;
            }
            if (!isFacingRight && LocalObjectMatrix.M00 > 0)
            {
                // set rotation to 90 degrees, scaleX to -1
                LocalObjectMatrix.M00 = 0;
                LocalObjectMatrix.M10 = LP32.ONE;
                LocalObjectMatrix.M01 = LP32.ONE;
                LocalObjectMatrix.M11 = 0;
            }
            SetIsDirtyRecursive();
        }
    }

    public void RotateDown()
    {
        if (animCountdown == 0)
        {
            Matrix.Temp.SetRotation(GameRuntime.UpdateDelta * 0.001f * -3.0f);
            Matrix.Temp.TranslationX = 0;
            Matrix.Temp.TranslationY = 0;
            LocalObjectMatrix.Mul(Matrix.Temp);
            if (isFacingRight && LocalObjectMatrix.M01 > 0)
            {
                // negative sine of angle > 0 -> angle is 180 to 360
                // set rotation to 0 degrees, scaleX to 1
                LocalObjectMatrix.M00 = LP32.ONE;
                LocalObjectMatrix.M10 = 0;
                LocalObjectMatrix.M01 = 0;
                LocalObjectMatrix.M11 = LP32.ONE;
            }
            if (!isFacingRight && LocalObjectMatrix.M01 < 0)
            {
                // angle is 0 to 180
                // set rotation to 0 degrees, scaleX to -1
                LocalObjectMatrix.M00 = -LP32.ONE;
                LocalObjectMatrix.M10 = 0;
                LocalObjectMatrix.M01 = 0;
                LocalObjectMatrix.M11 = LP32.ONE;
            }
            SetIsDirtyRecursive();
        }
    }

    public void Fire()
    {
        if (animCountdown == 0)
            animCountdown = CANNON_TOTAL_FRAMES;
    }
}
