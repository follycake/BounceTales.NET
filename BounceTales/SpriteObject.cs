using BounceTales.Ext.Rsc;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class SpriteObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 5;

    // Parameters
    internal short[] imageIDs;

    private short[] xCoords;
    private short[] yCoords;

    // State
    internal short[] actionImageIDs;

    public override int ReadData(byte[] data, int dataPos)
    {
        dataPos = base.ReadData(data, dataPos);
        int count = data[dataPos++] & 255;
        xCoords = new short[count];
        yCoords = new short[count];
        imageIDs = new short[count];
        actionImageIDs = new short[count];
        if (count > 0)
        {
            int bitSize = data[dataPos++] & 255;
            short baseX = ReadShort(data, dataPos);
            dataPos += 2;
            short baseY = ReadShort(data, dataPos);
            dataPos += 2;
            if (bitSize > 0)
            {
                dataPos = DecomposeBytesToShorts(xCoords, count, baseX, data, dataPos, bitSize);
                dataPos = DecomposeBytesToShorts(yCoords, count, baseY, data, dataPos, bitSize);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    xCoords[i] = baseX;
                    yCoords[i] = baseY;
                }
            }
            dataPos = DecomposeBytesToShorts(imageIDs, count, 0, data, dataPos, 16);
            for (int i = 0; i < count; i++)
                actionImageIDs[i] = -1;
        }
        return dataPos;
    }

    public override void Initialize()
    {
        base.Initialize();
        for (int i = 0; i < imageIDs.Length; i++)
        {
            int width = (GameRuntime.GetImageMapParam(imageIDs[i], ImageMap.Param.WIDTH) << 16) / ScreenSpaceMatrix.M00;
            int height = (GameRuntime.GetImageMapParam(imageIDs[i], ImageMap.Param.HEIGHT) << 16) / ScreenSpaceMatrix.M00;
            int a3 = (GameRuntime.GetImageMapParam(imageIDs[i], ImageMap.Param.ORIGIN_X) << 16) / ScreenSpaceMatrix.M00;
            int a4 = (GameRuntime.GetImageMapParam(imageIDs[i], ImageMap.Param.ORIGIN_Y) << 16) / ScreenSpaceMatrix.M00;
            int x = xCoords[i] - a3;
            int endX = width + x;
            int y = yCoords[i] - (height - a4);
            int endY = height + y;
            if (x < BBox.MinX) // TODO: Add AABB.Encapsulate method
                BBox.MinX = x;
            if (endX > BBox.MaxX)
                BBox.MaxX = endX;
            if (y < BBox.MinY)
                BBox.MinY = y;
            if (endY > BBox.MaxY)
                BBox.MaxY = endY;
        }
        BBox.MinX <<= 16;
        BBox.MaxX <<= 16;
        BBox.MinY <<= 16;
        BBox.MaxY <<= 16;
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        base.Draw(graphics, rootMatrix);
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        Matrix.MultMatrices(rootMatrix, tmpObjMatrix, out Matrix temp);
        for (int componentIdx = 0; componentIdx < imageIDs.Length; componentIdx++)
        {
            Vector2I pos = temp.MulVector(xCoords[componentIdx] << 16, yCoords[componentIdx] << 16) >> 16;
            if (actionImageIDs[componentIdx] > -1)
            {
                int anmProgress = imageIDs[componentIdx]; // field repurposed
                int normalAnmTime = anmProgress != 9999 ? anmProgress : BounceGame.LevelTimer;
                int anmTime;
                Color32 fadeColor;
                if (anmProgress > 0 || actionImageIDs[componentIdx] != 474)
                {
                    anmTime = normalAnmTime;
                    fadeColor = Color32.Zero;
                }
                else
                {
                    anmTime = 1;
                    fadeColor = Color32.FromARGB(((uint)Math.Abs(anmProgress) * 255 / 1500 & 0xFF) << 24);
                }
                Vector2I posAnim = pos;
                Graphics orgGraphics = GameRuntime.GetGraphicsObj();
                int actYPos;
                if (fadeColor != Color32.Zero)
                {
                    BounceGame.SpriteOffscreenGraphics.SetColor(0x0000FF); // red - transparency key color
                    BounceGame.SpriteOffscreenGraphics.FillRect(0, 0, BounceGame.SpriteFB.Width, BounceGame.SpriteFB.Height);
                    int halfFBWidth = BounceGame.SpriteFB.Width >> 1;
                    int height = BounceGame.SpriteFB.Height;
                    GameRuntime.SetGraphics(BounceGame.SpriteOffscreenGraphics);
                    actYPos = height;
                    pos.X = halfFBWidth;
                }
                else
                    actYPos = pos.Y;
                int anmFrameCount = GameRuntime.GetImageAnimationFrameCount(actionImageIDs[componentIdx]);
                int anmFrame;
                if (anmProgress != 9999)
                {
                    int anmLength = actionImageIDs[componentIdx] switch
                    {
                        474 => 750,
                        480 or 485 => 9999,
                        _ => 0,
                    };
                    anmFrame = (anmLength - anmTime) / GetAnmMsPerFrame(actionImageIDs[componentIdx]) % anmFrameCount;
                }
                else
                    anmFrame = anmTime / GetAnmMsPerFrame(actionImageIDs[componentIdx]) % anmFrameCount;
                if (anmFrame > anmFrameCount - 1)
                    anmFrame = anmFrameCount - 1;
                GameRuntime.DrawAnimatedImageRes(pos.X, actYPos, actionImageIDs[componentIdx], anmFrame);
                if (fadeColor != Color32.Zero)
                {
                    GameRuntime.SetGraphics(orgGraphics);
                    BounceGame.SpriteFB.GetRGB(BounceGame.SpriteFBRGB);
                    for (int rgbIndex = 0; rgbIndex < BounceGame.SpriteFBRGB.Length; rgbIndex++)
                    {
                        if (BounceGame.SpriteFBRGB[rgbIndex] == Color32.Blue)
                            BounceGame.SpriteFBRGB[rgbIndex] = Color32.Zero;
                        else
                            BounceGame.SpriteFBRGB[rgbIndex] = Color32.Subtract(BounceGame.SpriteFBRGB[rgbIndex], fadeColor);
                    }
                    GameRuntime.GetGraphicsObj().DrawRGB(BounceGame.SpriteFBRGB, posAnim.X - (BounceGame.SpriteFB.Width >> 1), posAnim.Y - BounceGame.SpriteFB.Height, BounceGame.SpriteFB.Width, BounceGame.SpriteFB.Height);
                }
            }
            else
                GameRuntime.DrawImageRes(pos.X, pos.Y, imageIDs[componentIdx]);
        }
        DebugDraw(graphics, 0, rootMatrix);
    }

    public override void UpdatePhysics()
    {
        base.UpdatePhysics();
        for (int i = 0; i < imageIDs.Length; i++)
        {
            if (actionImageIDs[i] > -1)
            {
                if (imageIDs[i] != 9999)
                    imageIDs[i] = (short)(imageIDs[i] - GameRuntime.UpdateDelta);
                if (imageIDs[i] > 0)
                    continue;
                if (actionImageIDs[i] != 474 || imageIDs[i] < -1500)
                {
                    Despawn();
                    return;
                }
            }
        }
    }

    public override void OnPlayerContact() // since 2.0.25
    {
        for (int componentIdx = 0; componentIdx < imageIDs.Length; componentIdx++)
        {
            short anmImage = -1;
            short anmLength = 0; // 9999 = loop forever
            switch (imageIDs[componentIdx])
            {
                case 118: // propeller flower
                    anmImage = 485;
                    anmLength = 9999;
                    break;
                case 334: // bumpy cracks stone wall
                    anmImage = 474;
                    anmLength = 750;
                    break;
                case 342: // color machine ray
                    anmImage = 480;
                    anmLength = 9999;
                    break;
            }
            if (anmImage > -1)
            {
                actionImageIDs[componentIdx] = anmImage;
                imageIDs[componentIdx] = anmLength;
            }
        }
    }

    private static int GetAnmMsPerFrame(int i)
    {
        return i switch
        {
            474 or 480 or 485 => 150,
            _ => 0,
        };
    }
}
