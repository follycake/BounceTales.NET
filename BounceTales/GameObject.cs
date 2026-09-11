using System.Diagnostics;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public class GameObject
{
    public const byte TYPEID_DUMMY = 1;

    [Flags]
    public enum ObjectFlags
    {
        Z_COORD_MASK = 31,
        NOCOLLIDE = 32,
        NODRAW = 128,
        UNKNOWN = 256
    }

    // Global state - instance counter
    private static short gameObjInstanceCount;

    // Global state - camera
    public static Matrix CameraMatrix = Matrix.Identity;
    private static Matrix inverseCameraMatrix = Matrix.Identity;

    public static GameObject CameraTarget;
    public static int CameraBounceFactor;
    public static int CameraStabilizeSpeed;

    public static int CameraVelocityX;
    public static int CameraVelocityY;
    private static int cameraTimer;

    // Global state - matrices
    public static Matrix ScreenSpaceMatrix = Matrix.Identity;

    // Global state - render queue
    private static int renderObjCount;
    private static GameObject[] objectsToRender = new GameObject[60];

    // Global state - dummy root object
    protected static GameObject dummyParent = CreateDummy();

    // Parameters
    private readonly byte objType;

    public ObjectFlags Flags;
    public sbyte ZCoord;

    // Parameters - relations
    protected short objectId;
    private short parentIdx;
    private short previousIdx;

    private GameObject parentNode;
    private GameObject previousNode;
    private GameObject nextNode;
    private GameObject firstChildNode;

    // State - transforms
    public Matrix LocalObjectMatrix = Matrix.Identity;
    public bool ObjectMatrixIsDirty = true;

    private Matrix absoluteObjectMatrix = Matrix.Identity;
    public Matrix InvAbsoluteObjectMatrix = Matrix.Identity;

    public bool RenderMatrixIsDirty = true;
    public Matrix RenderCalcMatrix = Matrix.Identity;
    public Matrix InverseRenderCalcMatrix = Matrix.Identity;

    public bool GeometryTransformIsDirty = true;

    // State - bounding box
    private bool bboxIsDirty = true;

    public AABB BBox;
    public AABB AllBBox;

    public static GameObject CreateDummy()
    {
        return new GameObject(TYPEID_DUMMY);
    }

    protected GameObject(byte objType)
    {
        this.objType = objType;
        objectId = gameObjInstanceCount++;
    }

    public virtual void Initialize()
    {
        BBox.MinX = int.MaxValue;
        BBox.MinY = int.MaxValue;
        BBox.MaxX = int.MinValue;
        BBox.MaxY = int.MinValue;
    }

    public virtual int ReadData(byte[] data, int dataPos)
    {
        parentIdx = ReadShort(data, dataPos);
        dataPos += 2;
        previousIdx = ReadShort(data, dataPos);
        dataPos += 2;
        byte transformFlags = data[dataPos++];
        if ((transformFlags & 7) == 7)
        {
            LocalObjectMatrix.M00 = ReadInt(data, dataPos);
            dataPos += 4;
            LocalObjectMatrix.M01 = ReadInt(data, dataPos);
            dataPos += 4;
            LocalObjectMatrix.TranslationX = ReadInt(data, dataPos);
            dataPos += 4;
            LocalObjectMatrix.M10 = ReadInt(data, dataPos);
            dataPos += 4;
            LocalObjectMatrix.M11 = ReadInt(data, dataPos);
            dataPos += 4;
            LocalObjectMatrix.TranslationY = ReadInt(data, dataPos);
            dataPos += 4;
        }
        else
        {
            if ((transformFlags & 1) > 0)
            {
                LocalObjectMatrix.TranslationX = ReadShort(data, dataPos) << 16;
                dataPos += 2;
                LocalObjectMatrix.TranslationY = ReadShort(data, dataPos) << 16;
                dataPos += 2;
            }
            if ((transformFlags & 2) > 0)
            {
                LocalObjectMatrix.SetRotation(LP32.LP32ToFP32(ReadInt(data, dataPos)));
                dataPos += 4;
            }
            if ((transformFlags & 4) > 0)
            {
                LocalObjectMatrix.SetScale(ReadInt(data, dataPos), ReadInt(data, dataPos + 4));
                dataPos += 8;
            }
        }
        Flags = (ObjectFlags)ReadInt(data, dataPos);
        dataPos += 4;
        ZCoord = (sbyte)((Flags & ObjectFlags.Z_COORD_MASK) - 16);
        RenderCalcMatrix = LocalObjectMatrix;
        return dataPos;
    }

    public static int DecomposeBytesToInts(int[] target, int count, int baseVal, byte[] src, int srcPos, int bitsPerInt)
    {
        int bitBuffer = 0;
        int bufIdx = 0;
        int index = 0;
        int bit = 1 << bitsPerInt - 1;
        int mask = (1 << bitsPerInt) - 1;
        while (index < count)
        {
            int oldBitsWithNewByte = bitBuffer | src[srcPos++] << bufIdx;
            bufIdx += 8;
            bitBuffer = oldBitsWithNewByte & (1 << bufIdx) - 1;
            while (bitsPerInt <= bufIdx)
            {
                int maskedBits = bitBuffer & mask;
                if ((maskedBits & bit) > 0)
                    maskedBits |= ~mask; // sign extend
                if (index < count)
                    target[index++] = maskedBits + baseVal << 16;
                bufIdx -= bitsPerInt;
                bitBuffer >>>= bitsPerInt;
            }
        }
        return srcPos;
    }

    public static int DecomposeBytesToShorts(short[] target, int count, int baseVal, byte[] src, int srcPos, int bitsPerShort)
    {
        int bitBuffer = 0;
        int bufIdx = 0;
        int index = 0;
        int bit = 1 << bitsPerShort - 1;
        int mask = (1 << bitsPerShort) - 1;
        while (index < count)
        {
            bitBuffer |= src[srcPos++] << bufIdx;
            bufIdx += 8;
            bitBuffer &= (1 << bufIdx) - 1;
            while (bitsPerShort <= bufIdx)
            {
                int result = bitBuffer & mask;
                if ((result & bit) > 0)
                    result |= ~mask; // sign extend
                if (index < count)
                    target[index++] = (short)(result + baseVal);
                bufIdx -= bitsPerShort;
                bitBuffer >>>= bitsPerShort;
            }
        }
        return srcPos;
    }

    public static short ReadShort(byte[] bArr, int offset)
    {
        return (short)(bArr[offset] << 8 | bArr[offset + 1] & 255);
    }

    public static int ReadInt(byte[] bArr, int offset)
    {
        return (bArr[offset] & 255) << 24 | (bArr[offset + 1] & 255) << 16 | (bArr[offset + 2] & 255) << 8 | bArr[offset + 3] & 255;
    }

    public virtual void UpdatePhysics()
    {
        if (RenderMatrixIsDirty)
        {
            if (parentNode != null)
                Matrix.MultMatrices(parentNode.RenderCalcMatrix, LocalObjectMatrix, out RenderCalcMatrix);
            else
                RenderCalcMatrix = LocalObjectMatrix;
            RenderCalcMatrix.Invert(out InverseRenderCalcMatrix);
            RenderMatrixIsDirty = false;
        }
    }

    public virtual void CheckCollisions(GameObject jVar)
    {
    }

    public virtual void OnPlayerContact()
    {
    }

    public virtual void Draw(Graphics graphics, Matrix rootMatrix)
    {
    }

    public short GetObjectId()
    {
        return objectId;
    }

    public void SetObjectId(short s)
    {
        objectId = s;
    }

    public byte GetObjType()
    {
        return objType;
    }

    internal static void AllocateRenderPool(int levelObjCount)
    {
        // high resolution 2D rendering needs this
        objectsToRender = new GameObject[levelObjCount];
        renderObjCount = 0;
    }

    public static void SetScreenSpaceMatrixByWindow(int winW, int winH)
    {
        ScreenSpaceMatrix.M00 = 43266;
        ScreenSpaceMatrix.M11 = -43266;
        if (winW < 200)
        {
            ScreenSpaceMatrix.M00 = 22306;
            ScreenSpaceMatrix.M11 = -22306;
        }
        ScreenSpaceMatrix.TranslationX = winW << 16 >> 1;
        ScreenSpaceMatrix.TranslationY = winH << 16 >> 1;
    }

    private static void DepthSort(GameObject[] objects, int start, int end)
    {
        while (start < end)
        {
            GameObject endObj = objects[end];
            int swapIdx = start - 1;
            for (int searchIdx = start; searchIdx < end; searchIdx++)
            {
                if (endObj.ZCoord < objects[searchIdx].ZCoord)
                {
                    swapIdx++;
                    (objects[searchIdx], objects[swapIdx]) = (objects[swapIdx], objects[searchIdx]);
                }
            }
            objects[end] = objects[swapIdx + 1];
            objects[swapIdx + 1] = endObj;
            int mid = swapIdx + 1;
            DepthSort(objects, start, mid - 1);
            start = mid + 1; // updated in 2.0.25
        }
    }

    protected static AABB screenAABB;

    public static void DrawSceneTree(GameObject root, Graphics g)
    {
        GetWorldMatrix(out Matrix rootMatrix);
        rootMatrix.Invert(out Matrix inverseRootMatrix);
        GetScreenWorldAABB(inverseRootMatrix, out screenAABB);
        renderObjCount = 0;
        GameObject currentObj = root;
        while (currentObj != null)
        {
            if (currentObj.objType != ParticleObject.TYPEID && !currentObj.IsInAABB(screenAABB))
                currentObj = currentObj.GetNextNode(root);
            else if ((currentObj.Flags & ObjectFlags.NODRAW) != 0) // nondraw
                currentObj = currentObj.GetNextNodeDescendToChildren(root);
            else if (renderObjCount < objectsToRender.Length)
            {
                objectsToRender[renderObjCount] = currentObj;
                renderObjCount++;
                currentObj = currentObj.GetNextNodeDescendToChildren(root);
            }
            else
            {
                Trace.WriteLine("Rendering engine queue too small!");
                break;
            }
        }
        GameObject[] renderObjs = objectsToRender;
        if (renderObjs != null && renderObjCount > 1)
        {
            if (renderObjCount != 2)
                DepthSort(renderObjs, 0, renderObjCount - 1);
            else if (renderObjs[0].ZCoord < renderObjs[1].ZCoord)
                (renderObjs[1], renderObjs[0]) = (renderObjs[0], renderObjs[1]);
        }
        for (int i = 0; i < renderObjCount; i++)
            objectsToRender[i].Draw(g, rootMatrix);
        renderObjCount = 0;
        for (int i = 0; i < objectsToRender.Length; i++)
            objectsToRender[i] = null;
    }

    public static void UpdateCamera(bool instant)
    {
        int i;
        int i2;
        int delta = GameRuntime.UpdateDelta;
        CameraTarget.LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        if (delta != 0)
        {
            i2 = (tmpObjMatrix.TranslationX - CameraTarget.RenderCalcMatrix.TranslationX) / delta;
            i = (tmpObjMatrix.TranslationY - CameraTarget.RenderCalcMatrix.TranslationY) / delta;
        }
        else
        {
            i = 0;
            i2 = 0;
        }

        int i5 = i2 << 7;
        int i6 = -i << 7;
        int i7 = tmpObjMatrix.TranslationX;
        int i8 = tmpObjMatrix.TranslationY;

        ScreenSpaceMatrix.Invert(out Matrix temp);
        Vector2I vectorMulRsl = temp.MulDirection(0, -GameRuntime.CurrentHeight / 5 << 16);
        int i9 = vectorMulRsl.X;
        int i10 = vectorMulRsl.Y;

        vectorMulRsl = temp.MulDirection(i5, i6);
        int cameraTx = vectorMulRsl.X + i5 + i7 + i9;
        int cameraTy = vectorMulRsl.Y + i6 + i8 + i10;
        if (instant)
        {
            CameraMatrix.TranslationX = cameraTx;
            CameraMatrix.TranslationY = cameraTy;
        }
        else
        {
            int diffX = cameraTx - CameraMatrix.TranslationX;
            int diffY = cameraTy - CameraMatrix.TranslationY;
            if (Math.Abs(diffX) < 327680)
                diffX = 0;
            if (Math.Abs(diffY) < 327680)
                diffY = 0;
            cameraTimer += delta;
            CameraMatrix.TranslationX += CameraVelocityX * delta; // Not accurate but reduces stuttering.
            CameraMatrix.TranslationY += CameraVelocityY * delta;
            while (cameraTimer >= 15)
            {
                //CameraMatrix.TranslationX += CameraVelocityX * 15;
                //CameraMatrix.TranslationY += CameraVelocityY * 15;
                CameraVelocityX += CameraBounceFactor * 15 * (diffX >> 6) >> 14;
                CameraVelocityY += CameraBounceFactor * 15 * (diffY >> 6) >> 14;
                CameraVelocityX -= CameraStabilizeSpeed * 15 * CameraVelocityX >> 14;
                CameraVelocityY -= CameraStabilizeSpeed * 15 * CameraVelocityY >> 14;
                cameraTimer -= 15;
            }
        }
    }

    public static void SnapCameraToTarget()
    {
        CameraVelocityX = 0;
        CameraVelocityY = 0;
        UpdateCamera(true);
    }

    public bool IsInAABB(AABB aabb)
    {
        GetBoundsAbs(out AABB tempBounds);
        return AABB.Intersects(tempBounds, aabb);
    }

    public static void GetWorldMatrix(out Matrix dest)
    {
        CameraMatrix.Invert(out inverseCameraMatrix);
        Matrix.MultMatrices(ScreenSpaceMatrix, inverseCameraMatrix, out dest);
        dest.TranslationX >>= 16;
        dest.TranslationX <<= 16;
        dest.TranslationY >>= 16;
        dest.TranslationY <<= 16;
    }

    public static void GetScreenWorldAABB(Matrix invWorldMatrix, out AABB dest)
    {
        Vector2I screenMin;
        Vector2I screenBoundMin = invWorldMatrix.MulVector(0, 0);
        Vector2I screenMax = invWorldMatrix.MulVector(GameRuntime.CurrentWidth << 16, GameRuntime.CurrentHeight << 16);
        if (screenBoundMin.X > screenMax.X)
        {
            screenMin.X = screenMax.X;
            screenMax.X = screenBoundMin.X;
        }
        else
            screenMin.X = screenBoundMin.X;
        if (screenBoundMin.Y > screenMax.Y)
        {
            screenMin.Y = screenMax.Y;
            screenMax.Y = screenBoundMin.Y;
        }
        else
            screenMin.Y = screenBoundMin.Y;
        dest = new AABB(screenMin, screenMax);
    }

    // TODO: This code repeats a bunch. Make a Matrix.MulAABB() or AABB.Transform() method for this.
    public static void MakeBoundsAbsolute(ref AABB dest, Matrix matrix)
    {
        Vector2I objMin = matrix.MulVector(dest.Min);
        Vector2I objMax = objMin;

        Vector2I temp = matrix.MulVector(dest.MinX, dest.MaxY);
        objMin = Vector2I.Min(objMin, temp);
        objMax = Vector2I.Max(objMax, temp);

        temp = matrix.MulVector(dest.Max);
        objMin = Vector2I.Min(objMin, temp);
        objMax = Vector2I.Max(objMax, temp);

        temp = matrix.MulVector(dest.MaxX, dest.MinY);
        objMin = Vector2I.Min(objMin, temp);
        objMax = Vector2I.Max(objMax, temp);
        dest = new AABB(objMin, objMax);
    }

    public void GetLocalBoundsAbs(out AABB dest, Matrix matrix)
    {
        dest = BBox;
        MakeBoundsAbsolute(ref dest, matrix);
    }

    public void GetBoundsAbs(out AABB dest)
    {
        dest = AllBBox;
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        MakeBoundsAbsolute(ref dest, tmpObjMatrix);
    }

    protected AABB Get2DBoundsAbs(Matrix rootMatrix)
    {
        LoadObjectMatrixToTarget(out Matrix tmpObjMatrix);
        Matrix.MultMatrices(rootMatrix, tmpObjMatrix, out Matrix temp);
        GetLocalBoundsAbs(out AABB tempBounds, temp);
        tempBounds.MinX >>= 16;
        tempBounds.MinY >>= 16;
        tempBounds.MaxX >>= 16;
        tempBounds.MaxY >>= 16;
        return tempBounds;
    }

    public void SetIsDirtyRecursive()
    {
        GameObject obj = this;
        while (obj != null)
        {
            obj.RenderMatrixIsDirty = true;
            obj.ObjectMatrixIsDirty = true;
            obj.GeometryTransformIsDirty = true;
            obj = obj.GetNextNodeDescendToChildren(this);
        }
    }

    public void RecalcAbsObjectMatrix()
    {
        absoluteObjectMatrix = LocalObjectMatrix;
        for (GameObject parent = parentNode; parent != null; parent = parent.parentNode)
            Matrix.MultMatrices(parent.LocalObjectMatrix, absoluteObjectMatrix, out absoluteObjectMatrix);
        absoluteObjectMatrix.Invert(out InvAbsoluteObjectMatrix);
        ObjectMatrixIsDirty = false;
    }

    public void LoadObjectMatrixToTarget(out Matrix dest)
    {
        if (ObjectMatrixIsDirty)
            RecalcAbsObjectMatrix();
        dest = absoluteObjectMatrix;
    }

    public static void MakeObjectLinks(GameObject[] objects)
    {
        foreach (GameObject obj in objects)
        {
            if (obj.parentIdx > -1)
            {
                GameObject jVar2 = objects[obj.parentIdx];
                obj.parentNode = jVar2;
                jVar2.firstChildNode ??= obj;
            }
            if (obj.previousIdx > -1)
            {
                GameObject prevObj = objects[obj.previousIdx];
                obj.previousNode = prevObj;
                prevObj.nextNode = obj;
            }
        }
        foreach (GameObject obj in objects)
        {
            obj.RenderMatrixIsDirty = true;
            obj.bboxIsDirty = true;
            obj.ObjectMatrixIsDirty = true;
            obj.RecalcAbsObjectMatrix();
            obj.LoadObjectMatrixToTarget(out obj.RenderCalcMatrix);
            obj.RenderCalcMatrix.Invert(out obj.InverseRenderCalcMatrix);
        }
    }

    public GameObject GetObjectRoot()
    {
        GameObject rsl = this;
        while (rsl.parentNode != null)
            rsl = rsl.parentNode;
        return rsl;
    }

    public GameObject GetNextNodeDescendToChildren(GameObject root)
    {
        if (root == null)
            return null;
        if (firstChildNode != null)
            return firstChildNode;
        if (root == this)
            return null;
        if (nextNode != null)
            return nextNode;
        GameObject parent = parentNode;
        while (parent != null && parent != root)
        {
            if (parent.nextNode != null)
                return parent.nextNode;
            parent = parent.parentNode;
        }
        return null;
    }

    public GameObject SearchByObjId(short searchId)
    {
        GameObject result = this;
        while (result != null && result.objectId != searchId)
            result = result.GetNextNodeDescendToChildren(this);
        return result;
    }

    public bool IsChildOf(GameObject other)
    {
        for (GameObject parent = parentNode; parent != null; parent = parent.parentNode)
        {
            if (other == parent)
                return true;
        }
        return false;
    }

    public GameObject GetNextNode(GameObject root)
    {
        if (root == null || this == root)
            return null;
        if (nextNode != null)
            return nextNode;
        GameObject parent = parentNode;
        while (parent != null && parent != root)
        {
            if (parent.nextNode != null)
                return parent.nextNode;
            parent = parent.parentNode;
        }
        return null;
    }

    public void MakeIndependent()
    {
        GameObject objRoot = GetObjectRoot();
        if (parentNode != null && parentNode != objRoot)
        {
            LoadObjectMatrixToTarget(out LocalObjectMatrix);
            SetParent(objRoot);
        }
    }

    public void SetParent(GameObject parent)
    {
        if (parent.IsChildOf(this))
            throw new ArgumentException("Can't set parent.", nameof(parent));
        if (parent == this)
            throw new ArgumentException("Can't set parent to self.", nameof(parent));
        Despawn();
        nextNode = parent.firstChildNode;
        nextNode?.previousNode = this;
        parentNode = parent;
        parentNode.firstChildNode = this;
    }

    public void Despawn()
    {
        if (parentNode != null)
        {
            if (parentNode.firstChildNode == this)
            {
                parentNode.firstChildNode = nextNode;
                nextNode?.previousNode = null;
            }
            else
            {
                previousNode?.nextNode = nextNode;
                nextNode?.previousNode = previousNode;
            }
            parentNode = null;
            nextNode = null;
            previousNode = null;
        }
    }

    public void SetBBoxIsDirty()
    {
        GameObject obj = this;
        while (obj != null)
        {
            obj.bboxIsDirty = true;
            obj = obj.parentNode;
        }
    }

    public void UpdateBBox()
    {
        if (bboxIsDirty)
        {
            AllBBox = BBox;
            for (GameObject obj = firstChildNode; obj != null; obj = obj.nextNode)
            {
                obj.UpdateBBox();
                Vector2I temp = obj.LocalObjectMatrix.MulVector(obj.AllBBox.Min);
                AllBBox.Min = Vector2I.Min(AllBBox.Min, temp);
                AllBBox.Max = Vector2I.Max(AllBBox.Max, temp);

                temp = obj.LocalObjectMatrix.MulVector(obj.AllBBox.MinX, obj.AllBBox.MaxY);
                AllBBox.Min = Vector2I.Min(AllBBox.Min, temp);
                AllBBox.Max = Vector2I.Max(AllBBox.Max, temp);

                temp = obj.LocalObjectMatrix.MulVector(obj.AllBBox.Max);
                AllBBox.Min = Vector2I.Min(AllBBox.Min, temp);
                AllBBox.Max = Vector2I.Max(AllBBox.Max, temp);

                temp = obj.LocalObjectMatrix.MulVector(obj.AllBBox.MaxX, obj.AllBBox.MinY);
                AllBBox.Min = Vector2I.Min(AllBBox.Min, temp);
                AllBBox.Max = Vector2I.Max(AllBBox.Max, temp);
            }
            bboxIsDirty = false;
        }
    }

    protected void DrawBBox(Graphics graphics, Matrix rootMatrix)
    {
        AABB bounds = Get2DBoundsAbs(rootMatrix);
        graphics.DrawRect(bounds.MinX, bounds.MinY, bounds.Width, bounds.Height);
    }

    [Conditional("DEBUG_DRAW_ON")]
    protected void DebugDraw(Graphics graphics, int color, Matrix rootMatrix)
    {
        graphics.SetColor(color);
        GameRuntime.SetTextStyle(-3, 1);
        DrawBBox(graphics, rootMatrix);
        AABB bounds = Get2DBoundsAbs(rootMatrix);
        GameRuntime.DrawText(ToString(), 0, ToString().Length, bounds.MinX + 2, bounds.MinY + 2, (int)(Graphics.Anchor.TOP | Graphics.Anchor.LEFT));
    }

    public override string ToString()
    {
        return GetType().Name + "|ID:" + GetObjectId();
    }
}
