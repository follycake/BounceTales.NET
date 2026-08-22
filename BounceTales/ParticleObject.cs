using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class ParticleObject : GameObject
{
    public enum Type
    {
        SPRITE_4DIR = 0, // up/down/left/right
        BUBBLES = 1,
        SPRITE_RANDOM = 2,
        SPRITE_BY_PARTICLE_NO = 3,
        SHOWER = 4,
        COLLAPSE = 5, // collapses back a little after expanding
        TRAIL = 6,
        SPRITE_2DIR = 7 // vertical/horizontal
    }

    public const byte TYPEID = 11;

    // Global state
    private static bool existAnyParticle;
    //private static int[] pointRotationResult = new int[2];

    // Parameters
    private readonly int[] imageIDs;
    private readonly Type type;

    private readonly Vector2I accel;
    private readonly Vector2I ambientVelocity;
    private readonly int falloff;

    private readonly int animationLifespan;

    private readonly int maxParticleCount;

    // State
    public int ParticleCount;

    private readonly int[] particleLifespans;
    private readonly Vector2I[] particlePositions;
    private readonly Vector2I[] velocity;
    private readonly byte[] particleImageIndices;

    // State - shower
    private readonly byte[] showerDir;
    private readonly short[] showerTimer;

    // State - bubble
    public int BubblePopY;
    public int MaxVelocityY;

    public ParticleObject(int maxParticles, int accelX, int accelY, int ambientVelocityX, int ambientVelocityY, int falloff, Type type, int[] spriteIDs, int animationLifespan, sbyte zCoord) : base(TYPEID)
    {
        if (!existAnyParticle)
            existAnyParticle = true;
        particleLifespans = new int[maxParticles];
        particlePositions = new Vector2I[maxParticles];
        velocity = new Vector2I[maxParticles];
        particleImageIndices = new byte[maxParticles];
        if (type == Type.SHOWER)
        {
            showerTimer = new short[maxParticles];
            showerDir = new byte[maxParticles];
            for (int i = 0; i < maxParticles; i++)
                showerTimer[i] = 0;
        }
        maxParticleCount = maxParticles;
        accel = new(accelX, accelY);
        ambientVelocity = new(ambientVelocityX, ambientVelocityY);
        this.falloff = falloff;
        ParticleCount = -1;
        this.type = type;
        imageIDs = spriteIDs;
        this.animationLifespan = animationLifespan;
        ZCoord = zCoord;
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        bool lifespanEnd;
        int i;
        int delta = GameRuntime.UpdateDelta * GameRuntime.GetUpdatesPerDraw();
        int particleIndex = 0;
        while (particleIndex < ParticleCount + 1)
        {
            particleLifespans[particleIndex] = particleLifespans[particleIndex] - delta;
            if (particleLifespans[particleIndex] > 0)
            {
                if (falloff != 0)
                    velocity[particleIndex] -= velocity[particleIndex] * falloff * delta >> 14;
                velocity[particleIndex] += accel * delta;
                if (type == Type.BUBBLES && velocity[particleIndex].Y > MaxVelocityY)
                    velocity[particleIndex].Y = MaxVelocityY;
                else if (type == Type.SHOWER)
                {
                    showerTimer[particleIndex] = (short)(showerTimer[particleIndex] - delta);
                    if (showerTimer[particleIndex] <= 0)
                    {
                        showerTimer[particleIndex] = (short)(Math.Abs(BounceGame.RNG.NextInt() % 200) + 300);
                        showerDir[particleIndex] = (byte)(Math.Abs(BounceGame.RNG.NextInt()) & 3);
                    }
                    int angle = delta / 2;
                    if (showerDir[particleIndex] == 1)
                        velocity[particleIndex] = RotatePoint(angle, velocity[particleIndex]);
                    else if (showerDir[particleIndex] == 2)
                        velocity[particleIndex] = RotatePoint(359 - angle, velocity[particleIndex]);
                }
                else if (type == Type.COLLAPSE)
                {
                    // FIXME: delta/5 will break on high framerates
                    velocity[particleIndex] = RotatePoint(delta / 5, velocity[particleIndex]);
                }
                particlePositions[particleIndex] += (velocity[particleIndex] + ambientVelocity) * delta >> 4;
                lifespanEnd = false;
            }
            else
                lifespanEnd = true;
            if (type == Type.BUBBLES && particlePositions[particleIndex].Y >= BubblePopY)
                lifespanEnd = true;
            if (!lifespanEnd)
            {
                int frameCount = GameRuntime.GetImageAnimationFrameCount(imageIDs[particleImageIndices[particleIndex]]);
                int frame = particleLifespans[particleIndex] * frameCount / animationLifespan;
                if (frame > frameCount - 1)
                    frame = frameCount - 1;
                Vector2I pos = rootMatrix.MulVector(particlePositions[particleIndex]);
                GameRuntime.DrawAnimatedImageRes(pos.X >> 16, pos.Y >> 16, imageIDs[particleImageIndices[particleIndex]], frameCount - 1 - frame);
                i = particleIndex;
            }
            else if (particleIndex == ParticleCount)
            {
                ParticleCount--;
                i = particleIndex;
            }
            else
            {
                particleLifespans[particleIndex] = particleLifespans[ParticleCount];
                particlePositions[particleIndex] = particlePositions[ParticleCount];
                velocity[particleIndex] = velocity[ParticleCount];
                ParticleCount--;
                i = particleIndex - 1;
            }
            particleIndex = i + 1;
        }
    }

    private static Vector2I RotatePoint(int angleDeg, Vector2I point)
    {
        short sin = BounceGame.SIN_COS_TABLE[angleDeg % 360];
        short cos = BounceGame.SIN_COS_TABLE[(angleDeg + 90) % 360];
        return new Vector2I((point.X * cos - point.Y * sin) / 360, (sin * point.X + cos * point.Y) / 360);
    }

    public void AttachToObject(GameObject obj)
    {
        SetParent(obj);
        LocalObjectMatrix.M00 = LP32.ONE;
        LocalObjectMatrix.M01 = 0;
        LocalObjectMatrix.TranslationX = 0;
        LocalObjectMatrix.M10 = 0;
        LocalObjectMatrix.M11 = LP32.ONE;
        LocalObjectMatrix.TranslationY = 0;
        RenderCalcMatrix = LocalObjectMatrix;
        Initialize();
    }

    public void EmitCircle(int count, int posX, int posY, int dirScaleX, int dirScaleY, int lifespanBase, int lifespanRange)
    {
        if (count + 1 + ParticleCount > maxParticleCount)
            count = maxParticleCount - ParticleCount - 1;
        for (int i = 0; i < count; i++)
        {
            int sinIdx = i * 360 / count;
            int cosIdx = sinIdx + 90 >= 360 ? sinIdx + 90 - 360 : sinIdx + 90;
            ParticleCount++;
            particleLifespans[ParticleCount] = lifespanRange != 0 ? BounceGame.RNG.NextInt() % lifespanRange + lifespanBase : lifespanBase;
            particlePositions[ParticleCount] = new(posX, posY);
            velocity[ParticleCount] = new(BounceGame.SIN_COS_TABLE[sinIdx] * dirScaleX, BounceGame.SIN_COS_TABLE[cosIdx] * dirScaleX); // Is this intentional? (dirScaleX is used for the Y component)
            if (type == Type.SPRITE_BY_PARTICLE_NO)
                particleImageIndices[ParticleCount] = (byte)(i % imageIDs.Length);
            else if (type == Type.SHOWER && Equals(BounceGame.WinParticle))
            {
                byte abs = (byte)Math.Abs(BounceGame.RNG.NextInt() % imageIDs.Length);
                if (abs > 1)
                    abs = (byte)Math.Abs(BounceGame.RNG.NextInt() % imageIDs.Length);
                particleImageIndices[ParticleCount] = abs;
            }
            else
                particleImageIndices[ParticleCount] = (byte)Math.Abs(BounceGame.RNG.NextInt() % imageIDs.Length);
        }
    }

    public void EmitBlast(int count, int posX, int posY, int directionScaleBase, int directionScaleRange, int directionX, int directionY, int directionBias, int lifespanBase, int lifespanRange)
    {
        if (ParticleCount + count + 1 > maxParticleCount)
            count = maxParticleCount - ParticleCount - 1;
        for (int i = 0; i < count; i++)
        {
            int velocityScale = directionScaleRange != 0 ? BounceGame.RNG.NextInt() % directionScaleRange + directionScaleBase : directionScaleBase;
            ParticleCount++;
            particleLifespans[ParticleCount] = lifespanRange != 0 ? BounceGame.RNG.NextInt() % lifespanRange + lifespanBase : lifespanBase;
            particlePositions[ParticleCount] = new(posX, posY);
            velocity[ParticleCount] = new(
                ((BounceGame.RNG.NextInt() % directionBias << 10) + directionX) * velocityScale >> 8,
                ((BounceGame.RNG.NextInt() % directionBias << 10) + directionY) * velocityScale >> 8
            );
            particleImageIndices[ParticleCount] = (byte)Math.Abs(BounceGame.RNG.NextInt() % imageIDs.Length);
        }
    }

    public void EmitBurst(int count, int posX, int posY, int dispBase, int dispRange, int dirAngleBase, int dirAngleRange, int lifespanBase, int lifespanRange)
    {
        if (count + 1 + ParticleCount > maxParticleCount)
            count = maxParticleCount - ParticleCount - 1;
        for (int i = 0; i < count; i++)
        {
            int dirAngleRng = dirAngleBase + BounceGame.RNG.NextInt() % (dirAngleRange / 2 + 1); // divide by two in order to make negative/positive add up to range
            int dirAngleNormTemp = dirAngleRng - (dirAngleRng >= 360 ? 360 : 0);
            int dirAngle = dirAngleNormTemp + (dirAngleNormTemp < 0 ? 360 : 0);
            int dirAngleCosIdx = dirAngle + 90 >= 360 ? dirAngle - 270 : dirAngle + 90;
            int rndDirScale = dispRange != 0 ? BounceGame.RNG.NextInt() % dispRange + dispBase : dispBase;
            ParticleCount++;
            particleLifespans[ParticleCount] = lifespanRange != 0 ? BounceGame.RNG.NextInt() % lifespanRange + lifespanBase : lifespanBase;
            particlePositions[ParticleCount] = new(posX, posY);
            velocity[ParticleCount] = new(BounceGame.SIN_COS_TABLE[dirAngle] * rndDirScale, BounceGame.SIN_COS_TABLE[dirAngleCosIdx] * rndDirScale);
            if (type == Type.SPRITE_4DIR)
            {
                if (dirAngle >= 35 && dirAngle <= 90)
                    particleImageIndices[ParticleCount] = 2;
                else if (dirAngle < 35)
                    particleImageIndices[ParticleCount] = 3;
                else if (dirAngle > 325)
                    particleImageIndices[ParticleCount] = 1;
                else
                    particleImageIndices[ParticleCount] = 0;
            }
            else if (type == Type.SPRITE_2DIR)
            {
                if (dirAngleBase == 0 || dirAngleBase == 180)
                    particleImageIndices[ParticleCount] = 0;
                else
                    particleImageIndices[ParticleCount] = 1;
            }
            else
                particleImageIndices[ParticleCount] = (byte)Math.Abs(BounceGame.RNG.NextInt() % imageIDs.Length);
        }
    }

    public void EmitIndependentBursts(int count, int posXMin, int posYMin, int posXMax, int posYMax, int dispBase, int dispRange, int dirAngleBase, int dirAngleRange, int lifespanBase, int lifespanRange)
    {
        for (int i = 0; i < count; i++)
            EmitBurst(1, posXMin + Math.Abs(BounceGame.RNG.NextInt() % (posXMax - posXMin + 1)), posYMin + Math.Abs(BounceGame.RNG.NextInt() % (posYMax - posYMin + 1)), dispBase, dispRange, dirAngleBase, dirAngleRange, lifespanBase, lifespanRange);
    }

    public void EmitTrail(int count, int posX, int posY, int dim, int dispBase, int dispRange, int dirAngleBase, int dirAngleRange, int lifespanBase, int lifespanRange)
    {
        int dispX;
        int dispY;
        for (int i = 0; i < count; i++)
        {
            do
            {
                dispX = BounceGame.RNG.NextInt() % (dim * 2 + 1);
                dispY = BounceGame.RNG.NextInt() % (dim * 2 + 1);
            } while (dispX * dispX + dispY * dispY > dim * dim);
            EmitBurst(1, posX + dispX, posY + dispY, dispBase, dispRange, dirAngleBase, dirAngleRange, lifespanBase, lifespanRange);
        }
    }
}
