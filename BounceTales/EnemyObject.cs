using System.Numerics;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class EnemyObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 10;

    public enum Type : byte
    {
        CANDLE = 0,
        BUMPER_UNUSED = 1,
        MOLE = 2,
        STALKER_UNUSED = 3
    }

    private static readonly int[] ENEMY_MOTION_SPEEDS = [6000, 4000, 4000, 10000];
    private static readonly int[] ENEMY_WIDTHS = [50, 100, 100, 150];
    private static readonly int[] ENEMY_HEIGHTS = [100, 50, 80, 150];

    private const int STALKER_RECHARGE_TIME = 2000;

    // Parameters
    internal Type enemyType;

    private int movePoint1X;
    private int movePoint1Y;
    private int movePoint2X;
    private int movePoint2Y;

    // State - common
    private byte state;

    internal byte propelType;

    private byte curMovePoint;
    private bool facingLeft;

    private int rechargeTimer;

    // State - mole
    private int moleWaitTimer;

    internal int molePeekPeriod;
    internal int molePeekTimer;

    internal bool moleIsVulnerable;

    // State - stalker
    private int stalkerInitX;
    private int stalkerInitY;

    private void KillAndDropEgg()
    {
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        int posX = tmpObjMatrix.TranslationX;
        int posY = tmpObjMatrix.TranslationY;
        BounceGame.EnemyDeathParticle.EmitCircle(10, posX, (ENEMY_HEIGHTS[(byte)enemyType] << 16 >> 1) + posY, 370, 0, 920, 230);
        Despawn();
        BounceGame.EnemyDeadEgg.LocalObjectMatrix.TranslationX = posX;
        BounceGame.EnemyDeadEgg.LocalObjectMatrix.TranslationY = posY + LP32.Int32ToLP32(30);
        BounceGame.EnemyDeadEgg.RenderCalcMatrix = LocalObjectMatrix;
        BounceGame.EnemyDeadEgg.RecalcAbsObjectMatrix();
        BounceGame.EnemyDeadEgg.RenderCalcMatrix.Invert(out BounceGame.EnemyDeadEgg.InverseRenderCalcMatrix);
    }

    private static void BounceAwayPlayerOnStomp()
    {
        if (BounceGame.BounceObj.CurVelocity.Y < 0.0f)
        {
            BounceGame.BounceObj.CurVelocity.Y = -BounceGame.BounceObj.CurVelocity.Y * 0.5f;
            BounceGame.BounceObj.CurVelocity.X *= 0.7f;
        }
        else
        {
            BounceGame.BounceObj.CurVelocity.X = -BounceGame.BounceObj.CurVelocity.X * 0.5f;
            BounceGame.BounceObj.CurVelocity.Y *= 0.7f;
        }
    }

    public override int ReadData(byte[] data, int dataPos)
    {
        int a = base.ReadData(data, dataPos);
        movePoint1X = (ReadShort(data, a) << 16) + LocalObjectMatrix.TranslationX;
        movePoint1Y = (ReadShort(data, a + 2) << 16) + LocalObjectMatrix.TranslationY;
        movePoint2X = (ReadShort(data, a + 4) << 16) + LocalObjectMatrix.TranslationX;
        movePoint2Y = (ReadShort(data, a + 6) << 16) + LocalObjectMatrix.TranslationY;
        a += 8;
        enemyType = (Type)data[a++];
        curMovePoint = 0;
        rechargeTimer = 0;
        molePeekTimer = 0;
        facingLeft = movePoint1X < movePoint2X;
        propelType = 1;
        moleWaitTimer = 2000;
        state = 1;
        molePeekPeriod = 800;
        moleIsVulnerable = false;
        if (enemyType == Type.STALKER_UNUSED)
        {
            rechargeTimer = STALKER_RECHARGE_TIME;
            stalkerInitX = LocalObjectMatrix.TranslationX;
            stalkerInitY = LocalObjectMatrix.TranslationY;
            movePoint1X = stalkerInitX;
            movePoint1Y = stalkerInitY;
            movePoint2X = stalkerInitX;
            movePoint2Y = stalkerInitY;
        }
        return a;
    }

    public override void Initialize()
    {
        BBox.MinX = -ENEMY_WIDTHS[(byte)enemyType] << 15;
        BBox.MaxX = ENEMY_WIDTHS[(byte)enemyType] << 15;
        BBox.MinY = 0;
        BBox.MaxY = ENEMY_HEIGHTS[(byte)enemyType] << 16;
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        Matrix.MultMatrices(rootMatrix, tmpObjMatrix, out Matrix temp);
        int posX = temp.TranslationX >> 16;
        int posY = temp.TranslationY >> 16;
        switch (enemyType)
        {
            case Type.CANDLE: // candle
                GameRuntime.DrawAnimatedImageRes(posX, posY, 467, BounceGame.LevelTimer / 50 % 6);
                break;
            case Type.BUMPER_UNUSED:
                if (facingLeft)
                    GameRuntime.DrawImageRes(posX, posY, -41);
                else
                    GameRuntime.DrawImageRes(posX, posY, -42);
                break;
            case Type.MOLE: // mole
                if (state != 0)
                {
                    int clipX = graphics.ClipX;
                    int clipY = graphics.ClipY;
                    int clipWidth = graphics.ClipWidth;
                    int clipHeight = graphics.ClipHeight;
                    int i3 = posY - clipY;
                    if (i3 > clipHeight)
                        i3 = clipHeight;
                    graphics.SetClip(clipX, clipY, clipWidth, i3);
                    int molePeekY = molePeekTimer * 61 / molePeekPeriod;
                    GameRuntime.DrawImageRes(posX, posY + 61 - molePeekY, facingLeft ? 189 : 194);
                    graphics.SetClip(clipX, clipY, clipWidth, clipHeight);
                }
                GameRuntime.DrawAnimatedImageRes(posX, posY, 504, state == 0 ? BounceGame.LevelTimer / 150 % 4 : 0);
                break;
            case Type.STALKER_UNUSED:
                GameRuntime.DrawImageRes(posX, posY, -28);
                break;
        }
        DebugDraw(graphics, 0xFF0000, rootMatrix);
    }

    public override void OnPlayerContact()
    {
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        int myX = tmpObjMatrix.TranslationX;
        int bounceX = BounceGame.BounceObj.LocalObjectMatrix.TranslationX;
        if (rechargeTimer <= 0 && propelType == 0 && state == 1)
        {
            rechargeTimer = 500;
            if (myX < bounceX)
                BounceGame.BounceObj.Push.X += 200.0f;
            else
                BounceGame.BounceObj.Push.X -= 200.0f;
            BounceGame.BounceObj.Push.Y += 400.0f;
            BounceGame.BounceObj.CurVelocity = Vector2.Zero;
        }
        else if (propelType == 1)
        {
            if (myX < bounceX)
                BounceGame.BounceObj.Push.X += 100.0f;
            else
                BounceGame.BounceObj.Push.X -= 100.0f;
            BounceGame.BounceObj.CurVelocity = Vector2.Zero;
        }
    }

    public void OnPlayerHit()
    {
        switch (enemyType)
        {
            case Type.CANDLE:
                if (rechargeTimer <= 0)
                {
                    LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
                    if (tmpObjMatrix.TranslationX < BounceGame.BounceObj.LocalObjectMatrix.TranslationX)
                        BounceGame.BounceObj.Push.X += 500.0f;
                    else
                        BounceGame.BounceObj.Push.X -= 500.0f;
                    BounceGame.BounceObj.CurVelocity = Vector2.Zero;
                    rechargeTimer = 500;
                }
                break;
            case Type.BUMPER_UNUSED:
                if (BounceGame.BounceObj.BallForme == BounceObject.Forme.BUMPY_CRACKS)
                    KillAndDropEgg();
                else
                    BounceGame.CurrentPlayerState = BounceGame.PlayerState.LOSE;
                break;
            case Type.MOLE:
                if (rechargeTimer <= 0 && propelType == 0 && state == 0)
                {
                    molePeekTimer = 0;
                    state = 1;
                    molePeekPeriod = 200;
                }
                break;
            case Type.STALKER_UNUSED:
                BounceGame.CurrentPlayerState = BounceGame.PlayerState.LOSE;
                LocalObjectMatrix.TranslationX = stalkerInitX;
                LocalObjectMatrix.TranslationY = stalkerInitY;
                movePoint1X = stalkerInitX;
                movePoint1Y = stalkerInitY;
                movePoint2X = stalkerInitX;
                movePoint2Y = stalkerInitY;
                break;
        }
    }

    public override void UpdatePhysics()
    {
        SetIsDirtyRecursive();
        base.UpdatePhysics();
        if (BounceGame.CurrentLevel != LevelID.FINAL_RIDE || EventObject.EventVars[8] != 5)
        {
            // since 2.0.25 - final boss death bugfix
            int tx = LocalObjectMatrix.TranslationX;
            int ty = LocalObjectMatrix.TranslationY;
            if (rechargeTimer > 0)
            {
                rechargeTimer -= GameRuntime.UpdateDelta;
                if (rechargeTimer <= 0)
                {
                    rechargeTimer = 0;
                    if (enemyType == Type.STALKER_UNUSED)
                    {
                        rechargeTimer = STALKER_RECHARGE_TIME;
                        movePoint2X = movePoint1X;
                        movePoint2Y = movePoint1Y;
                        int i7 = movePoint2X - tx;
                        int i8 = movePoint2Y - ty;
                        float f = i7 >> 16;
                        float f2 = i8 >> 16;
                        int sqrt = LP32.FP64ToLP32(Math.Sqrt(f * f + f2 * f2));
                        int i9 = (STALKER_RECHARGE_TIME << 1) * ENEMY_MOTION_SPEEDS[(byte)enemyType];
                        if (i9 > sqrt)
                        {
                            double d = i9 / (double)sqrt;
                            movePoint2X = (int)(i7 * (d - 1.0d)) + movePoint2X;
                            movePoint2Y += (int)((d - 1.0d) * i8);
                        }
                        movePoint1X = BounceGame.BounceObj.LocalObjectMatrix.TranslationX;
                        movePoint1Y = BounceGame.BounceObj.LocalObjectMatrix.TranslationY;
                    }
                }
            }
            if (enemyType == Type.STALKER_UNUSED)
            {
                int i10 = movePoint2X - tx;
                int i11 = movePoint2Y - ty;
                float f3 = i10 >> 16;
                float f4 = i11 >> 16;
                int sqrt2 = LP32.FP64ToLP32(Math.Sqrt(f3 * f3 + f4 * f4));
                if (sqrt2 != 0)
                {
                    double d2 = ENEMY_MOTION_SPEEDS[(byte)enemyType] * GameRuntime.UpdateDelta / (double)sqrt2;
                    tx += (int)(i10 * d2);
                    ty += (int)(i11 * d2);
                }
            }
            else
            {
                if (enemyType == Type.MOLE)
                {
                    if (moleWaitTimer > 0)
                    {
                        moleWaitTimer -= GameRuntime.UpdateDelta;
                        if (moleWaitTimer <= 0)
                        {
                            if (propelType == 0)
                            {
                                propelType = 1;
                                moleWaitTimer = 2000;
                                state = 1;
                                molePeekPeriod = 800;
                            }
                            else
                            {
                                propelType = 0;
                                moleWaitTimer = 5000;
                                state = 2;
                                molePeekPeriod = 800;
                                moleIsVulnerable = true;
                            }
                        }
                    }
                    if (state == 1)
                    {
                        molePeekTimer += GameRuntime.UpdateDelta;
                        if (molePeekTimer >= molePeekPeriod)
                        {
                            if (propelType == 0)
                            {
                                state = 2;
                                molePeekPeriod = 200;
                            }
                            molePeekTimer = molePeekPeriod;
                        }
                    }
                    else if (state == 2)
                    {
                        molePeekTimer -= GameRuntime.UpdateDelta;
                        if (molePeekTimer <= 0)
                        {
                            state = 0;
                            molePeekTimer = 0;
                            moleIsVulnerable = false;
                        }
                    }
                }
                int targetTX;
                int targetTY;
                int otherMovePointX;
                if (curMovePoint == 0)
                {
                    targetTX = movePoint1X;
                    targetTY = movePoint1Y;
                    otherMovePointX = movePoint2X;
                }
                else
                {
                    targetTX = movePoint2X;
                    targetTY = movePoint2Y;
                    otherMovePointX = movePoint1X;
                }
                if (enemyType == Type.CANDLE && Math.Abs(tx - targetTX) > LP32.Int32ToLP32(70) && Math.Abs(tx - otherMovePointX) > LP32.Int32ToLP32(70))
                {
                    LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
                    int myAbsX = tmpObjMatrix.TranslationX;
                    int bounceX = BounceGame.BounceObj.LocalObjectMatrix.TranslationX;
                    if (Math.Abs(bounceX - myAbsX) < LP32.Int32ToLP32(120) && (myAbsX < bounceX && targetTX < tx || myAbsX > bounceX && targetTX > tx))
                    {
                        if (curMovePoint == 0)
                        {
                            targetTX = movePoint2X;
                            targetTY = movePoint2Y;
                            curMovePoint = 1;
                        }
                        else
                        {
                            targetTX = movePoint1X;
                            targetTY = movePoint1Y;
                            curMovePoint = 0;
                        }
                    }
                }
                int xdiff = targetTX - tx;
                int ydiff = targetTY - ty;
                float f5 = xdiff >> 16;
                float f6 = ydiff >> 16;
                int distToTarget = LP32.FP64ToLP32(Math.Sqrt(f5 * f5 + f6 * f6));
                bool xDone = false;
                bool yDone = false;
                if (distToTarget != 0)
                {
                    double d3 = GameRuntime.UpdateDelta * ENEMY_MOTION_SPEEDS[(byte)enemyType] / (double)distToTarget;
                    int i16 = (int)(xdiff * d3);
                    int i17 = (int)(ydiff * d3);
                    tx += i16;
                    ty += i17;
                    if (i16 >= 0 && tx >= targetTX || i16 <= 0 && tx <= targetTX)
                    {
                        xDone = true;
                        tx = targetTX;
                    }
                    if (i17 >= 0 && ty >= targetTY || i17 <= 0 && ty <= targetTY)
                    {
                        yDone = true;
                        ty = targetTY;
                    }
                }

                if (xDone && yDone || distToTarget == 0)
                {
                    tx = targetTX;
                    ty = targetTY;
                    curMovePoint = curMovePoint == 0 ? (byte)1 : (byte)0;
                }
            }
            if (enemyType != Type.MOLE || enemyType == Type.MOLE && propelType == 0 && state == 0)
            {
                facingLeft = LocalObjectMatrix.TranslationX < tx;
                LocalObjectMatrix.TranslationX = tx;
                LocalObjectMatrix.TranslationY = ty;
                RenderMatrixIsDirty = true;
                ObjectMatrixIsDirty = true;
            }
        }
    }

    public void Stomp()
    {
        switch (enemyType)
        {
            case Type.CANDLE:
                BounceAwayPlayerOnStomp();
                KillAndDropEgg();
                break;
            case Type.BUMPER_UNUSED:
            case Type.STALKER_UNUSED:
            default:
                break;
            case Type.MOLE:
                BounceAwayPlayerOnStomp();
                KillAndDropEgg();
                break;
        }
    }
}
