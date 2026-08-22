using System.Diagnostics;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class BounceObject : GameObject
{
    public const byte TYPEID = 4;

    public enum Forme
    {
        BOUNCE = 0,
        BUMPY_CRACKS = 1,
        WOLLY = 2
    }

    // Draw - eyes
    private static readonly int[] EYE_ANIMATION_IMAGE_IDS = [458, 460, 454, 452, 456];

    // Draw - bounce
    private const int BOUNCE_PRIMARY_COLOR = 0xED1C24;
    private const int BOUNCE_SECONDARY_COLOR = 0xAF1100;
    private const int BOUNCE_HIGHLIGHT_COLOR = 0xFFFFFF;

    // Draw - Bumpy Cracks
    private static readonly short[] BUMPY_CRACKS_ROTATION_SPRITES =
    [
        211, 212, 223, 234, 237, 238, 239, 240, 241, 242, 213,
        214, 215, 216, 217, 218, 219, 220, 221, 222, 224, 225,
        226, 227, 228, 229, 230, 231, 232, 233, 235, 236
    ];

    // Draw - Wolly
    private static readonly int[] WOLLY_SEGMENT_COLORS =
    [
        0xEDEDED,
        0x0064BC,
        0xEDEDED,
        0xE11900,
        0xEDEDED,
        0xE8DF05
    ];

    // Collisions
    private const int MAX_COLLISION_POINTS = 16;
    private const float LP32_TO_FP32_MULTIPLIER = 1.5258789E-5f;

    // Dimensions
    public static readonly int[] BALL_DIMENS = [20, 20, 20];
    public static int[] BALL_DIMENS_SCREENSPACE = new int[3];

    /*Physics
    List of physics variables (1cm = 1px):

    Base gravity - natural gravity present in the overworld [cm/s]
    Gravity - per-ball multiplier of the base gravity [1]
    Friction - how much surface contact deccelerates the ball [1]
    Ricochet factor - how much the ball bounces away from walls [(cm/s)/(cm/s)] = [1]
    Movement acceleration - amount of speed added when moving with the keypad [cm/s/s]
    Maximum movement speed - maximum velocity achievable through natural motion [cm/s]
    Mid-air movement suppression - Mid-air acceleration mitigation intensity [1]
    Jump acceleration - vertical acceleration added when jumping [cm/s/s]
    Inverse maximum jump slope - the maximum slope on which jumping is still possible, written as the minimum absolute cosine value of it [1]*/
    private const float BASE_GRAVITY_X = 0.0f;
    private const float BASE_GRAVITY_Y = -400.0f;
    public static readonly float[] GRAVITY = [1.0f, 1.4f, 0.5f];

    private static readonly float[] FRICTION = [0.6f, 0.3f, 0.6f];
    private static readonly float[] RICOCHET_FACTOR = [0.1f, 0.1f, 0.1f];

    private static readonly float[] MOVEMENT_ACCELERATION = [500.0f, 350.0f, 312.5f];
    private static readonly float[] MAXIMUM_MOVEMENT_SPEED = [280.0f, 350.0f, 150.0f];
    private static readonly float[] MIDAIR_MOVEMENT_SUPPRESSION = [1.5f, 2.5f, 1.2f];

    private static readonly float[] JUMP_ACCELERATION = [288.0f, 255.0f, 162.5f];
    private static readonly float[] MAX_JUMP_SLOPE_INV =
    [
        (float)Math.Cos(0.9599310755729675d), // cos 55deg
        (float)Math.Cos(0.9599310755729675d),
        (float)Math.Cos(0.9599310755729675d)
    ];

    // Global state - AABB collisions
    private static bool aabbRayResult;
    private static Vector2I aabbRay;
    private static int aabbRayWeight;

    // Parameters
    private readonly bool isPlayer;

    // State - eye animation
    // made these non-static to allow for more BounceObjects
    public int IdleAnimStartTimer;
    public int EyeFrame;
    private int idleAnimTimer;

    // State - visuals
    public bool IsVisible = true;
    public Color FadeColor = Color.Zero;

    // State - physics
    public bool EnablePhysics = true;
    public bool IsGrounded;

    private float airTimeCounter;

    public float CurXVelocity;
    public float CurYVelocity;
    public float CurVelocity;

    public float LastXVelocity;
    public float LastYVelocity;

    private float slopeSinAbs;
    private float slopeCosAbs;

    private const float torqueFalloff = 0.5f;

    public float TorqueX;
    public float TorqueY;
    private float rotation;

    public float GravityX;
    public float GravityY;

    public float PushX;
    public float PushY;

    public Forme BallForme = 0;

    // State - stretch
    public bool ReqSkipAccelStretch;

    private readonly int[] reqStretchMagnitudes;
    private readonly int[] stretchMagnitudes;

    private readonly int[] stretchResults;
    private readonly int[] stretchResultsAbs;

    private readonly int[] stretchDirBalance;
    private readonly int[] stretchBuffer;

    // State - collision
    private int collPointCount;

    private readonly bool[] f41a = new bool[MAX_COLLISION_POINTS];

    private readonly int[] collPointsX = new int[MAX_COLLISION_POINTS];
    private readonly int[] collPointsY = new int[MAX_COLLISION_POINTS];

    private readonly int[] f65l = new int[MAX_COLLISION_POINTS];
    private readonly int[] f66m = new int[MAX_COLLISION_POINTS];

    private readonly int[] f68n = new int[MAX_COLLISION_POINTS];
    private readonly int[] f70o = new int[MAX_COLLISION_POINTS];

    // State - Super Bounce
    private int superBounceParticleTimer;

    static BounceObject()
    {
        UpdateScreenSpaceConstants();
    }

    public BounceObject(bool isPlayer) : base(TYPEID)
    {
        this.isPlayer = isPlayer;
        if (isPlayer)
        {
            reqStretchMagnitudes = new int[4];
            stretchResults = new int[4];
            stretchResultsAbs = new int[4];
            stretchMagnitudes = new int[4];
            stretchDirBalance = new int[4];
            stretchBuffer = new int[4];
            ResetStretch();
        }
    }

    public override void Initialize()
    {
        BBox.MinX = -BALL_DIMENS[(int)BallForme] << 16;
        BBox.MaxX = BALL_DIMENS[(int)BallForme] << 16;
        BBox.MinY = -BALL_DIMENS[(int)BallForme] << 16;
        BBox.MaxY = BALL_DIMENS[(int)BallForme] << 16;
    }

    public void SetPosXY(int posX, int posY)
    {
        ResetPhysics();
        LocalObjectMatrix.TranslationX = posX;
        LocalObjectMatrix.TranslationY = posY;
        RenderCalcMatrix.TranslationX = posX;
        RenderCalcMatrix.TranslationY = posY;
        ReqSkipAccelStretch = true;
        if (isPlayer)
        {
            LastXVelocity = 0.0f;
            LastYVelocity = 0.0f;
            for (int i3 = 0; i3 < 4; i3++)
            {
                reqStretchMagnitudes[i3] = 0;
                stretchResults[i3] = 0;
                stretchResultsAbs[i3] = 0;
                stretchMagnitudes[i3] = 0;
            }
        }
        ObjectMatrixIsDirty = true;
    }

    public override void CheckCollisions(GameObject startNode)
    {
        int higherX;
        int lowerX;
        int higherY;
        int lowerY;
        int xmax;
        int xmin;
        int ymax;
        int ymin;
        int i9;
        bool z;
        for (int i10 = 2; i10 < 10; i10++)
        {
            collPointCount = 0;
            int ballDiameter = BALL_DIMENS[(int)BallForme] << 16;
            int ballDiameterSquared = BALL_DIMENS[(int)BallForme] * BALL_DIMENS[(int)BallForme] << 16;
            int xChange = LocalObjectMatrix.TranslationX - RenderCalcMatrix.TranslationX;
            int yChange = LocalObjectMatrix.TranslationY - RenderCalcMatrix.TranslationY;
            GameObject other = startNode;
            while (other != null)
            {
                Vector2I relToOther = other.InverseRenderCalcMatrix.MulVector(RenderCalcMatrix.Translation);
                if (other.ObjectMatrixIsDirty)
                    other.RecalcAbsObjectMatrix();
                Vector2I newRelToOther = other.InvAbsoluteObjectMatrix.MulVector(LocalObjectMatrix.Translation);
                if (relToOther.X > newRelToOther.X)
                {
                    higherX = relToOther.X;
                    lowerX = newRelToOther.X;
                }
                else
                {
                    higherX = newRelToOther.X;
                    lowerX = relToOther.X;
                }
                if (relToOther.Y > newRelToOther.Y)
                {
                    higherY = relToOther.Y;
                    lowerY = newRelToOther.Y;
                }
                else
                {
                    higherY = newRelToOther.Y;
                    lowerY = relToOther.Y;
                }
                int collAABBMinX = lowerX - ballDiameter;
                int collAABBMinY = lowerY - ballDiameter;
                int collAABBMaxX = higherX + ballDiameter;
                int collAABBMaxY = higherY + ballDiameter;
                if (!AABB.Intersects(other.AllBBox, new(collAABBMinX, collAABBMinY, collAABBMaxX, collAABBMaxY)))
                    other = other.GetNextNode(startNode);
                else if ((other.Flags & ObjectFlags.NOCOLLIDE) == 0)
                {
                    //System.out.println("checkcoll me " + getObjectId() + " other " + other.getObjectId() + " isplayer " + isPlayer + " mybbox " + collAABBMinX + "/" + collAABBMaxX + "/" + collAABBMinY + "/" + collAABBMaxY);
                    switch (other.GetObjType())
                    {
                        case GeometryObject.TYPEID:
                            GeometryObject geom = (GeometryObject)other;
                            bool z2 = false;
                            int i24 = 0;
                            int i25 = 0;
                            int i26 = 0;
                            int i27 = 0;
                            int i28 = 0;
                            int vertCount = geom.GetVertexCount() - 1;
                            for (int vertIdx = 0; vertIdx < vertCount; vertIdx++)
                            {
                                int x1 = geom.XCoordBuffer[vertIdx];
                                int y1 = geom.YCoordBuffer[vertIdx];
                                int x2 = geom.XCoordBuffer[vertIdx + 1];
                                int y2 = geom.YCoordBuffer[vertIdx + 1];
                                if (x1 > x2)
                                {
                                    xmax = x1;
                                    xmin = x2;
                                }
                                else
                                {
                                    xmax = x2;
                                    xmin = x1;
                                }
                                if (y1 > y2)
                                {
                                    ymax = y1;
                                    ymin = y2;
                                }
                                else
                                {
                                    ymax = y2;
                                    ymin = y1;
                                }
                                if (AABB.Intersects(new(xmin, ymin, xmax, ymax), new(collAABBMinX, collAABBMinY, collAABBMaxX, collAABBMaxY)))
                                {
                                    if (!z2)
                                    {
                                        z = true;
                                        i24 = newRelToOther.X - relToOther.X;
                                        i25 = newRelToOther.Y - relToOther.Y;
                                        i26 = (int)Math.Sqrt(i24 * (long)i24 + i25 * (long)i25);
                                        if (i26 != 0)
                                        {
                                            i9 = (int)(((long)i24 << 16) / i26);
                                            i28 = (int)(((long)i25 << 16) / i26);
                                        }
                                        else
                                            i9 = i27;
                                    }
                                    else
                                    {
                                        i9 = i27;
                                        z = z2;
                                    }
                                    int lineYDim = y1 - y2;
                                    int lineXDimNeg = -(x1 - x2);
                                    int sqrt = (int)Math.Sqrt(lineYDim * (long)lineYDim + lineXDimNeg * (long)lineXDimNeg);
                                    int i37 = (int)((lineYDim * (long)BALL_DIMENS[(int)BallForme] << 16) / sqrt);
                                    int i38 = (int)((lineXDimNeg * (long)BALL_DIMENS[(int)BallForme] << 16) / sqrt);
                                    int i39 = x1 + i37;
                                    int i40 = y1 + i38;
                                    int i41 = x2 + i37;
                                    int i42 = y2 + i38;
                                    if (i24 * (long)lineYDim + i25 * (long)lineXDimNeg < 0)
                                    {
                                        if (AABBIntersectRay(relToOther.X, relToOther.Y, i24, i25, i39, i40, i41, i42, ballDiameterSquared))
                                            RegistCollPoint(geom, aabbRayWeight, xChange, yChange, lineYDim, lineXDimNeg, aabbRayResult);
                                        if (M9c(relToOther.X, relToOther.Y, i9, i28, i26, x1, y1, BALL_DIMENS[(int)BallForme]))
                                            RegistCollPoint(geom, aabbRayWeight, xChange, yChange, aabbRay.X - x1, aabbRay.Y - y1, aabbRayResult);
                                        if (M9c(relToOther.X, relToOther.Y, i9, i28, i26, x2, y2, BALL_DIMENS[(int)BallForme]))
                                            RegistCollPoint(geom, aabbRayWeight, xChange, yChange, aabbRay.X - x2, aabbRay.Y - y2, aabbRayResult);
                                    }
                                }
                                else
                                {
                                    i9 = i27;
                                    z = z2;
                                }
                                i27 = i9;
                                z2 = z;
                            }
                            other = other.GetNextNodeDescendToChildren(startNode);
                            break;
                        case 3:
                        case 5:
                        default:
                            other = other.GetNextNodeDescendToChildren(startNode);
                            break;
                        case TYPEID:
                            other = other.GetNextNodeDescendToChildren(startNode);
                            break;
                        case WaterObject.TYPEID: // water
                            WaterObject water = (WaterObject)other;
                            int waterMinX = water.area.MinX << 16;
                            if (AABBIntersectRay(waterMinX, water.area.MinY << 16, (water.area.MaxX << 16) - waterMinX, 0, relToOther.X, relToOther.Y, newRelToOther.X, newRelToOther.Y, 0))
                                water.OnBounceSurfaceContact(aabbRayWeight, CurYVelocity, BALL_DIMENS[(int)BallForme], this);
                            if (newRelToOther.Y - ballDiameter < water.surfaceY)
                                water.UpdateBounceSwim(newRelToOther.X, newRelToOther.Y - ballDiameter, this);
                            other = other.GetNextNodeDescendToChildren(startNode);
                            break;
                        case CannonObject.TYPEID:
                            CannonObject cannon = (CannonObject)other;
                            if (cannon.loadAABB.CheckBoundCross(relToOther, newRelToOther))
                                cannon.OnPlayerContact();
                            other = other.GetNextNodeDescendToChildren(startNode);
                            break;
                        case TrampolineObject.TYPEID: // jump pad
                            TrampolineObject jumpPad = (TrampolineObject)other;
                            if (AABBIntersectRay(
                                LP32.Int32ToLP32(-70),
                                LP32.Int32ToLP32(95),
                                LP32.Int32ToLP32(140),
                                0,
                                relToOther.X,
                                relToOther.Y,
                                newRelToOther.X,
                                newRelToOther.Y,
                                0
                            ))
                            {
                                float yvel = CurYVelocity;
                                if (yvel < 0.0f)
                                {
                                    int yvelLim = -(int)yvel;
                                    if (yvelLim < 100)
                                        yvelLim = 100;
                                    if (yvelLim > 1000)
                                        yvelLim = 1000;
                                    jumpPad.period = (1100 - yvelLim >> 1) + 20;
                                    jumpPad.progress = jumpPad.period / GameRuntime.GetImageAnimationFrameCount(jumpPad.imageId);
                                    float basePush = jumpPad.basePush * 2.0f / 100.0f;
                                    float maxPush = 400f * basePush;
                                    jumpPad.calcPush = -yvel * basePush;
                                    if (jumpPad.calcPush > maxPush)
                                        jumpPad.calcPush = maxPush;
                                    jumpPad.SetJumper(this);
                                    jumpPad.isJumpFinished = false;
                                    jumpPad.OnPlayerContact();
                                }
                            }
                            other = other.GetNextNodeDescendToChildren(startNode);
                            break;
                        case EggObject.TYPEID: // collected egg
                            EggObject collectEgg = (EggObject)other;
                            other = other.GetNextNodeDescendToChildren(startNode);
                            int i45 = newRelToOther.X >> 16;
                            int i46 = newRelToOther.Y >> 16;
                            if (i45 * i45 + i46 * i46 < 2025)
                            {
                                collectEgg.LoadObjectMatrixToTarget(out TmpObjMatrix);
                                BounceGame.EggCollectParticle.EmitCircle(8, TmpObjMatrix.TranslationX, TmpObjMatrix.TranslationY, 540, 0, 540, 0);
                                if (collectEgg.Equals(BounceGame.EnemyDeadEgg))
                                {
                                    BounceGame.EggCount++;
                                    collectEgg.LocalObjectMatrix.TranslationX = int.MaxValue;
                                    collectEgg.LocalObjectMatrix.TranslationY = int.MaxValue;
                                    collectEgg.RenderCalcMatrix = collectEgg.LocalObjectMatrix;
                                    collectEgg.RenderCalcMatrix.Invert(out collectEgg.InverseRenderCalcMatrix);
                                    collectEgg.ObjectMatrixIsDirty = true;
                                }
                                else
                                {
                                    collectEgg.Despawn();
                                    BounceGame.EggCount++;
                                }
                            }
                            break;
                        case EnemyObject.TYPEID: // enemy collision
                            EnemyObject enemy = (EnemyObject)other;
                            other = other.GetNextNodeDescendToChildren(startNode);
                            bool isStomp = false;
                            if (enemy.enemyType == EnemyObject.Type.CANDLE || enemy.enemyType == EnemyObject.Type.MOLE && (enemy.propelType == 1 || enemy.moleIsVulnerable))
                            {
                                AABB aabb = new(enemy.BBox.MinX, enemy.BBox.MinY + (enemy.BBox.MaxY - enemy.BBox.MinY << 1) / 3, enemy.BBox.MaxX, enemy.BBox.MaxY);
                                isStomp = aabb.CheckBoundCross(relToOther, newRelToOther);
                            }
                            if (isStomp)
                                enemy.Stomp();
                            else
                            {
                                if (enemy.BBox.CheckBoundCross(relToOther, newRelToOther))
                                    enemy.OnPlayerHit();
                                if (enemy.enemyType == EnemyObject.Type.MOLE)
                                {
                                    AABB aabb = new(enemy.BBox.MinX, enemy.BBox.MinY, enemy.BBox.MaxX, enemy.BBox.MinY + enemy.molePeekTimer * 100 / enemy.molePeekPeriod * (enemy.BBox.MaxY - enemy.BBox.MinY) / 100);
                                    if (aabb.CheckBoundCross(relToOther, newRelToOther))
                                        enemy.OnPlayerContact();
                                }
                            }
                            break;
                    }
                }
                else
                    other = other.GetNextNodeDescendToChildren(startNode);
            }
            if (collPointCount != 0)
            {
                long nearestDistance = long.MaxValue;
                int nearestCollIdx = -1;
                for (int collIndex = 0; collIndex < collPointCount; collIndex++)
                {
                    long distX = collPointsX[collIndex] - RenderCalcMatrix.TranslationX;
                    long distY = collPointsY[collIndex] - RenderCalcMatrix.TranslationY;
                    long distance = distX * distX + distY * distY;
                    if (f41a[collIndex])
                        distance = -distance;
                    if (distance > 0x271000000000L)
                        Debug.WriteLine("Sanity check failed! Found collision is too far, distance: " + Math.Sqrt(distance) / 65536.0d);
                    else if (distance < nearestDistance)
                    {
                        nearestCollIdx = collIndex;
                        nearestDistance = distance;
                    }
                }
                if (nearestCollIdx != -1)
                {
                    float f3 = 1000.0f / GameRuntime.UpdateDelta;
                    float f4 = collPointsX[nearestCollIdx] * LP32_TO_FP32_MULTIPLIER;
                    float f5 = collPointsY[nearestCollIdx] * LP32_TO_FP32_MULTIPLIER;
                    float f6 = f65l[nearestCollIdx] * LP32_TO_FP32_MULTIPLIER;
                    float f7 = f66m[nearestCollIdx] * LP32_TO_FP32_MULTIPLIER;
                    float sqrt2 = 1.0f / (float)Math.Sqrt((double)(f6 * f6 + f7 * f7));
                    float xslope = sqrt2 * f6;
                    float yslope = sqrt2 * f7;
                    float f10 = f68n[nearestCollIdx] * LP32_TO_FP32_MULTIPLIER;
                    float f11 = f70o[nearestCollIdx] * LP32_TO_FP32_MULTIPLIER;
                    float f12 = f10 * xslope + f11 * yslope;
                    float f13 = f12 * xslope;
                    float f14 = f12 * yslope;
                    if (xslope * f10 + yslope * f11 < 0.0f)
                    {
                        f13 = -f13;
                        f14 = -f14;
                    }
                    float f15 = f10 * f3;
                    float f16 = f11 * f3;
                    float f17 = (LocalObjectMatrix.TranslationX - collPointsX[nearestCollIdx]) * LP32_TO_FP32_MULTIPLIER;
                    float f18 = (LocalObjectMatrix.TranslationY - collPointsY[nearestCollIdx]) * LP32_TO_FP32_MULTIPLIER;
                    float f19 = f10 + f4;
                    float f20 = f11 + f5;
                    float f21 = f17 * xslope + f18 * yslope;
                    float f22 = f21 * xslope;
                    float f23 = f21 * yslope;
                    float f24 = f13 + f4 + (f17 - f22 - f22 * RICOCHET_FACTOR[(int)BallForme]) + 0.01f * xslope;
                    float f25 = f14 + (f18 - f23 - f23 * RICOCHET_FACTOR[(int)BallForme]) + f5 + 0.01f * yslope;
                    float f26 = CurXVelocity * xslope + CurYVelocity * yslope;
                    float f27 = f26 * xslope;
                    float f28 = f26 * yslope;
                    float f29 = CurXVelocity - f27 - f27 * RICOCHET_FACTOR[(int)BallForme];
                    float f30 = CurYVelocity - f28 - f28 * RICOCHET_FACTOR[(int)BallForme];
                    float f31 = f15 * xslope + f16 * yslope;
                    CurXVelocity = f29 + f31 * xslope;
                    CurYVelocity = f30 + f31 * yslope;
                    float f32 = CurXVelocity - f15;
                    float f33 = CurYVelocity - f16;
                    float sqrt3 = (float)Math.Sqrt((double)(f32 * f32 + f33 * f33));
                    float f34 = sqrt3 != 0.0f ? f32 / sqrt3 : 0.0f;
                    float f35 = sqrt3 != 0.0f ? f33 / sqrt3 : 0.0f;
                    float f36 = -(0.0f * xslope + BASE_GRAVITY_Y * yslope) * FRICTION[(int)BallForme] * GRAVITY[(int)BallForme];
                    float f37 = f34 * f36;
                    float f38 = f35 * f36;
                    float f39 = f3 * GRAVITY[(int)BallForme];
                    float f40 = f32 * f39;
                    float f41 = f39 * f33;
                    if (f40 * f40 + f41 * f41 < f37 * f37 + f38 * f38)
                    {
                        GravityX -= f40;
                        GravityY -= f41;
                    }
                    else
                    {
                        GravityX -= f37;
                        GravityY -= f38;
                    }
                    TorqueX = TorqueX * (1.0f - torqueFalloff) + torqueFalloff * f32;
                    TorqueY = TorqueY * (1.0f - torqueFalloff) + torqueFalloff * f33;
                    airTimeCounter = 0.0f;
                    IsGrounded = true;
                    slopeSinAbs = xslope;
                    slopeCosAbs = yslope;
                    RenderCalcMatrix.TranslationX = LP32.FP32ToLP32(f19);
                    RenderCalcMatrix.TranslationY = LP32.FP32ToLP32(f20);
                    LocalObjectMatrix.TranslationX = LP32.FP32ToLP32(f24);
                    LocalObjectMatrix.TranslationY = LP32.FP32ToLP32(f25);
                    RecalcAbsObjectMatrix();
                    RenderCalcMatrix.Invert(out InverseRenderCalcMatrix);
                    collPointCount = 0;
                }
            }
            else
                break;
        }
        airTimeCounter += GameRuntime.UpdateDelta * 0.001f;
        if (airTimeCounter > 0.25f) // since this is done both in coll check and physics update, it's actually 1/8th of a second instead of 1/4th
            IsGrounded = false;
    }

    // TODO: collPoints should be a Vector2I[]
    private void RegistCollPoint(GeometryObject geometry, int t, int x, int y, int x2, int y2, bool z)
    {
        Vector2I vectorMulRsl;
        if (t > 0)
        {
            collPointsX[collPointCount] = RenderCalcMatrix.TranslationX + (int)(x * (long)t >> 16);
            collPointsY[collPointCount] = RenderCalcMatrix.TranslationY + (int)(y * (long)t >> 16);
            f41a[collPointCount] = z;

            vectorMulRsl = geometry.RenderCalcMatrix.MulDirection(x2, y2);
            int i6 = vectorMulRsl.X;
            int i7 = vectorMulRsl.Y;

            geometry.LoadObjectMatrixToTarget(out TmpObjMatrix);
            vectorMulRsl = TmpObjMatrix.MulDirection(x2, y2);
            int i8 = vectorMulRsl.X;
            int i9 = vectorMulRsl.Y;
            f65l[collPointCount] = (int)(i6 * (long)(LP32.ONE - t) + i8 * (long)t >> 16);
            f66m[collPointCount] = (int)(i7 * (long)(LP32.ONE - t) + i9 * (long)t >> 16);
        }
        else if (t < 0)
            throw new Exception("t < 0, t: " + t);
        else
        {
            vectorMulRsl = geometry.RenderCalcMatrix.MulVector(aabbRay);
            collPointsX[collPointCount] = vectorMulRsl.X;
            collPointsY[collPointCount] = vectorMulRsl.Y;
            f41a[collPointCount] = z;
            vectorMulRsl = geometry.RenderCalcMatrix.MulVector(x2, y2);
            f65l[collPointCount] = vectorMulRsl.X;
            f66m[collPointCount] = vectorMulRsl.Y;
        }

        vectorMulRsl = geometry.RenderCalcMatrix.MulVector(aabbRay);
        int i10 = vectorMulRsl.X;
        int i11 = vectorMulRsl.Y;

        geometry.LoadObjectMatrixToTarget(out TmpObjMatrix);
        vectorMulRsl = TmpObjMatrix.MulVector(aabbRay);
        int i12 = (int)(GameRuntime.UpdateDelta * 6553.6f);
        f68n[collPointCount] = vectorMulRsl.X - i10 + i12 * 0;
        f70o[collPointCount] = vectorMulRsl.Y - i11 + i12 * 0;
        collPointCount++;
        if (geometry.Event > -1)
        {
            Debug.WriteLine("Geometry " + GetObjectId() + " started event " + geometry.Event);
            ((EventObject)GetObjectRoot().SearchByObjId(geometry.Event)).ChangeEventState(EventObject.State.ACTIVE);
        }
    }

    // TODO: Move to AABB maybe?
    private static bool AABBIntersectRay(int minX, int minY, int width, int height, int rayx1, int rayy1, int rayx2, int rayy2, int epsilon)
    {
        long l2 = rayx1 * (long)height >> 16;
        long l3 = rayy1 * (long)width >> 16;
        long l4 = rayx2 * (long)height >> 16;
        long l5 = rayy2 * (long)width >> 16;
        long l6 = l2 - l3 - l4 + l5;
        if (l6 == 0L)
            return false;
        long l7 = (l2 - l3 + (width * (long)minY >> 16) - (height * (long)minX >> 16) << 16) / l6;
        if (l7 < 0L || l7 > LP32.ONE)
            return false;
        long weight = (rayx1 * (long)(rayy2 - minY) + rayy1 * (long)(minX - rayx2) + rayx2 * (long)minY - rayy2 * (long)minX) / l6;
        if (weight >= 0L && weight <= LP32.ONE)
        {
            aabbRayWeight = (int)weight;
            aabbRay.X = (int)(minX + (weight * width >> 16));
            aabbRay.Y = (int)(minY + (weight * height >> 16));
            aabbRayResult = false;
            return true;
        }
        if (weight < 0L)
        {
            long l9 = minX - rayx1;
            long l10 = rayx2 - rayx1;
            long l11 = minY - rayy1;
            long l12 = rayy2 - rayy1;
            long l13 = l9 * l10 + l11 * l12 >> 16;
            if (l13 <= 0L)
                return false;
            long l14 = l10 * l10 + l12 * l12 >> 16;
            if (l13 >= l14)
                return false;
            long l15 = rayx1 + ((l13 = (l13 << 16) / l14) * l10 >> 16);
            long l16 = l15 - minX;
            long l17 = rayy1 + (l13 * l12 >> 16);
            long l18 = l17 - minY;
            long l19 = l16 * l16 + l18 * l18 >> 16;
            if (l19 > epsilon)
                return false;
            aabbRayWeight = 0;
            aabbRay.X = (int)l15;
            aabbRay.Y = (int)l17;
            aabbRayResult = true;
            return true;
        }
        return false;
    }

    private static bool M9c(int i, int i2, int i3, int i4, int i5, int i6, int i7, int i8)
    {
        long j = i - i6;
        long j2 = i2 - i7;
        long j3 = i3 * j + i4 * j2 >> 16;
        if (j3 >= 0)
            return false;
        long j4 = (j * j + j2 * j2 >> 16) - (i8 * i8 << 16);
        if (j4 <= 0)
        {
            aabbRayWeight = 0;
            int sqrt = (int)Math.Sqrt(j * j + j2 * j2);
            int i9 = 0;
            int i10 = 0;
            if (sqrt != 0)
            {
                i9 = (int)((i8 * j << 16) / sqrt);
                i10 = (int)((i8 * j2 << 16) / sqrt);
            }
            aabbRay.X = i9 + i6;
            aabbRay.Y = i10 + i7;
            aabbRayResult = true;
            return true;
        }
        long j5 = (j3 * j3 >> 16) - j4;
        if (j5 < 0)
            return false;
        long sqrt2 = -j3 - (int)Math.Sqrt(j5 << 16);
        if (sqrt2 > i5)
            return false;
        aabbRayWeight = (int)((sqrt2 << 16) / i5);
        aabbRay.X = (int)(i + (i3 * sqrt2 >> 16));
        aabbRay.Y = (int)((sqrt2 * i4 >> 16) + i2);
        aabbRayResult = false;
        return true;
    }

    public static void UpdateScreenSpaceConstants()
    {
        BALL_DIMENS_SCREENSPACE[0] = (BALL_DIMENS[0] * ScreenSpaceMatrix.M00 >> 16) + 1;
        BALL_DIMENS_SCREENSPACE[1] = (BALL_DIMENS[1] * ScreenSpaceMatrix.M00 >> 16) + 1;
        BALL_DIMENS_SCREENSPACE[2] = (BALL_DIMENS[2] * ScreenSpaceMatrix.M00 >> 16) + 1;
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        base.Draw(graphics, rootMatrix);
        int fbBallCY;
        int fbBallCX;
        Graphics graphics2;
        if (IsVisible)
        {
            LoadObjectMatrixToTarget(out TmpObjMatrix);
            Matrix.MultMatrices(rootMatrix, TmpObjMatrix, out Matrix.Temp);
            Matrix.Temp.MulVector(LocalObjectMatrix.Translation); // ???
            int ballX = Matrix.Temp.TranslationX >> 16;
            int ballY = Matrix.Temp.TranslationY >> 16;
            Vector2I vectorMulRsl = Matrix.Temp.MulVector(-(BALL_DIMENS[(int)BallForme] << 16), BALL_DIMENS[(int)BallForme] << 16);
            int ballTLX = vectorMulRsl.X >> 16;
            int ballTLY = vectorMulRsl.Y >> 16;
            int ballHalfWidthX = ballX - ballTLX;
            int ballHalfWidthY = ballY - ballTLY;
            if (isPlayer)
            {
                switch (BallForme)
                {
                    case Forme.BOUNCE:
                        {
                            Graphics orgGraphics = GameRuntime.GetGraphicsObj();
                            if (FadeColor != Color.Zero)
                            {
                                BounceGame.BallGraphics.SetColor(0x0000FF);
                                BounceGame.BallGraphics.FillRect(0, 0, BounceGame.BallFramebuffer.Width, BounceGame.BallFramebuffer.Height);
                                fbBallCX = BounceGame.BallFramebuffer.Width >> 1;
                                fbBallCY = BounceGame.BallFramebuffer.Height >> 1;
                                graphics2 = BounceGame.BallGraphics;
                                GameRuntime.SetGraphics(BounceGame.BallGraphics);
                            }
                            else
                            {
                                fbBallCY = ballY;
                                fbBallCX = ballX;
                                graphics2 = graphics;
                            }
                            for (int axisA = 0; axisA < 4; axisA++)
                            {
                                stretchDirBalance[axisA] = 0;
                                for (int axisB = 0; axisB < 4; axisB++)
                                {
                                    if (axisA != axisB)
                                        stretchDirBalance[axisA] -= stretchResultsAbs[axisB] >> 1;
                                }
                            }
                            for (int axis = 0; axis < 4; axis++)
                                stretchBuffer[axis] = 999;
                            FillStretchedCircle(fbBallCX, fbBallCY, BALL_DIMENS_SCREENSPACE[(int)BallForme] + 2, BALL_DIMENS_SCREENSPACE[(int)BallForme] + 2, unchecked((int)0xFF000000), graphics2, false, false);
                            FillStretchedCircle(fbBallCX, fbBallCY, BALL_DIMENS_SCREENSPACE[(int)BallForme], BALL_DIMENS_SCREENSPACE[(int)BallForme], BOUNCE_PRIMARY_COLOR, graphics2, true, false);
                            int innerRadius = BALL_DIMENS_SCREENSPACE[(int)BallForme] * 90 / 100;
                            FillStretchedCircle(fbBallCX, fbBallCY + 2, innerRadius, BALL_DIMENS_SCREENSPACE[(int)BallForme], BOUNCE_SECONDARY_COLOR, graphics2, false, false);
                            FillStretchedCircle(fbBallCX + 1, fbBallCY - 1, innerRadius, BALL_DIMENS_SCREENSPACE[(int)BallForme], BOUNCE_PRIMARY_COLOR, graphics2, false, true);
                            Matrix.Temp.SetRotation(5.3f);
                            int i16 = stretchBuffer[1] >> 1;
                            int i17 = stretchBuffer[1] - i16 + 1;
                            int highlightX = fbBallCX + (Matrix.Temp.M00 * i17 >> 16);
                            int highlightY = fbBallCY + (i17 * Matrix.Temp.M10 >> 16);
                            for (int i20 = 0; i20 < 12; i20++)
                            {
                                int abs = Math.Abs(i20 & 1) + 31;
                                int i21 = 100 - (i20 << 2);
                                Matrix.Temp.SetRotation(rotation + i20 * 0.8f);
                                int posBase = stretchResultsAbs[1] + stretchDirBalance[1] >> 10;
                                for (int i22 = 0; i22 < 4; i22++)
                                {
                                    int i23 = (stretchBuffer[i22] - (stretchBuffer[i22] >> 1 >> 1)) * i21 / 100;
                                    int i24 = (Matrix.Temp.M00 * i23 >> 16) + fbBallCX;
                                    int i25 = (i23 * Matrix.Temp.M10 >> 16) + fbBallCY;
                                    if (i22 == 0)
                                    {
                                        if (i24 >= fbBallCX && i25 <= fbBallCY)
                                        {
                                            GameRuntime.DrawImageRes(i24, i25, abs);
                                            break;
                                        }
                                    }
                                    else if (i22 == 1)
                                    {
                                        if (i24 <= fbBallCX && i25 <= fbBallCY)
                                        {
                                            GameRuntime.DrawImageRes(i24, posBase + i25, abs);
                                            break;
                                        }
                                    }
                                    else if (i22 == 2)
                                    {
                                        if (i24 <= fbBallCX && i25 >= fbBallCY)
                                        {
                                            GameRuntime.DrawImageRes(i24, posBase + i25, abs);
                                            break;
                                        }
                                    }
                                    else if (i24 >= fbBallCX && i25 >= fbBallCY)
                                    {
                                        GameRuntime.DrawImageRes(i24, posBase + i25, abs);
                                        break;
                                    }
                                    i22++;
                                }
                            }
                            GameRuntime.DrawImageRes(fbBallCX, fbBallCY, 17);
                            FillStretchedCircle(highlightX, highlightY, i16 >> 1, BALL_DIMENS_SCREENSPACE[(int)BallForme], BOUNCE_HIGHLIGHT_COLOR, graphics2, false, false);
                            GameRuntime.DrawImageRes(fbBallCX, fbBallCY, 28);
                            if (FadeColor != Color.Zero) // TODO: We could make this more efficient by adding a tint argument to DrawRegion.
                            {
                                GameRuntime.SetGraphics(orgGraphics);
                                BounceGame.BallFramebuffer.GetRGB(BounceGame.BallFramebufferRGB);
                                int rgbIdx = 0;
                                Color key = Color.FromARGB(0xFF0000FF);
                                for (int y = 0; y < BounceGame.BallFramebuffer.Height; y++)
                                {
                                    for (int x = 0; x < BounceGame.BallFramebuffer.Width; x++)
                                    {
                                        if (BounceGame.BallFramebufferRGB[rgbIdx] == key)
                                            BounceGame.BallFramebufferRGB[rgbIdx] = Color.Zero;
                                        else
                                            BounceGame.BallFramebufferRGB[rgbIdx] = Color.Subtract(BounceGame.BallFramebufferRGB[rgbIdx], FadeColor);
                                        rgbIdx++;
                                    }
                                }
                                GameRuntime.GetGraphicsObj().DrawRGB(BounceGame.BallFramebufferRGB,
                                    ballX - (BounceGame.BallFramebuffer.Width >> 1),
                                    ballY - (BounceGame.BallFramebuffer.Height >> 1),
                                    BounceGame.BallFramebuffer.Width,
                                    BounceGame.BallFramebuffer.Height
                                );
                            }
                            if (BounceGame.IsSuperBounceUnlocked && !BounceGame.LevelPaused)
                            {
                                superBounceParticleTimer += GameRuntime.UpdateDelta * GameRuntime.GetUpdatesPerDraw();
                                if (superBounceParticleTimer > 150)
                                {
                                    BounceGame.SuperBounceParticle.EmitTrail(EventObject.EventVars[4] / 120, BounceGame.BounceObj.LocalObjectMatrix.TranslationX, BounceGame.BounceObj.LocalObjectMatrix.TranslationY, BALL_DIMENS[0] << 15, 0, 0, 0, 0, 1000, 166);
                                    superBounceParticleTimer = 0;
                                }
                            }
                            ballY = fbBallCY;
                            ballX = fbBallCX;
                            break;
                        }
                    case Forme.BUMPY_CRACKS:
                        {
                            // optimized method
                            int degrees = (int)float.RadiansToDegrees(rotation) % 360;
                            if (degrees < 0)
                                degrees += 360;
                            /*
                            while (degrees < 0) { // this could get real slow real quick
                                degrees += 360;
                            }
                            while (degrees > 359) {
                                degrees -= 360;
                            }*/
                            GameRuntime.DrawImageRes(ballX, ballY, BUMPY_CRACKS_ROTATION_SPRITES[31 - (int)(degrees / 11.25f)]);
                            break;
                        }
                    case Forme.WOLLY:
                        {
                            int wollyCenterWidth = ballHalfWidthX >> 2;
                            int centerCircleX = ballX - wollyCenterWidth;
                            int centerCircleY = ballY - wollyCenterWidth;
                            int ballWidth = BALL_DIMENS_SCREENSPACE[(int)BallForme] << 1;
                            graphics.SetColor(0x000000); // outline
                            graphics.FillArc(ballTLX - 2, ballTLY - 2, ballWidth + 4, ballWidth + 4, 0, 360);
                            for (int wollySegment = 0; wollySegment < 6; wollySegment++)
                            {
                                graphics.SetColor(WOLLY_SEGMENT_COLORS[wollySegment]);
                                graphics.FillArc(ballTLX, ballTLY, ballWidth, ballWidth, wollySegment * 60 - (int)float.RadiansToDegrees(rotation), 60);
                            }
                            graphics.SetColor(WOLLY_SEGMENT_COLORS[0]);
                            graphics.FillArc(centerCircleX, centerCircleY, wollyCenterWidth << 1, wollyCenterWidth << 1, 0, 360);
                            GameRuntime.DrawImageRes(ballX, ballY, 5);
                            break;
                        }
                }
                if (EyeFrame == 1 || BounceGame.GetPlayerState() == BounceGame.PlayerState.LOSE_UPDATE)
                    GameRuntime.DrawImageRes(ballX, ballY, 20); // owowowowow
                else if (EyeFrame == 2 || EyeFrame == 3)
                {
                    int eyeImageId = 462;
                    int frameCount = 2;
                    if (EyeFrame == 3)
                    {
                        eyeImageId = 447;
                        frameCount = 4;
                    }
                    int frameInvIndex = idleAnimTimer * frameCount / 600;
                    if (frameInvIndex > frameCount - 1)
                        frameInvIndex = frameCount - 1;
                    GameRuntime.DrawAnimatedImageRes(ballX, ballY, eyeImageId, frameCount - 1 - frameInvIndex);
                }
                else if (EyeFrame >= 4 && EyeFrame <= 8)
                    GameRuntime.DrawAnimatedImageRes(ballX, ballY, EYE_ANIMATION_IMAGE_IDS[EyeFrame - 4], 0);
                else if (BounceGame.GetPlayerState() == BounceGame.PlayerState.WIN_UPDATE)
                    GameRuntime.DrawAnimatedImageRes(ballX, ballY, 465, 0);
            }
            else
            {
                graphics.SetColor(BOUNCE_PRIMARY_COLOR);
                graphics.FillArc(ballTLX, ballTLY, ballHalfWidthX << 1, ballHalfWidthY << 1, 0, 360);
            }
        }
        DebugDraw(graphics, 0xFFCC00, rootMatrix);
    }

    private void StretchInDirection(int dir, int magnitude)
    {
        if (magnitude > 0)
        {
            if (magnitude > 70)
                magnitude = 70;
        }
        else if (magnitude < 0 && magnitude < -70)
            magnitude = -70;
        reqStretchMagnitudes[dir] += magnitude;
    }

    private void ResetStretch()
    {
        for (int i = 0; i < 4; i++)
        {
            reqStretchMagnitudes[i] = 0;
            stretchResults[i] = 0;
            stretchResultsAbs[i] = 0;
            stretchMagnitudes[i] = 0;
        }
    }

    private void FillStretchedCircle(int cx, int cy, int radius, int stretchRadius, int color, Graphics graphics, bool writeStretchBuffer, bool z2)
    {
        graphics.SetColor(color);
        int i6 = 2;
        int axis = 0;
        while (axis < 4)
        {
            int width = radius - (stretchResultsAbs[i6] + stretchDirBalance[i6] >> 10) * radius / stretchRadius;
            int height = radius - (stretchResultsAbs[axis >> 1] + stretchDirBalance[axis >> 1] >> 10) * radius / stretchRadius;
            if (z2)
                width++;
            if (writeStretchBuffer)
            {
                stretchBuffer[axis] = width;
                if (height < width)
                    stretchBuffer[axis] = height;
            }
            graphics.FillArc(cx - width, cy - height + (stretchResultsAbs[1] + stretchDirBalance[1] >> 10), width << 1, height << 1, axis * 90, 90);
            int i10 = axis == 0 ? i6 + 1 : i6;
            if (axis == 2)
                i10--;
            axis++;
            i6 = i10;
        }
    }

    public void ResetPhysics()
    {
        CurXVelocity = 0.0f;
        CurYVelocity = 0.0f;
        PushX = 0.0f;
        PushY = 0.0f;
        GravityX = 0.0f;
        GravityY = 0.0f;
        TorqueX = 0.0f;
        TorqueY = 0.0f;
    }

    public override void UpdatePhysics()
    {
        float f;
        float f2;
        SetIsDirtyRecursive();
        base.UpdatePhysics();
        ObjectMatrixIsDirty = true;
        if (EnablePhysics)
        {
            GravityX += BASE_GRAVITY_X * GRAVITY[(int)BallForme];
            GravityY += BASE_GRAVITY_Y * GRAVITY[(int)BallForme];
            if (isPlayer)
            {
                if (!ReqSkipAccelStretch)
                {
                    float xaccel = CurXVelocity - LastXVelocity;
                    float yaccel = CurYVelocity - LastYVelocity;
                    if (xaccel > 0.0f)
                        StretchInDirection(3, ((int)xaccel >> 2 << 1) / 3);
                    else
                        StretchInDirection(2, (-(int)xaccel >> 2 << 1) / 3);
                    if (yaccel > 0.0f)
                        StretchInDirection(1, ((int)yaccel >> 2 << 1) / 3);
                    else
                        StretchInDirection(0, (-(int)yaccel >> 2 << 1) / 3);
                }
                ReqSkipAccelStretch = false;
            }
            float motionDelta = GameRuntime.UpdateDelta * 0.001f;
            if (isPlayer)
            {
                LastXVelocity = CurXVelocity;
                LastYVelocity = CurYVelocity;
            }
            float invGravity = 1.0f / GRAVITY[(int)BallForme];
            CurXVelocity += GravityX * invGravity * motionDelta;
            CurYVelocity += GravityY * invGravity * motionDelta;
            CurXVelocity += PushX * invGravity;
            CurYVelocity += PushY * invGravity;
            LocalObjectMatrix.TranslationX += LP32.FP32ToLP32(CurXVelocity * motionDelta);
            LocalObjectMatrix.TranslationY += LP32.FP32ToLP32(CurYVelocity * motionDelta);
            GravityX = 0.0f;
            GravityY = 0.0f;
            PushX = 0.0f;
            PushY = 0.0f;
            float f7 = TorqueX * slopeSinAbs + TorqueY * slopeCosAbs;
            float f8 = slopeSinAbs * f7;
            float f9 = slopeCosAbs * f7;
            if (slopeSinAbs * TorqueX + slopeCosAbs * TorqueY >= 0.0f)
            {
                f = TorqueX - f8;
                f2 = TorqueY - f9;
            }
            else
            {
                f = TorqueX + f8;
                f2 = TorqueY + f9;
            }
            float sqrt = motionDelta * ((float)Math.Sqrt(f * f + f2 * f2) / BALL_DIMENS[(int)BallForme]);
            rotation += f2 * slopeSinAbs - f * slopeCosAbs > 0.0f ? -sqrt : sqrt;
            airTimeCounter += GameRuntime.UpdateDelta * 0.001f;
            if (airTimeCounter > 0.25f)
                IsGrounded = false;
            CurVelocity = (float)Math.Sqrt((double)(CurXVelocity * CurXVelocity + CurYVelocity * CurYVelocity));
            if (CurVelocity > 999.0f) // terminal velocity
            {
                float invVelocity = 999.0f / CurVelocity;
                CurXVelocity *= invVelocity;
                CurYVelocity *= invVelocity;
                CurVelocity = 999.0f;
            }
            if (isPlayer)
            {
                for (int axis = 0; axis < 4; axis++)
                {
                    // this part of the code was reworked to be 16.16 fixed point in order
                    // for stretching to work properly on very high framerates
                    // since part of the formula is to divide by 65536 (shr 16), a lot of precision
                    // was lost at low deltas, which made stretching very weird on 1000FPS
                    // the long multiplication loses a bit of performance in exchange for making it work properly
                    stretchMagnitudes[axis] += reqStretchMagnitudes[axis] << 16;
                    reqStretchMagnitudes[axis] = 0;
                    if (stretchMagnitudes[axis] > 70 << 16)
                        stretchMagnitudes[axis] = 70 << 16;
                    else if (stretchMagnitudes[axis] < -70 << 16)
                        stretchMagnitudes[axis] = -70 << 16;
                    stretchResults[axis] += stretchMagnitudes[axis] * GameRuntime.UpdateDelta;
                    stretchResultsAbs[axis] = (stretchResults[axis] >> 16) * ScreenSpaceMatrix.M00 >> 16;
                    stretchMagnitudes[axis] += (int)(GameRuntime.UpdateDelta * 7L * -stretchResults[axis] >> 16);
                    stretchMagnitudes[axis] -= (int)(GameRuntime.UpdateDelta * 120L * stretchMagnitudes[axis] >> 16);
                }
            }
            if (isPlayer)
            {
                if (idleAnimTimer > 0)
                {
                    idleAnimTimer -= GameRuntime.UpdateDelta;
                    IdleAnimStartTimer = 3000;
                    if (idleAnimTimer <= 0)
                    {
                        if (EyeFrame < 2 || EyeFrame > 8)
                        {
                            idleAnimTimer = 0;
                            EyeFrame = 0;
                        }
                        else
                        {
                            int abs = Math.Abs(BounceGame.RNG.NextInt() % 3) == 0 ? 3 : Math.Abs(BounceGame.RNG.NextInt() % 6) + 3;
                            idleAnimTimer = 1500;
                            EyeFrame = abs;
                            if (abs == 3)
                                idleAnimTimer = 600;
                        }
                    }
                }
                else if (BounceGame.GetPlayerState() == BounceGame.PlayerState.PLAY && BounceGame.GetControllerState() == BounceGame.Controller.NORMAL && Math.Abs(CurXVelocity) < 40.0f && Math.Abs(CurYVelocity) < 40.0f)
                {
                    IdleAnimStartTimer -= GameRuntime.UpdateDelta;
                    if (IdleAnimStartTimer <= 0)
                    {
                        idleAnimTimer = 600;
                        EyeFrame = 2;
                        IdleAnimStartTimer = 3000;
                    }
                }
                if (Math.Abs(CurXVelocity) >= 40.0f || Math.Abs(CurYVelocity) >= 40.0f)
                {
                    if (EyeFrame != 1)
                        EyeFrame = 0;
                    IdleAnimStartTimer = 3000;
                }
            }
            if (FadeColor != Color.Zero)
            {
                int fadeAlpha = FadeColor.ToARGB() >>> 24;
                int alphaDecrement = GameRuntime.UpdateDelta / 2;
                if (alphaDecrement < 1)
                    alphaDecrement = 1;
                int newFadeAlpha = fadeAlpha - alphaDecrement;
                if (newFadeAlpha < 0)
                    newFadeAlpha = 0;
                FadeColor = Color.FromARGB((uint)(newFadeAlpha << 24));
            }
        }
    }

    public void UpdateDeathAnimation()
    {
        SetIsDirtyRecursive();
        base.UpdatePhysics();
        ResetStretch();
        int i = 3000 - BounceGame.ExitWaitTimer;
        if (i <= 1000)
            LocalObjectMatrix.TranslationY = (BounceGame.SIN_COS_TABLE[i / 5 % 360] << 14) + BounceGame.DeathBaseY;
        else
            LocalObjectMatrix.TranslationY = BounceGame.DeathBaseY + (BounceGame.SIN_COS_TABLE[200] << 14) - (i - 1000) * 22500;
    }

    public void Jump(bool small)
    {
        if (slopeCosAbs > MAX_JUMP_SLOPE_INV[(int)BallForme] && IsGrounded)
        {
            if (small)
                PushY += JUMP_ACCELERATION[(int)BallForme] / 2.0f;
            else
                PushY += JUMP_ACCELERATION[(int)BallForme];
            IsGrounded = false;
            if (isPlayer)
            {
                if (small)
                {
                    StretchInDirection(0, -27);
                    StretchInDirection(1, -27);
                }
                else
                {
                    StretchInDirection(0, -53);
                    StretchInDirection(1, -53);
                }
            }
        }
    }

    public void MoveLeft()
    {
        float speedBoost = 0.0f;
        if (BounceGame.IsSuperBounceUnlocked && BallForme == Forme.BOUNCE)
            speedBoost = 50.0f;
        if (IsGrounded)
        {
            if (CurXVelocity > -MAXIMUM_MOVEMENT_SPEED[(int)BallForme] - speedBoost)
                GravityX -= speedBoost + MOVEMENT_ACCELERATION[(int)BallForme];
        }
        else if (CurXVelocity > -MAXIMUM_MOVEMENT_SPEED[(int)BallForme] - speedBoost)
            GravityX -= (speedBoost + MOVEMENT_ACCELERATION[(int)BallForme]) / MIDAIR_MOVEMENT_SUPPRESSION[(int)BallForme];
    }

    public void MoveRight()
    {
        float speedBoost = 0.0f;
        if (BounceGame.IsSuperBounceUnlocked && BallForme == Forme.BOUNCE)
            speedBoost = 50.0f;
        if (IsGrounded)
        {
            if (CurXVelocity < MAXIMUM_MOVEMENT_SPEED[(int)BallForme] + speedBoost)
                GravityX += speedBoost + MOVEMENT_ACCELERATION[(int)BallForme];
        }
        else if (CurXVelocity < MAXIMUM_MOVEMENT_SPEED[(int)BallForme] + speedBoost)
            GravityX += (speedBoost + MOVEMENT_ACCELERATION[(int)BallForme]) / MIDAIR_MOVEMENT_SUPPRESSION[(int)BallForme];
    }

    public void CycleForme()
    {
        BallForme++;
        if (BallForme > Forme.WOLLY)
            BallForme = 0;
        if ((int)BallForme > BounceGame.GetUnlockedFormeCount())
            BallForme = 0;
        Initialize();
    }
}
