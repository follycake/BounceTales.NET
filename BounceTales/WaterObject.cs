using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class WaterObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 6;

    private const int COLOR_WATER = 0x441111EE;
    private const int COLOR_AIR_TUNNEL = 0x44000000;

    public enum Region : byte
    {
        SURFACE = 0,
        DEPTHS = 1
    }

    // Parameters - preset
    private int color;
    internal AABB area;

    private sbyte gravityXLeft;
    private sbyte gravityXRight;
    private sbyte gravityYTop;
    private sbyte gravityYBottom;

    // Parameters - calculated
    private Region region;
    internal int vertexCount;
    private int maxSplashX;

    internal int surfaceY;
    private short areaWidth;

    // State - splashes
    private int splashTimer;
    private sbyte[] splashYOffsets;
    private int splashLimit;

    private int[] splashIntensity;
    private int[] splashSpread;
    private sbyte[] splashDirections;
    private sbyte[] splashPermanence;
    private int[] splashSpeeds;
    private int[] splashXPos;

    // State - particles
    private int bounceBubbleTimer;
    private int ambientParticleTimer;

    public override int ReadData(byte[] data, int dataPos)
    {
        dataPos = base.ReadData(data, dataPos);
        area.MinX = ReadShort(data, dataPos);
        area.MaxY = ReadShort(data, dataPos + 2);
        area.MaxX = ReadShort(data, dataPos + 4);
        area.MinY = ReadShort(data, dataPos + 6);
        dataPos += 8;
        gravityYTop = (sbyte)data[dataPos++];
        gravityXRight = (sbyte)data[dataPos++];
        gravityYBottom = (sbyte)data[dataPos++];
        gravityXLeft = (sbyte)data[dataPos++];
        dataPos++; // skip alpha
        int colorAGB = 0x44000000 | (data[dataPos++] & 255) << 16 | (data[dataPos++] & 255) << 8;
        int red = data[dataPos++] & 255;
        color = colorAGB | red;
        if (red == 16)
            region = Region.DEPTHS;
        else
            region = Region.SURFACE;
        if (color != COLOR_AIR_TUNNEL)
            color = COLOR_WATER;
        areaWidth = (short)(area.MaxX - area.MinX);
        if (IsWater() && region == Region.SURFACE)
        {
            int maxSplashVerts = areaWidth * 50 / 100;
            splashYOffsets = new sbyte[maxSplashVerts];
            this.maxSplashX = maxSplashVerts << 12;
            ambientParticleTimer = 0;
            vertexCount = maxSplashVerts + 2;
            for (int i = 0; i < maxSplashVerts; i++)
                splashYOffsets[i] = 0;
            int maxSplashX = maxSplashVerts / 20;
            splashLimit = (maxSplashX << 1) + 2;
            splashIntensity = new int[splashLimit];
            splashSpread = new int[splashLimit];
            splashDirections = new sbyte[splashLimit];
            splashPermanence = new sbyte[splashLimit];
            splashSpeeds = new int[splashLimit];
            splashXPos = new int[splashLimit];
            for (int i = 0; i < splashLimit; i++)
                splashIntensity[i] = 0;
            for (int i = 2; i <= maxSplashX - 2; i++)
            {
                int length = (splashYOffsets.Length - 1 << 12) * i / maxSplashX;
                int intensity1 = (Math.Abs(BounceGame.RNG.NextInt() % 2) + 24 << 12 << 1) / 9;
                InsertSplash(intensity1, intensity1 * 3, -1, (Math.Abs(BounceGame.RNG.NextInt() % 2) + 10 << 12) / 3, length, 1);
                int intensity2 = (Math.Abs(BounceGame.RNG.NextInt() % 2) + 24 << 12 << 1) / 9;
                InsertSplash(intensity2, intensity2 * 3, 1, (Math.Abs(BounceGame.RNG.NextInt() % 2) + 10 << 12) / 3, length, 1);
            }
        }
        return dataPos;
    }

    public override void Initialize()
    {
        BBox.MinX = area.MinX << 16;
        BBox.MaxX = area.MaxX << 16;
        BBox.MinY = (area.MaxY << 16) - LP32.Int32ToLP32(30);
        BBox.MaxY = (area.MinY << 16) + LP32.Int32ToLP32(30);
        surfaceY = area.MinY << 16;
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        int airEmitDir;
        int airYMax;
        int airXMin;
        int airYMin;
        int airXMax;
        int baseSplash;
        int delta = GameRuntime.UpdateDelta * GameRuntime.GetUpdatesPerDraw();
        if (IsWater())
        {
            if (!BounceGame.LevelPaused)
            {
                ambientParticleTimer += delta;
                if (ambientParticleTimer > 150)
                {
                    LoadObjectMatrixToTarget(out TmpObjMatrix);
                    int swimPosXMin = TmpObjMatrix.TranslationX + (area.MinX << 16);
                    int swimPosXMax = TmpObjMatrix.TranslationX + (area.MaxX << 16);
                    int swimPosYMin = TmpObjMatrix.TranslationY + (area.MaxY << 16);
                    BounceGame.BubbleParticle.BubblePopY = TmpObjMatrix.TranslationY + (area.MinY << 16);
                    BounceGame.BubbleParticle.MaxVelocityY = 60000;
                    BounceGame.BubbleParticle.EmitIndependentBursts(1, swimPosXMin, swimPosYMin, swimPosXMax, swimPosYMin, 0, 0, 0, 0, 4000, 666);
                    ambientParticleTimer = 0;
                }
            }
            if (!BounceGame.LevelPaused && region == Region.SURFACE)
            {
                splashTimer += delta;
                Array.Fill<sbyte>(splashYOffsets, 0);
                for (int splashIdx = 0; splashIdx < splashLimit; splashIdx++)
                {
                    if (splashIntensity[splashIdx] != 0)
                    {
                        if (splashPermanence[splashIdx] != 1)
                        {
                            splashIntensity[splashIdx] -= delta * 28 >> 3;
                            splashSpread[splashIdx] -= delta * 12 >> 3;
                            splashSpeeds[splashIdx] -= delta * 4 >> 3;
                        }
                        splashXPos[splashIdx] += splashDirections[splashIdx] * splashSpeeds[splashIdx] * delta >> 6;
                        if (splashSpread[splashIdx] <= 0 || splashSpeeds[splashIdx] <= 0)
                            splashIntensity[splashIdx] = 0;

                        if (splashIntensity[splashIdx] <= 0)
                            splashIntensity[splashIdx] = 0;
                        else if (splashXPos[splashIdx] < 0 || splashXPos[splashIdx] >> 12 >= maxSplashX >> 12)
                        {
                            splashDirections[splashIdx] = (sbyte)-splashDirections[splashIdx]; // splash in the other direction
                            if (splashXPos[splashIdx] < 0)
                                splashXPos[splashIdx] = 0;
                            else if (splashXPos[splashIdx] >> 12 >= maxSplashX >> 12)
                                splashXPos[splashIdx] = (maxSplashX >> 12) - 1 << 12;

                            if (splashPermanence[splashIdx] != 1)
                            {
                                splashIntensity[splashIdx] -= (delta << 1) * 28 >> 3;
                                splashSpread[splashIdx] -= (delta << 1) * 12 >> 3;
                                splashSpeeds[splashIdx] -= (delta << 1) * 4 >> 3;
                            }
                        }
                        if (splashIntensity[splashIdx] > 0 && (baseSplash = splashXPos[splashIdx] >> 12) >= 0 && baseSplash < splashYOffsets.Length)
                        {
                            splashYOffsets[baseSplash] = (sbyte)(splashYOffsets[baseSplash] + (splashIntensity[splashIdx] >> 12));
                            int maxSpread = splashSpread[splashIdx] >> 12;
                            if (maxSpread == 0)
                                maxSpread = 1;
                            int i14 = 180 / maxSpread;
                            for (int spread = 1; spread < maxSpread; spread++)
                            {
                                int increment = splashIntensity[splashIdx] * (BounceGame.SIN_COS_TABLE[i14 * spread + 90] + 360 >> 1) / 360 >> 12;
                                if (baseSplash - spread >= 0)
                                    splashYOffsets[baseSplash - spread] = (sbyte)(splashYOffsets[baseSplash - spread] + increment);
                                if (baseSplash + spread < splashYOffsets.Length)
                                    splashYOffsets[baseSplash + spread] = (sbyte)(splashYOffsets[baseSplash + spread] + increment);
                            }
                        }
                    }
                }
                for (int i = 0; i < splashYOffsets.Length; i++)
                {
                    if (i > 0 && splashYOffsets[i - 1] < splashYOffsets[i] && i < splashYOffsets.Length - 1 && splashYOffsets[i + 1] < splashYOffsets[i])
                        splashYOffsets[i]--;
                }
            }
            LoadObjectMatrixToTarget(out TmpObjMatrix);
            Matrix.MultMatrices(rootMatrix, TmpObjMatrix, out Matrix.Temp);
            Vector2I min = Matrix.Temp.MulVector(area.Min << 16) >> 16;
            Vector2I max = Matrix.Temp.MulVector(area.Max << 16) >> 16;
            int width = max.X - min.X;
            if (region == Region.SURFACE)
            {
                /*int length = ((width << 8 << 10) / splashYOffsets.Length) >> 8;
                int[] xPoints = GeometryObject.TEMP_QUAD_XS;
                int[] yPoints = GeometryObject.TEMP_QUAD_YS;
                // seafloor
                xPoints[0] = maxx;
                yPoints[0] = maxy;
                xPoints[1] = minx;
                yPoints[1] = maxy;
                // surface
                int nPoints = 2;
                int vertIdx = 2;
                while (vertIdx < splashYOffsets.Length + 2)
                {
                    if (vertIdx > 2)
                    {
                        sbyte lastSplashY = splashYOffsets[vertIdx - 2];
                        for (int scan = vertIdx + 1; scan < splashYOffsets.Length + 2 && splashYOffsets[scan - 2] == lastSplashY; ++scan)
                            vertIdx++;
                    }
                    if (vertIdx == splashYOffsets.Length + 1)
                    {
                        xPoints[nPoints] = maxx;
                        yPoints[nPoints] = miny;
                    }
                    else
                    {
                        xPoints[nPoints] = minx + (((vertIdx - 2) * length) >> 10);
                        yPoints[nPoints] = miny - ((splashYOffsets[vertIdx - 2] * length) >> 10);
                    }
                    nPoints++;
                    vertIdx++;
                }
                directGraphics.FillPolygon(xPoints, yPoints, nPoints, BounceGame.GetStolenColorIfApplicable(color));*/

                // Not accurate, but good enough for rendering.
                // Instead of making a big polygon which would be expensive to triangulate, we make smaller quadrilaterals which are easier to render.
                int length = (width << 8 << 10) / splashYOffsets.Length >> 8;
                Vector2I p0 = new(min.X, max.Y);
                Vector2I p1 = min;
                Vector2I p2, p3;

                for (int i = 0; i < splashYOffsets.Length; i++)
                {
                    if (i > 0)
                    {
                        sbyte lastSplashY = splashYOffsets[i];
                        for (int scan = i + 1; scan < splashYOffsets.Length && splashYOffsets[scan] == lastSplashY; scan++)
                            i++;
                    }

                    if (i == splashYOffsets.Length - 1)
                    {
                        p2.X = max.X + 1;
                        p2.Y = min.Y;
                    }
                    else
                    {
                        p2.X = min.X + (i * length >> 10);
                        p2.Y = min.Y - (splashYOffsets[i] * length >> 10);
                    }
                    p3 = new(p2.X, max.Y);

                    graphics.FillQuad(p0, p1, p2, p3, Color.FromARGB(BounceGame.GetStolenColorIfApplicable(color)));

                    p0 = p3;
                    p1 = p2;
                }
            }
            else
            {
                /*int[] polyX = GeometryObject.TEMP_QUAD_XS;
                int[] polyY = GeometryObject.TEMP_QUAD_YS;
                polyX[0] = minx;
                polyY[0] = miny;
                polyX[1] = maxx;
                polyY[1] = miny;
                polyX[2] = maxx;
                polyY[2] = maxy;
                polyX[3] = minx;
                polyY[3] = maxy;
                directGraphics.FillPolygon(polyX, polyY, 4, BounceGame.GetStolenColorIfApplicable(color));*/
                graphics.FillRect(min.X, min.Y, max.X - min.X, max.Y - min.Y);
            }
        }
        else if (!BounceGame.LevelPaused)
        {
            // air tunnel
            ambientParticleTimer += delta;
            if (ambientParticleTimer > 150)
            {
                LoadObjectMatrixToTarget(out TmpObjMatrix);
                int leftX = TmpObjMatrix.TranslationX + (area.MinX << 16);
                int rightX = TmpObjMatrix.TranslationX + (area.MaxX << 16);
                int topY = TmpObjMatrix.TranslationY + (area.MaxY << 16);
                int bottomY = TmpObjMatrix.TranslationY + (area.MinY << 16);
                int horizontalGravity = Math.Abs(gravityXLeft);
                if (Math.Abs(gravityXRight) > horizontalGravity)
                    horizontalGravity = Math.Abs(gravityXRight);
                int verticalGravity = Math.Abs(gravityYTop);
                if (Math.Abs(gravityYBottom) > verticalGravity)
                    verticalGravity = Math.Abs(gravityYBottom);
                int dispBase;
                if (gravityXLeft + gravityXRight > 0)
                {
                    dispBase = horizontalGravity << 7;
                    airEmitDir = 90;
                    airYMax = bottomY;
                    airXMin = leftX;
                    airYMin = topY;
                    airXMax = leftX;
                }
                else if (gravityXLeft + gravityXRight < 0)
                {
                    dispBase = horizontalGravity << 7;
                    airEmitDir = 270;
                    airYMax = bottomY;
                    airXMin = rightX;
                    airYMin = topY;
                    airXMax = rightX;
                }
                else if (gravityYTop + gravityYBottom > 0)
                {
                    dispBase = verticalGravity << 7;
                    airEmitDir = 0;
                    airYMax = topY;
                    airXMin = leftX;
                    airYMin = topY;
                    airXMax = rightX;
                }
                else
                {
                    dispBase = verticalGravity << 7;
                    airEmitDir = 180;
                    airYMax = bottomY;
                    airXMin = leftX;
                    airYMin = bottomY;
                    airXMax = rightX;
                }
                BounceGame.AirTunnelParticle.EmitIndependentBursts(1, airXMin, airYMin, airXMax, airYMax, dispBase, dispBase / 8, airEmitDir, 0, 2000, 333);
                ambientParticleTimer = 0;
            }
        }
    }

    private bool IsWater()
    {
        return color != COLOR_AIR_TUNNEL;
    }

    private void InsertSplash(int intensity, int spread, int direction, int speed, int xPos, int permanence)
    {
        for (int newIdx = 0; newIdx < splashLimit; newIdx++)
        {
            if (splashIntensity[newIdx] == 0)
            {
                splashIntensity[newIdx] = intensity * 50 / 100;
                splashSpread[newIdx] = spread * 50 / 100;
                splashDirections[newIdx] = (sbyte)direction;
                splashPermanence[newIdx] = (sbyte)permanence;
                splashSpeeds[newIdx] = speed * 50 / 100;
                splashXPos[newIdx] = xPos;
                return;
            }
        }
    }

    public void OnBounceSurfaceContact(int xposWeight, float splashIntensity, int radius, BounceObject bounce)
    {
        if (IsWater())
        {
            bounceBubbleTimer = 0;
            splashIntensity = Math.Clamp(splashIntensity, -230.0f, 230.0f);
            int intensityAbs = (int)Math.Abs(splashIntensity);
            if (region == 0)
            {
                int length = (int)((splashYOffsets.Length - 1 << 12) * (long)xposWeight >> 16);
                int i3 = (intensityAbs * 100 << 12) / 1500;
                int i4 = i3 << 1;
                InsertSplash(i3, i4, -1, 20480, length - (radius << 2), 0);
                InsertSplash(i3, i4, 1, 20480, length + (radius << 2), 0);
            }
            bounce.TorqueX /= 3.0f;
            bounce.TorqueY /= 3.0f;
            if (region == Region.SURFACE)
            {
                LoadObjectMatrixToTarget(out TmpObjMatrix);
                int splashY = TmpObjMatrix.TranslationY + (area.MinY << 16);
                int splashRange = intensityAbs * 500 / 230;
                int splashParticleCount = intensityAbs * 6 / 230;
                int splashLifespan = intensityAbs * 800 / 230;
                BounceGame.WaterSplashParticle.EmitBurst(
                    splashParticleCount,
                    bounce.LocalObjectMatrix.TranslationX, // bugfix replace BounceGame.bounceObj with param bounce
                    splashY,
                    splashRange,
                    splashRange >> 2,
                    325,
                    30,
                    splashLifespan,
                    splashLifespan / 6
                );
                BounceGame.WaterSplashParticle.EmitBurst(
                    splashParticleCount,
                    bounce.LocalObjectMatrix.TranslationX, // bugfix replace BounceGame.bounceObj with param bounce
                    splashY,
                    splashRange,
                    splashRange >> 2,
                    35,
                    30,
                    splashLifespan,
                    splashLifespan / 6
                );
            }
        }
    }

    public void UpdateBounceSwim(int x, int y, BounceObject bounce)
    {
        bounceBubbleTimer += GameRuntime.UpdateDelta;
        if (!BounceGame.WaterSingletonFlag)
        {
            // force recalc only one water block per update
            float xWeight = (x - BBox.MinX) / (float)(BBox.MaxX - BBox.MinX);
            float yWeight = (y - BBox.MinY) / (float)(BBox.MaxY - BBox.MinY);
            int antiGravityX;
            int antiGravityY;

            // This lerp may not seem very smart at first glance, but it reduces the number of floating point multiplications, which is actually great
            if (gravityXLeft < gravityXRight)
                antiGravityX = (int)(xWeight * (gravityXRight - gravityXLeft)) + gravityXLeft;
            else
                antiGravityX = (int)((1.0f - xWeight) * (gravityXLeft - gravityXRight)) + gravityXRight;

            if (gravityYBottom < gravityYTop)
                antiGravityY = (int)(yWeight * (gravityYTop - gravityYBottom)) + gravityYBottom;
            else
                antiGravityY = (int)((1.0f - yWeight) * (gravityYBottom - gravityYTop)) + gravityYTop;
            bounce.GravityX += antiGravityX << 5;
            bounce.GravityY += antiGravityY << 5;

            if (bounce.TorqueX > 0.0f)
            {
                bounce.TorqueX -= GameRuntime.UpdateDelta * bounce.TorqueX / 400f;
                if (bounce.TorqueX < 0.0f)
                    bounce.TorqueX = 0.0f;
            }
            else if (bounce.TorqueX < 0.0f)
            {
                bounce.TorqueX -= GameRuntime.UpdateDelta * bounce.TorqueX / 400f;
                if (bounce.TorqueX > 0.0f)
                    bounce.TorqueX = 0.0f;
            }
            if (bounce.TorqueY > 0.0f)
            {
                bounce.TorqueY -= GameRuntime.UpdateDelta * bounce.TorqueY / 400f;
                if (bounce.TorqueY < 0.0f)
                    bounce.TorqueY = 0.0f;
            }
            else if (bounce.TorqueY < 0.0f)
            {
                bounce.TorqueY -= GameRuntime.UpdateDelta * bounce.TorqueY / 400f;
                if (bounce.TorqueY > 0.0f)
                    bounce.TorqueY = 0.0f;
            }

            if (IsWater())
            {
                float invGravity = 1.0f / BounceObject.GRAVITY[(int)bounce.BallForme];
                float slowdown = GameRuntime.UpdateDelta * 0.0014f;

                if (bounce.CurXVelocity > 0.0f)
                {
                    bounce.CurXVelocity -= bounce.CurXVelocity * invGravity * slowdown;
                    if (bounce.CurXVelocity < 0.0f)
                        bounce.CurXVelocity = 0.0f;
                }
                else
                {
                    bounce.CurXVelocity -= bounce.CurXVelocity * invGravity * slowdown;
                    if (bounce.CurXVelocity > 0.0f)
                        bounce.CurXVelocity = 0.0f;
                }

                if (bounce.CurYVelocity > 0.0f)
                {
                    bounce.CurYVelocity -= invGravity * bounce.CurYVelocity * slowdown;
                    if (bounce.CurYVelocity < 0.0f)
                        bounce.CurYVelocity = 0.0f;
                }
                else
                {
                    bounce.CurYVelocity -= invGravity * bounce.CurYVelocity * slowdown;
                    if (bounce.CurYVelocity > 0.0f)
                        bounce.CurYVelocity = 0.0f;
                }

                if (bounceBubbleTimer > 150)
                {
                    LoadObjectMatrixToTarget(out TmpObjMatrix);
                    BounceGame.BubbleParticle.BubblePopY = TmpObjMatrix.TranslationY + (area.MinY << 16);
                    BounceGame.BubbleParticle.MaxVelocityY = 60000;
                    BounceGame.BubbleParticle.EmitTrail(EventObject.EventVars[4] / 60, bounce.LocalObjectMatrix.TranslationX, bounce.LocalObjectMatrix.TranslationY, BounceObject.BALL_DIMENS[0] << 15, 0, 0, 0, 0, 4000, 666);
                    bounceBubbleTimer = 0;
                }
                bounce.ZCoord = 8;
            }
            BounceGame.WaterSingletonFlag = true;
        }
    }
}
