using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class GeometryObject() : GameObject(TYPEID)
{
    public const byte TYPEID = 2;

    // Global temp variables
    //public static int[] TEMP_QUAD_XS; // We don't need these anymore.
    //public static int[] TEMP_QUAD_YS;

    // Parameters
    public short Event;

    // Vertex data
    private int rgbColor;

    private short[] indexBuffer;
    public int[] XCoordBuffer;
    public int[] YCoordBuffer;

    // Render state
    private Vector2I[] bufTransformed;

    public int GetVertexCount()
    {
        return XCoordBuffer.Length;
    }

    public override int ReadData(byte[] data, int dataPos)
    {
        dataPos = base.ReadData(data, dataPos);
        int vertexCount = ReadShort(data, dataPos) + 1;
        int facepointCount = ReadShort(data, dataPos + 2);
        rgbColor = ReadInt(data, dataPos + 4);
        XCoordBuffer = new int[vertexCount];
        YCoordBuffer = new int[vertexCount];
        bufTransformed = new Vector2I[vertexCount];
        indexBuffer = new short[facepointCount];
        byte dataBitSize = data[dataPos + 8];
        short vertexXBase = ReadShort(data, dataPos + 9);
        dataPos = DecomposeBytesToInts(XCoordBuffer, vertexCount - 1, vertexXBase, data, dataPos + 11, dataBitSize);
        short vertexYBase = ReadShort(data, dataPos);
        dataPos = DecomposeBytesToInts(YCoordBuffer, vertexCount - 1, vertexYBase, data, dataPos + 2, dataBitSize);
        dataPos = DecomposeBytesToShorts(indexBuffer, facepointCount, 0, 1, data, dataPos + 1, data[dataPos]);
        XCoordBuffer[vertexCount - 1] = XCoordBuffer[0];
        YCoordBuffer[vertexCount - 1] = YCoordBuffer[0];

        Event = ReadShort(data, dataPos);
        dataPos += 2;
        Initialize();
        return dataPos;
    }

    public override void Initialize()
    {
        base.Initialize();
        for (int i = 0; i < XCoordBuffer.Length; i++)
        {
            if (XCoordBuffer[i] < BBox.MinX)
                BBox.MinX = XCoordBuffer[i];
            if (XCoordBuffer[i] > BBox.MaxX)
                BBox.MaxX = XCoordBuffer[i];
        }
        for (int i = 0; i < YCoordBuffer.Length; i++)
        {
            if (YCoordBuffer[i] < BBox.MinY)
                BBox.MinY = YCoordBuffer[i];
            if (YCoordBuffer[i] > BBox.MaxY)
                BBox.MaxY = YCoordBuffer[i];
        }
    }

    public override void Draw(Graphics graphics, Matrix rootMatrix)
    {
        Vector2I t;
        if (GeometryTransformIsDirty)
        {
            LoadObjectMatrixToTarget(out TmpObjMatrix);
            t = rootMatrix.Translation;
            rootMatrix.Translation = Vector2I.Zero;
            Matrix.MultMatrices(rootMatrix, TmpObjMatrix, out Matrix.Temp);
            rootMatrix.Translation = t;
            for (int i = 0; i < XCoordBuffer.Length; i++)
                bufTransformed[i] = Matrix.Temp.MulVector(XCoordBuffer[i], YCoordBuffer[i]) >> 16;
            GeometryTransformIsDirty = false;
        }

        // Values used for culling if offscreen
        int maxX = GameRuntime.CurrentWidth - 1;
        int maxY = GameRuntime.CurrentHeight - 1;

        t = rootMatrix.Translation >> 16;
        graphics.SetColor(BounceGame.GetStolenColorIfApplicable(rgbColor));
        for (int faceIdx = 0; faceIdx < indexBuffer.Length; faceIdx += 3)
        {
            short v1 = indexBuffer[faceIdx];
            short v2 = indexBuffer[faceIdx + 1];
            short v3 = indexBuffer[faceIdx + 2];
            Vector2I p1 = bufTransformed[v1] + t;
            Vector2I p2 = bufTransformed[v2] + t;
            Vector2I p3 = bufTransformed[v3] + t;
            if ((p1.X >= 0 || p2.X >= 0 || p3.X >= 0) && (p1.Y >= 0 || p2.Y >= 0 || p3.Y >= 0) && (p1.X <= maxX || p2.X <= maxX || p3.X <= maxX) && (p1.Y <= maxY || p2.Y <= maxY || p3.Y <= maxY))
                graphics.FillTriangle(p1, p2, p3);
        }
        DebugDraw(graphics, 0xFF0000, rootMatrix);
    }
}
