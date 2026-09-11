using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class TrampolineObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 8;

    // Parameters
    internal short imageId;
    internal sbyte basePush;

    // State
    internal bool isJumpFinished;

    internal float calcPush;

    internal int period;
    internal int progress;
    private int animFrame;

    private BounceObject jumper;

    public override int ReadData(byte[] data, int dataPos)
    {
        dataPos = base.ReadData(data, dataPos);
        imageId = ReadShort(data, dataPos);
        dataPos += 2;
        basePush = (sbyte)data[dataPos++];
        progress = 0;
        period = 0;
        animFrame = 0;
        int levelType = BounceGame.GetLevelType(BounceGame.CurrentLevel);
        switch (levelType)
        {
            case 0: // green hill zone // Sonic reference???
                imageId = 497;
                break;
            case 1: // spooky zone
                imageId = 509;
                break;
            case 2: // bonus zone
                imageId = 490;
                break;
        }
        return dataPos;
    }

    public override void Initialize()
    {
        BBox.MinX = -LP32.FP32ToLP32(70f);
        BBox.MaxX = LP32.FP32ToLP32(70f);
        BBox.MinY = 0;
        BBox.MaxY = LP32.FP32ToLP32(95f);
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        Matrix.MultMatrices(rootMatrix, tmpObjMatrix, out Matrix temp);
        GameRuntime.DrawAnimatedImageRes(temp.TranslationX >> 16, temp.TranslationY >> 16, imageId, animFrame);
        DebugDraw(graphics, 0xFF00FF, rootMatrix);
    }

    public void SetJumper(BounceObject j)
    {
        if (jumper != j)
            ReleaseJumper();
        jumper = j;
        jumper.EnablePhysics = false;
        jumper.CurXVelocity = 0.0f;
        jumper.CurYVelocity = 0.0f;
    }

    private void ReleaseJumper()
    {
        if (jumper != null)
        {
            jumper.EnablePhysics = true;
            jumper.TorqueX = 0.0f;
            jumper.TorqueY = 0.0f;
            jumper.ReqSkipAccelStretch = true;
            if (jumper.Equals(BounceGame.BounceObj))
                BounceGame.CurrentControllerState = BounceGame.Controller.NORMAL;
        }
    }

    public override void OnPlayerContact()
    {
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        jumper.LocalObjectMatrix.TranslationY = tmpObjMatrix.TranslationY - (LP32.Int32ToLP32((short)GameRuntime.GetCompoundSpriteParamEx(GameRuntime.GetImageIdAfterAnimation(imageId, animFrame), 0)) / ScreenSpaceMatrix.M00 << 16);
        if (jumper.Equals(BounceGame.BounceObj))
            BounceGame.CurrentControllerState = BounceGame.Controller.FROZEN;
    }

    public override void UpdatePhysics()
    {
        base.UpdatePhysics();
        if (progress < period)
        {
            progress += GameRuntime.UpdateDelta;
            int frameCount = GameRuntime.GetImageAnimationFrameCount(imageId);
            if (progress >= period - period / frameCount) // jump on last frame
            {
                if (!isJumpFinished)
                {
                    isJumpFinished = true;
                    jumper.CurYVelocity = calcPush;
                    ReleaseJumper();
                }
                animFrame = frameCount * progress / period;
            }
            else
            {
                animFrame = frameCount * progress / period;
                OnPlayerContact();
            }
            if (progress >= period)
            {
                progress = 0;
                period = 0;
                animFrame = 0;
            }
        }
    }
}
