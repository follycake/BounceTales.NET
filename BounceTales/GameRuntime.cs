using System.Buffers.Binary;
using System.Diagnostics;
using BounceTales.Ext.Rsc;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class GameRuntime : Displayable, IResourceHandler
{
    public enum ScreenOrientation
    {
        UNKNOWN = 0,
        RPORTRAIT = 1,
        PORTRAIT = 2,
        RLANDSCAPE = 3,
        LANDSCAPE = 4
    }

    public enum Softkey
    {
        CENTER = 0,
        RIGHT = 1,
        LEFT = 2
    }

    [Flags]
    public enum KeyEventFlags
    {
        JMEKEYCODE = 8,
        PRESS = 1,
        RELEASE = 2
    }

    public enum ControlMode
    {
        RAW = 1,
        GAME = 2,
        STRINPUT = 3
    }

    public enum SystemEvent
    {
        START = 1,
        RESIZE = 2,
        PAUSE = 3
    }

    private const Sprite.Transform NO_TRANSFORM = (Sprite.Transform)(-1);
    private const int LOADING_WAIT_TIMEOUT = 500;

    private static readonly Font[] FONTS =
    [
        Font.GetFont(Font.Face.PROPORTIONAL, Font.Style.ITALIC, 0),
        Font.GetFont(Font.Face.PROPORTIONAL, Font.Style.BOLD, 0),
        Font.GetFont(Font.Face.PROPORTIONAL, Font.Style.PLAIN, 0)
    ];

    private static readonly char[] SPLITTABLE_CHARACTERS = ['\n', ' ', '-'];

    private static readonly char[][] KEY_TO_CHAR_MAP =
    [
        [' ', '0'], //0 key
        ['.', ',', '?', '!', '\'', '-', '(', ')', '@', '/', ':', '_', '1'], //1/voicemail key
        ['a', 'b', 'c', 'ä', 'å', '2'], //2/abc key
        ['d', 'e', 'f', '3'], //3/def key
        ['g', 'h', 'i', '4'], //4/ghi key
        ['j', 'k', 'l', '5'], //5/jkl key
        ['m', 'n', 'o', 'ö', '6'], //6/mno key
        ['p', 'q', 'r', 's', '7'], //7/pqrs key
        ['t', 'u', 'v', '8'], //8/tuv key
        ['w', 'x', 'y', 'z', '9'], //9/wxyz key
        [], //star key
        [] //pound key
    ];

    // ENGINE GLOBAL STATE
    public static RMIDlet MidLet;

    private static bool midletIsPaused;

    private static IResourceHandler[] resHandlers;
    private static GameRuntime mInstance;

    private static readonly object gameMutex = new();

    public static bool GameThreadStarted;
    private static bool reqSystemGamePause;
    private static bool gameIsLoading;
    private static bool reqClose;

    private static SystemEvent[] systemEventQueue;
    private static int systemEventQueueSize;

    // GAME UPDATE TIMING
    private static int maxUpdateDelta = 500;
    private static int updatesPerDraw = 1;

    public static int UpdateDelta;
    private static long currentTime;
    private static long lastUpdateTimestamp;

    // RESOURCE MAP
    private static ResourceInfo[] resourceInfo;
    private static string[] resourcePaths;
    private static ResourceBatch[] resourceBatchInfo;

    // RESOURCE LOADER
    private static readonly object loadingMutex = new();

    private static List<int> resLoadQueue;
    private static List<int> resUnloadQueue;

    private static bool[] isResourceLoaded;
    private static object[] loadedResources;

    private static short[][] residentResMap;

    private static int sceneLoadQueueSize;
    private static GameScene[] sceneLoaderQueue;

    // IMAGE RESOURCES
    private static Image[] imageResources;
    private static ImageMap[] imageMaps;
    private static ImageMapEx[] imageMaps2;
    //private static readonly short[] tempImageDrawParams = new short[6];

    // RESIDENT STRINGS (unused)
    private static int residentStringFieldCount;
    private static string[] residentStrings;

    // RENDERING
    public static int CurrentWidth = 240;
    public static int CurrentHeight = 320;

    private static Graphics mGraphics;
    private static Graphics lastGraphics;

    private static int paintMode;
    private static int textShadowType;
    private static int currentFont = -1;
    private static Sprite.Transform nextDrawTransform = NO_TRANSFORM;
    private static readonly int[] textColors = new int[2];

    private static readonly bool isGamePaintEnabled = true;

    private static string[] reqSoftkeyTexts;
    private static string[] softkeyTexts;
    private static int[] softkeyUITypes;

    // SOUND
    private static bool music_IsEnabled;
    private static int music_IdCurrent = -1;
    private static int music_IdQueuedAfterSysUnpause = -1;
    private static int music_IdBeforeSysPause = -1;
    private const int music_MasterVolume = 60;

    // INPUT / HID
    private static bool disableHID;
    private static ControlMode controlMode = ControlMode.RAW;

    // for control mode 1
    private static int buttonsDown;
    private static int buttonsHit;
    private static int buttonsHeld;
    private static int keyQueueSize;
    private static KeyCode[] keyQueue;

    // for control mode 3
    private static char curTypedChar;
    private static KeyCode typeSeqLastKey = 0;
    private static long typeSeqLastTime;
    private static int typeSeqKeyRepeatNo;
    private static bool typingKeyIsHeld;
    private static long typingKeyHoldStartTime;
    private static KeyCode typingKeyHeldId;

    // BOUNCE
    public static BounceGame BounceGame;

    public static long CurrentTimeMillis()
    {
        return MidLet.System.CurrentTimeMillis();
    }

    public static void ResetGlobalState()
    {
        typingKeyIsHeld = false;
        textShadowType = 0;
        currentFont = -1;
        nextDrawTransform = NO_TRANSFORM;
        residentStringFieldCount = 0;
        ResetHID();
        typeSeqLastKey = 0;
        typeSeqKeyRepeatNo = 0;
        GameThreadStarted = false;
        reqClose = false;
    }

    public static void PressKey(KeyCode key)
    {
        OnKeyEvent((int)key, KeyEventFlags.PRESS);
    }

    public static void ReleaseKey(KeyCode key)
    {
        OnKeyEvent((int)key, KeyEventFlags.RELEASE);
    }

    public static void Quit()
    {
        reqClose = true;
    }

    // TODO: Just make this thing public
    public static int GetUpdatesPerDraw()
    {
        return updatesPerDraw;
    }

    public static void SetUpdatesPerDraw(int value)
    {
        updatesPerDraw = value;
    }

    public static void SetMaxUpdateDelta(int i)
    {
        maxUpdateDelta = i;
    }

    public static Graphics GetGraphicsObj()
    {
        return mGraphics;
    }

    public static void SetGraphics(Graphics graphics)
    {
        graphics ??= lastGraphics;
        mGraphics = graphics;
    }

    public static bool SetChildClip(int x, int y, int width, int height, int[] oldClipDest)
    {
        int newWidth;
        int newHeight;
        int clipX = mGraphics.ClipX;
        int clipY = mGraphics.ClipY;
        int clipWidth = mGraphics.ClipWidth;
        int clipHeight = mGraphics.ClipHeight;
        oldClipDest[0] = clipX;
        oldClipDest[1] = clipY;
        oldClipDest[2] = clipWidth;
        oldClipDest[3] = clipHeight;
        if (x < clipX)
        {
            newWidth = width - (clipX - x);
            x = clipX;
        }
        else
            newWidth = width;
        if (y < clipY)
        {
            newHeight = height - (clipY - y);
            y = clipY;
        }
        else
            newHeight = height;
        if (x + newWidth > clipX + clipWidth)
            newWidth = clipX + clipWidth - x;
        if (y + newHeight > clipY + clipHeight)
            newHeight = clipY + clipHeight - y;
        if (newWidth <= 0 || newHeight <= 0)
            return false;
        mGraphics.SetClip(x, y, newWidth, newHeight);
        return true;
    }

    public static int GetCurrentFont()
    {
        return currentFont;
    }

    public static int GetFontHeight(int fontId)
    {
        int fontIndex;
        if (fontId == -1)
            fontIndex = 0;
        else if (fontId == -2)
            fontIndex = 1;
        else if (fontId != -3)
            return -1;
        else
            fontIndex = 2;
        return FONTS[fontIndex].GetHeight();
    }

    public static void SetTextStyle(int font, int shadowType)
    {
        currentFont = font;
        textShadowType = shadowType;
        textColors[0] = 0;
    }

    public static void SetTextColor(int colorType, int colorValue)
    {
        textColors[colorType] = colorValue;
    }

    public static int GetStrRenderWidth(int fontId, string str, int off, int len)
    {
        int c;
        if (fontId == -1)
            c = 0;
        else if (fontId == -2)
            c = 1;
        else if (fontId != -3)
            return -1;
        else
            c = 2;
        return FONTS[c].SubstringWidth(str, off, len);
    }

    public static string[] PrepareStringLines(string str, int maxWidth, int fontId, bool b, int n3, int n4, bool dynamicWidth)
    {
        string[] array = new string[5];
        int lineCount = 0;
        int n6 = 0;
        int max = n3;
        int n7 = 0;
        int length = str.Length;
        int strPos = 0;
        while (strPos < length)
        {
            bool b3 = b && GetFontHeight(fontId) * lineCount < n4;
            int n8;
            if (b3)
            {
                n8 = maxWidth - n3;
                ++n6;
            }
            else
                n8 = maxWidth;
            int n9 = 0;
            int n10 = strPos;
            int n11 = length;
            while (strPos < length)
            {
                int n12 = length;
                int j = strPos;
                while (j < length)
                {
                    int n13;
                    for (n13 = 0; n13 < SPLITTABLE_CHARACTERS.Length && str[j] != SPLITTABLE_CHARACTERS[n13]; ++n13)
                    {
                    }
                    if (n13 < SPLITTABLE_CHARACTERS.Length)
                    {
                        n12 = j;
                        if (str[n12] == '-')
                            ++n12;
                        break;
                    }
                    ++j;
                }
                if (GetStrRenderWidth(fontId, str, n10, n12 - n10) < n8)
                {
                    n11 = n12;
                    strPos = n12;
                    ++n9;
                    if (n11 < length && str[n11] == '\n')
                        break;
                    while (strPos < length)
                    {
                        int n14;
                        for (n14 = 0; n14 < SPLITTABLE_CHARACTERS.Length && str[strPos] != SPLITTABLE_CHARACTERS[n14]; ++n14)
                        {
                        }
                        if (n14 >= SPLITTABLE_CHARACTERS.Length || str[strPos] == '-' || str[strPos] == '\n')
                            break;
                        ++strPos;
                    }
                }
                else
                {
                    if (n9 == 0)
                    {
                        int n15;
                        for (n15 = n10; GetStrRenderWidth(fontId, str, n10, n15 - n10 + 1) < n8; ++n15)
                        {
                        }
                        n11 = n15;
                        strPos = n15;
                    }
                    break;
                }
            }
            int a = GetStrRenderWidth(fontId, str, n10, n11 - n10);
            if (b3)
            {
                max = Math.Max(max, a + n3);
                n7 = Math.Max(n7, a + n3);
            }
            else
                n7 = Math.Max(n7, a);
            string[] array2 = array;
            int n16 = lineCount + 1;
            string substring = str[n10..n11];
            int n17 = n16;
            string[] array3 = array2;
            if (array2.Length <= n17)
            {
                string[] array4 = new string[array3.Length + 5];
                array3.CopyTo(array4, 0);
                array3 = array4;
            }
            array3[n17] = substring;
            array = array3;
            ++lineCount;
            if (strPos < length && str[strPos] == '\n')
                ++strPos;
            Thread.Sleep(1);
        }
        int blockWidth = dynamicWidth ? n7 : maxWidth;
        int blockHeight;
        if (b)
            blockHeight = Math.Max(lineCount * GetFontHeight(fontId), n4);
        else
            blockHeight = lineCount * GetFontHeight(fontId) + n4;
        array[0] = lineCount + " " + n6 + " " + max + " " + blockWidth + " " + blockHeight;
        return array;
    }

    public static void DrawText(string str, int offset, int length, int xpos, int ypos, int flags)
    {
        // Why doesn't this use the offset variable?
        int fontIndex;
        Graphics.Anchor anchor;
        switch (currentFont)
        {
            case -1:
                fontIndex = 0;
                break;
            case -2:
                fontIndex = 1;
                break;
            case -3:
                fontIndex = 2;
                break;
            default:
                return;
        }
        if ((flags & 2) == 2)
        {
            anchor = (Graphics.Anchor)(flags & -3 | 16);
            ypos -= GetFontHeight(currentFont) / 2;
        }
        else
            anchor = (Graphics.Anchor)flags;
        mGraphics.SetFont(FONTS[fontIndex]);
        if (textShadowType != 1)
        {
            mGraphics.SetColor(textColors[1]);
            switch (textShadowType)
            {
                case 2:
                    mGraphics.DrawSubstring(str, 0, length, xpos + 1, ypos + 1, anchor);
                    break;
                case 3:
                    int i7 = 0;
                    while (i7 < 4)
                    {
                        int i8 = i7 + 1;
                        mGraphics.DrawSubstring(str, 0, length, xpos + (i7 & 1) * (i7 - 2), ypos + (i8 & 1) * (i8 - 2), anchor);
                        i7 = i8;
                    }
                    break;
            }
        }
        mGraphics.SetColor(textColors[0]);
        mGraphics.DrawSubstring(str, 0, length, xpos, ypos, anchor);
    }

    private static int GetImgResIdx(int imgMapIdx)
    {
        int baseOffset;
        int mapType;
        byte[] bArr;
        bool is16bit;
        short imageId;
        if (imgMapIdx == -1 || imgMapIdx < -1)
            return -1;
        if (imgMapIdx >= imageMaps.Length)
        {
            int map2Index = imgMapIdx - imageMaps.Length;
            if (map2Index >= imageMaps2.Length)
                return -1;
            short s2 = imageMaps2[map2Index].Offset;
            short s3 = imageMaps2[map2Index].ResBatchId;
            if (s3 < 0)
                return -1;
            byte[] bArr2 = (byte[])loadedResources[s3];
            if (s2 == -1 || bArr2 == null)
                return -1;
            baseOffset = s2 + 1;
            byte b = bArr2[s2];
            mapType = b & 3;
            bArr = bArr2;
            is16bit = (b & 4) != 0;
        }
        else if (imageMaps[imgMapIdx].ImageId == -1)
            return -1;
        else
        {
            bArr = null;
            baseOffset = 0;
            is16bit = false;
            mapType = -99;
        }
        if (mapType != 0 && mapType != -99)
            imageId = -1;
        else if (mapType == 0)
        {
            int i5 = is16bit ? baseOffset + 12 : baseOffset + 6;
            imageId = GetShortFromByteArray(bArr, i5);
        }
        else
            imageId = imageMaps[imgMapIdx].ImageId;
        return imageId;
    }

    public static Image GetImageResource(int i)
    {
        int d = GetImgResIdx(i);
        if (d >= 0)
            return imageResources[d];
        return null;
    }

    public static int GetImageMapParam(int mapId, ImageMap.Param paramId)
    {
        if (mapId < imageMaps.Length)
            return imageMaps[mapId].GetParam(paramId);
        ImageMapEx m2 = imageMaps2[mapId - imageMaps.Length];
        short offset = m2.Offset;
        byte[] b = (byte[])loadedResources[m2.ResBatchId];
        int dataOffs = offset + 1;
        if ((b[offset] & 4) != 0) // 16bit data
        {
            int offset16 = ((int)paramId << 1) + dataOffs;
            return GetShortFromByteArray(b, offset16);
        }
        return b[dataOffs + (int)paramId];
    }

    private static short GetShortFromByteArray(byte[] arr, int offset)
    {
        return (short)(arr[offset] << 8 | arr[offset + 1] & 255);
    }

    private static int GetIntFromByteArray(byte[] arr, int offset)
    {
        return (arr[offset] & 255) << 24 | (arr[offset + 1] & 255) << 16 | (arr[offset + 2] & 255) << 8 | arr[offset + 3] & 255;
    }

    public static void DrawAnimatedImageRes(int xpos, int ypos, int imageId, int anmFrame)
    {
        DrawImageRes(xpos, ypos, GetImageIdAfterAnimation(imageId, anmFrame));
    }

    public static int GetImageIdAfterAnimation(int imageId, int frame)
    {
        // for images of type 2
        ImageMapEx m2 = imageMaps2[imageId - imageMaps.Length];
        return GetShortFromByteArray((byte[])loadedResources[m2.ResBatchId],
            m2.Offset // base
            + 1 // header
            + 2 // frame count
            + frame * 2 // frame
        );
    }

    public static int GetCompoundSpriteParamEx(int imgMapId, int paramIndex)
    {
        ImageMapEx m2 = imageMaps2[imgMapId - imageMaps.Length];
        byte[] data = (byte[])loadedResources[m2.ResBatchId];
        byte flags = data[m2.Offset];
        int mapType = flags & 3;
        int paramQuantization = (flags & 4) != 0 ? 2 : 1;
        if (mapType != 1)
            return 0;
        int paramOffs = m2.Offset + 1 + paramQuantization * 4;
        int dataOffs = paramOffs + 2 // skip count field
                                 + GetShortFromByteArray(data, paramOffs) * paramQuantization * 3 // skip count * sizeof(params)
                                 + (paramQuantization << 1) * paramIndex;
        if (paramQuantization == 1)
            return data[dataOffs + 1] & 0xFFFF | data[dataOffs] << 16 & unchecked((int)0xFFFF0000);
        return GetIntFromByteArray(data, dataOffs);
    }

    public static int GetImageAnimationFrameCount(int imageId)
    {
        ImageMapEx m2 = imageMaps2[imageId - imageMaps.Length];
        return GetShortFromByteArray((byte[])loadedResources[m2.ResBatchId], m2.Offset + 1);
    }

    public static void DrawImageRes(int xpos, int ypos, int mapId)
    {
        if (mapId < 0)
        {
            ypos -= 20;
            mGraphics.SetColor(0xFFFFFF);
            mGraphics.FillRect(xpos, ypos, 20, 20);
            mGraphics.SetColor(0xFF00FF);
            mGraphics.FillRect(xpos, ypos, 10, 10);
            mGraphics.FillRect(xpos + 10, ypos + 10, 10, 10);
            return; // invalid resource
        }
        int paramSrcIdx;
        int mapType;
        int dataStride;
        byte[] paramSrc;
        Image image;
        int resIndex = 0;
        if (mapId < imageMaps.Length)
        {
            mapType = -99;
            paramSrc = null;
            paramSrcIdx = 0;
            dataStride = 0;
        }
        else
        {
            ImageMapEx m2 = imageMaps2[mapId - imageMaps.Length];
            short s = m2.Offset;
            byte[] bArr2 = (byte[])loadedResources[m2.ResBatchId];
            paramSrcIdx = s + 1;
            byte exHeader = bArr2[s];
            mapType = exHeader & 3;
            dataStride = (exHeader & 4) != 0 ? 2 : 1;
            paramSrc = bArr2;
        }
        if (mapType == 0 || mapType == -99)
        {
            Span<short> tempImageDrawParams = stackalloc short[6];
            if (mapType == 0)
            {
                if (dataStride == 2)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        tempImageDrawParams[i] = GetShortFromByteArray(paramSrc, paramSrcIdx);
                        paramSrcIdx += 2;
                    }
                }
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        tempImageDrawParams[i] = paramSrc[paramSrcIdx];
                        paramSrcIdx++;
                    }
                }
                image = imageResources[GetShortFromByteArray(paramSrc, paramSrcIdx)];
            }
            else
            {
                tempImageDrawParams[0] = (short)(imageMaps[mapId].Width & 0xFF);
                tempImageDrawParams[1] = (short)(imageMaps[mapId].Height & 0xFF);
                tempImageDrawParams[2] = imageMaps[mapId].OriginX;
                tempImageDrawParams[3] = imageMaps[mapId].OriginY;
                tempImageDrawParams[4] = (short)(imageMaps[mapId].AtlasX & 0xFF);
                tempImageDrawParams[5] = (short)(imageMaps[mapId].AtlasY & 0xFF);
                image = imageResources[imageMaps[mapId].ImageId];
            }
            int drawX = xpos - tempImageDrawParams[2];
            int drawY = ypos - tempImageDrawParams[3];
            short drawW = tempImageDrawParams[0];
            short drawH = tempImageDrawParams[1];
            if (drawW > 0 && drawH > 0)
            {
                if (nextDrawTransform != NO_TRANSFORM)
                {
                    mGraphics.DrawRegion(image, tempImageDrawParams[4], tempImageDrawParams[5], drawW, drawH, nextDrawTransform, drawX, drawY, Graphics.Anchor.TOP | Graphics.Anchor.LEFT);
                    nextDrawTransform = NO_TRANSFORM;
                }
                else
                    mGraphics.DrawRegion(image, tempImageDrawParams[4], tempImageDrawParams[5], drawW, drawH, Sprite.Transform.NONE, drawX, drawY, Graphics.Anchor.TOP | Graphics.Anchor.LEFT);
            }
        }
        else if (mapType == 1) // compound
        {
            int dataOffs = dataStride * 4 + paramSrcIdx;
            short resCount = GetShortFromByteArray(paramSrc, dataOffs);
            int streamPos = dataOffs + 2;
            if (dataStride == 2)
            {
                while (resIndex < resCount)
                {
                    DrawImageRes(GetShortFromByteArray(paramSrc, streamPos) + xpos,
                        GetShortFromByteArray(paramSrc, streamPos + 2) + ypos,
                        GetShortFromByteArray(paramSrc, streamPos + 4));
                    streamPos += 6;
                    resIndex++;
                }
            }
            else
            {
                while (resIndex < resCount)
                {
                    DrawImageRes(xpos + (sbyte)paramSrc[streamPos], ypos + (sbyte)paramSrc[streamPos + 1], paramSrc[streamPos + 2]);
                    streamPos += 3;
                    resIndex++;
                }
            }
        }
    }

    public static void DrawImageResAnchored(int x, int y, int mapId, Graphics.Anchor anchor)
    {
        int adjustX = x + GetImageMapParam(mapId, ImageMap.Param.ORIGIN_X);
        int adjustY = GetImageMapParam(mapId, ImageMap.Param.ORIGIN_Y) + y;
        if ((anchor & Graphics.Anchor.HCENTER) != 0)
            adjustX -= GetImageMapParam(mapId, ImageMap.Param.WIDTH) / 2;
        if ((anchor & Graphics.Anchor.RIGHT) != 0)
            adjustX -= GetImageMapParam(mapId, ImageMap.Param.WIDTH);
        if ((anchor & Graphics.Anchor.VCENTER) != 0)
            adjustY -= GetImageMapParam(mapId, ImageMap.Param.HEIGHT) / 2;
        if ((anchor & Graphics.Anchor.BOTTOM) != 0)
            adjustY -= GetImageMapParam(mapId, ImageMap.Param.HEIGHT);
        DrawImageRes(adjustX, adjustY, mapId);
    }

    public static void DrawImageResTransformed(int x, int y, int mapId, Graphics.Anchor anchor, Sprite.Transform transform)
    {
        nextDrawTransform = transform;
        DrawImageResAnchored(x, y, mapId, anchor);
        //DrawImageResAnchored(i, i2, 12, Graphics.Anchor.TOP | Graphics.Anchor.RIGHT);
    }

    public static void ReplaceImageResource(int imageId, Image image)
    {
        int d = GetImgResIdx(imageId);
        if (d >= 0)
            imageResources[d] = image;
    }

    public static void StartLoadScene(GameScene sceneId)
    {
        if (sceneLoadQueueSize == 40)
            throw new Exception();
        sceneLoaderQueue[sceneLoadQueueSize] = sceneId;
        sceneLoadQueueSize++;
    }

    public static void SetSoftkey(Softkey softkey, string text, int type)
    {
        reqSoftkeyTexts[(int)softkey] = text;
        softkeyUITypes[(int)softkey] = type;
    }

    public static void ResetSoftkeys()
    {
        for (int i = 0; i < 3; i++)
            SetSoftkey((Softkey)i, null, -1);
    }

    private static void GamePaint(Graphics graphics)
    {
        if (paintMode != 0 && graphics != null)
        {
            try
            {
                lastGraphics = graphics;
                mGraphics = graphics;
                graphics.SetClip(0, 0, CurrentWidth, CurrentHeight);
                if (BounceGame == null)
                {
                    // idle blank screen
                    graphics.SetColor(0);
                    graphics.FillRect(0, 0, CurrentWidth, CurrentHeight);
                }
                else if (isGamePaintEnabled)
                {
                    // game
                    for (int paintResult = BounceGame.Paint(0, paintMode); paintResult != 0; paintResult = BounceGame.Paint(paintResult, paintMode))
                    {
                    }
                }
                else
                    return;

                // draw softkey bar
                graphics.SetClip(0, 0, CurrentWidth, CurrentHeight);
                for (int i = 0; i < 3; i++)
                {
                    string str = softkeyTexts[i];
                    if (str != null)
                    {
                        Graphics.Anchor anchor = GetSoftkeyScreenAnchor((Softkey)i);
                        int i2 = (anchor & Graphics.Anchor.LEFT) != 0 ? 2 : 0;
                        if ((anchor & Graphics.Anchor.RIGHT) != 0)
                            i2 = CurrentWidth - 2;
                        int xpos = (anchor & Graphics.Anchor.HCENTER) != 0 ? CurrentWidth >> 1 : i2;
                        int ypos = (anchor & Graphics.Anchor.TOP) != 0 ? 2 : 0;
                        if ((anchor & Graphics.Anchor.BOTTOM) != 0)
                            ypos = CurrentHeight - 2;
                        if ((anchor & Graphics.Anchor.VCENTER) != 0)
                            ypos = CurrentHeight >> 1;
                        BounceGame.DrawSoftkeyUI(str, softkeyUITypes[i], xpos, ypos, (int)anchor);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }

    private static void CallGamePaint(int mode)
    {
        paintMode = mode;
        GamePaint(MidLet.Graphics.GetGraphics());
        MidLet.Graphics.FlushGraphics();
        MidLet.Audio?.Update();
        paintMode = 0;
    }

    public static void SetBacklight(bool isOn)
    {
        // TODO
    }

    public static bool IsScreenPortrait()
    {
        return GetScreenOrientationFromSoftkeys() == ScreenOrientation.RPORTRAIT || GetScreenOrientationFromSoftkeys() == ScreenOrientation.PORTRAIT;
    }

    public static bool IsScreenLandscape()
    {
        return GetScreenOrientationFromSoftkeys() == ScreenOrientation.RLANDSCAPE || GetScreenOrientationFromSoftkeys() == ScreenOrientation.LANDSCAPE;
    }

    public static int GetSoftkeyBarWidth()
    {
        return GetScreenOrientationFromSoftkeys() != ScreenOrientation.UNKNOWN ? -1 : 0;
    }

    public static int GetSoftkeyBarHeight()
    {
        if (GetScreenOrientationFromSoftkeys() != ScreenOrientation.UNKNOWN)
            return BounceGame.GetSoftkeyBarSize();
        return 0;
    }

    private static Graphics.Anchor GetSoftkeyScreenAnchor(Softkey softkey)
    {
        // portrait mode
        return softkey switch
        {
            Softkey.CENTER => Graphics.Anchor.BOTTOM | Graphics.Anchor.HCENTER,
            Softkey.RIGHT => Graphics.Anchor.BOTTOM | Graphics.Anchor.RIGHT,
            Softkey.LEFT => Graphics.Anchor.BOTTOM | Graphics.Anchor.LEFT,
            _ => (Graphics.Anchor)(-1)
        };
        // landscape mode
        /*return softkey switch
        {
            Softkey.CENTER => Graphics.LEFT | Graphics.VCENTER,
            Softkey.RIGHT => Graphics.LEFT | Graphics.BOTTOM,
            Softkey.LEFT => Graphics.LEFT | Graphics.TOP,
            _ => -1,
        };*/
    }

    public static ScreenOrientation GetScreenOrientationFromSoftkeys()
    {
        Graphics.Anchor VERTICAL_ANCHOR_MASK = Graphics.Anchor.BASELINE | Graphics.Anchor.TOP | Graphics.Anchor.BOTTOM | Graphics.Anchor.VCENTER;
        Graphics.Anchor HORIZONTAL_ANCHOR_MASK = Graphics.Anchor.LEFT | Graphics.Anchor.RIGHT | Graphics.Anchor.HCENTER;
        if ((GetSoftkeyScreenAnchor(Softkey.CENTER) & VERTICAL_ANCHOR_MASK) == (GetSoftkeyScreenAnchor(Softkey.RIGHT) & VERTICAL_ANCHOR_MASK))
            return (GetSoftkeyScreenAnchor(Softkey.CENTER) & Graphics.Anchor.BOTTOM) != 0 ? ScreenOrientation.PORTRAIT : ScreenOrientation.RPORTRAIT;
        if ((GetSoftkeyScreenAnchor(Softkey.CENTER) & HORIZONTAL_ANCHOR_MASK) == (GetSoftkeyScreenAnchor(Softkey.RIGHT) & HORIZONTAL_ANCHOR_MASK))
            return (GetSoftkeyScreenAnchor(Softkey.CENTER) & Graphics.Anchor.LEFT) != 0 ? ScreenOrientation.RLANDSCAPE : ScreenOrientation.LANDSCAPE;
        return ScreenOrientation.UNKNOWN;
    }

    public static void PlayMusic(int id, bool loop)
    {
        lock (gameMutex)
        {
            if (reqSystemGamePause)
            {
                if (loop)
                    music_IdQueuedAfterSysUnpause = id;
                else
                    music_IdQueuedAfterSysUnpause = -1;
                return;
            }
            music_IdQueuedAfterSysUnpause = -1;
            byte[] bytes = GetLoadedResData(id);
            if (bytes != null)
            {
                StopMusic();
                if (music_IsEnabled)
                {
                    try
                    {
                        if (loop)
                            music_IdCurrent = id;
                        MidLet.Audio?.SetVolumeLevel(music_MasterVolume);
                        MidLet.Audio?.Play(bytes, loop);
                    }
                    catch (IOException ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
        }
    }

    public static void StopMusic()
    {
        lock (gameMutex)
        {
            music_IdQueuedAfterSysUnpause = -1;
            if (music_IsEnabled)
            {
                try
                {
                    MidLet.Audio?.Stop();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                music_IdCurrent = -1;
            }
        }
    }

    public static bool IsMusicEnabled()
    {
        return music_IsEnabled;
    }

    public static void SetMusicEnabled(bool enabled)
    {
        music_IsEnabled = enabled;
    }

    public static void InitHID(ControlMode mode)
    {
        buttonsDown = 0;
        controlMode = mode;
    }

    public static void ResetHID()
    {
        buttonsDown = 0;
        buttonsHeld = 0;
        buttonsHit = 0;
        keyQueueSize = 0;
        disableHID = true;
    }

    private static void OnKeyEvent(int keyCode, KeyEventFlags flags)
    {
        KeyCode keyId;
        if (!disableHID && isGamePaintEnabled)
        {
            // We don't really need this anymore.
            /*if ((flags & KEYEVENT_FLAG_JMEKEYCODE) == KEYEVENT_FLAG_JMEKEYCODE)
            {
                try
                {
                    keyId = ConvertKeyCode(keyCode);
                }
                catch (Throwable th)
                {
                    return;
                }
            }
            else*/
            keyId = (KeyCode)keyCode;
            if (controlMode == ControlMode.GAME) // convert numbers to directional keys
            {
                keyId = keyId switch
                {
                    KeyCode.NUM5 => KeyCode.SOFTKEY_MIDDLE,
                    KeyCode.NUM2 => KeyCode.UP,
                    KeyCode.NUM8 => KeyCode.DOWN,
                    KeyCode.NUM4 => KeyCode.LEFT,
                    KeyCode.NUM6 => KeyCode.RIGHT,
                    _ => keyId
                };
            }
            if (controlMode != ControlMode.STRINPUT || (keyId < KeyCode.NUM0 || keyId > KeyCode.POUND) && keyId != KeyCode.POUND)
            {
                if ((flags & KeyEventFlags.PRESS) == KeyEventFlags.PRESS)
                {
                    if (keyQueueSize < 20)
                    {
                        keyQueue[keyQueueSize++] = keyId;
                    }
                    if (keyId != KeyCode.INVALID)
                    {
                        buttonsDown |= 1 << (int)keyId;
                        buttonsHit |= 1 << (int)keyId;
                    }
                }
                else if ((flags & KeyEventFlags.RELEASE) == KeyEventFlags.RELEASE && keyId != KeyCode.INVALID)
                    buttonsDown = ~(1 << (int)keyId) & buttonsDown;
            }
            else
            {
                if ((flags & KeyEventFlags.PRESS) != 0)
                {
                    if (keyId != KeyCode.POUND)
                    {
                        long currentTimeMillis = CurrentTimeMillis();
                        if (currentTimeMillis - typeSeqLastTime >= 700 || keyId != typeSeqLastKey)
                            typeSeqKeyRepeatNo = 0;
                        else
                            typeSeqKeyRepeatNo++;
                        int keyNumber = keyId - KeyCode.NUM0;
                        if (KEY_TO_CHAR_MAP[keyNumber].Length != 0 && keyNumber >= 0)
                        {
                            typeSeqLastTime = currentTimeMillis;
                            typeSeqLastKey = keyId;
                            char c = KEY_TO_CHAR_MAP[keyNumber][typeSeqKeyRepeatNo % KEY_TO_CHAR_MAP[keyNumber].Length];
                            if ((keyId != typingKeyHeldId || c != curTypedChar) && !typingKeyIsHeld)
                            {
                                typingKeyHeldId = keyId;
                                curTypedChar = c;
                                typingKeyHoldStartTime = CurrentTimeMillis();
                                typingKeyIsHeld = true;
                            }
                        }
                    }
                }
                else if ((flags & KeyEventFlags.RELEASE) != 0)
                {
                    if (keyId >= KeyCode.NUM0 && keyId <= KeyCode.NUM9 && CurrentTimeMillis() - typeSeqLastTime > 1200)
                    {
                        typeSeqKeyRepeatNo = 0;
                        typeSeqLastTime = 0;
                    }
                    EndTypingKeyHold(keyId);
                }
            }
        }
    }

    public static bool CheckButton(KeyCode buttonBit)
    {
        return (buttonsHeld & 1 << (int)buttonBit) != 0;
    }

    private static void EndTypingKeyHold(KeyCode keyCode)
    {
        if (keyCode == typingKeyHeldId && typingKeyIsHeld)
        {
            typingKeyIsHeld = false;
            typingKeyHeldId = (KeyCode)999;
            curTypedChar = ' ';
        }
    }

    private static bool IsGameLoading()
    {
        return resLoadQueue.Count > 0 || resUnloadQueue.Count > 0 || sceneLoadQueueSize > 0;
    }

    private static void ReadResourceTable()
    {
        if (resourcePaths == null)
        {
            ResidentResHeader[] resident;
            using (DataInputStream dis = new(MidLet.System.GetResourceAsStream(ResourceID.RESMAP_FILENAME)))
            {
                int rscCount = dis.ReadShort();
                resourcePaths = new string[rscCount];
                resourceInfo = new ResourceInfo[rscCount];
                for (int i = 0; i < rscCount; i++)
                {
                    resourcePaths[i] = dis.ReadUTF();
                    resourceInfo[i] = new ResourceInfo(dis);
                }

                int batchCount = dis.ReadShort();
                resourceBatchInfo = new ResourceBatch[batchCount];
                loadedResources = new object[batchCount];
                isResourceLoaded = new bool[batchCount];
                for (int batchIdx = 0; batchIdx < batchCount; batchIdx++)
                    resourceBatchInfo[batchIdx] = new ResourceBatch(dis);

                int residentCount = dis.ReadShort();
                resident = new ResidentResHeader[residentCount];
                for (int i = 0; i < residentCount; i++)
                    resident[i] = new ResidentResHeader(dis);
            }
            for (int i = 0; i < resident.Length; i++)
            {
                ResidentResHeader h = resident[i];
                using DataInputStream strm = GetStreamForRscId(h.ResId);
                if (strm != null)
                {
                    for (int gameRtIdx = 0; gameRtIdx < resHandlers.Length; gameRtIdx++)
                    {
                        if (resHandlers[gameRtIdx].LoadResidentData(strm, h.Type))
                            break;
                    }
                }
            }
            resHandlers[0].LoadResidentData(null, -1);
        }
    }

    private static void ForceSkipBytes(DataInputStream dis, int amount)
    {
        for (int bytesSkipped = 0; bytesSkipped < amount; bytesSkipped++)
            bytesSkipped = dis.SkipBytes(amount - bytesSkipped);
    }

    private static DataInputStream GetStreamForRscId(int rscId)
    {
        if (!resourceInfo[rscId].Exists())
            return null;
        DataInputStream dis = new(MidLet.System.GetResourceAsStream("/" + resourcePaths[rscId]));
        if (resourceInfo[rscId].SkipOffset == -1)
            return dis;
        ForceSkipBytes(dis, resourceInfo[rscId].SkipOffset);
        return dis;
    }

    public static byte[] GetLoadedResData(int i)
    {
        if (i >= 0 && loadedResources != null)
            return (byte[])loadedResources[i];
        return null;
    }

    public static void LoadResource(int resId)
    {
        if (resId >= 0)
        {
            int integer = resId;
            if (!resLoadQueue.Contains(integer))
                resLoadQueue.Add(integer);
            resUnloadQueue.Remove(integer);
        }
    }

    public bool LoadResource(DataInputStream dis, string finalRscPath, int readLength, ResourceType resType, int batchId, int subResIdx)
    {
        switch (resType)
        {
            case ResourceType.IMAGE:
                int imgResNo = GetShortFromByteArray(GetLoadedResData(batchId), 0) // table.baseImageID
                               + subResIdx;
                if (dis != null)
                {
                    byte[] bytes = ReadInputStreamToBytes(dis.Stream, readLength);
                    imageResources[imgResNo] = Image.CreateImage(bytes, 0, bytes.Length);
                }
                else
                    imageResources[imgResNo] = Image.CreateImage("/" + finalRscPath);
                return true;
            case ResourceType.MIDI:
            case ResourceType.LEVEL:
                if (dis != null)
                    PreloadResourceImpl(ReadInputStreamToBytes(dis.Stream, readLength), batchId, null);
                else
                    PreloadResourceImpl(null, batchId, "/" + finalRscPath);
                return true;
            default:
                return false;
        }
    }

    private static void ProcessResourceLoad()
    {
        int newCurStreamPos;
        string newLastRscPath;
        List<int[]> vector = [];
        for (int i = 0; i < resLoadQueue.Count; i++)
        {
            int batchId = resLoadQueue[i];
            if (!isResourceLoaded[batchId])
            {
                int firstAvailInsertIdx = 0;
                int subResIdx = -1;
                while (subResIdx < resourceBatchInfo[batchId].SubResIds.Length)
                {
                    int rscId = subResIdx == -1 ? resourceBatchInfo[batchId].MainResId : resourceBatchInfo[batchId].SubResIds[subResIdx];
                    //System.out.println("req load " + batchId + " / " + subResIdx + " / " + rscId + " first insidx " + firstAvailInsertIdx + " mypath " + resourcePaths[rscId]);
                    int size = vector.Count;
                    int insertIdx = firstAvailInsertIdx;
                    while (true)
                    {
                        if (insertIdx >= vector.Count)
                        {
                            insertIdx = size;
                            break;
                        }
                        int[] other = vector[insertIdx];
                        bool pathsMatch = resourcePaths[rscId].Equals(resourcePaths[other[2]]);
                        bool z2 = insertIdx == vector.Count - 1 || !resourcePaths[rscId].Equals(resourcePaths[vector[insertIdx + 1][2]]);
                        if (pathsMatch && resourceInfo[rscId].SkipOffset < resourceInfo[other[2]].SkipOffset)
                            break;
                        if (pathsMatch && z2)
                        {
                            insertIdx++;
                            break;
                        }
                        insertIdx++;
                    }
                    //System.out.println("act insidx " + insertIdx);
                    firstAvailInsertIdx = subResIdx == -1 ? insertIdx + 1 : firstAvailInsertIdx;
                    vector.Insert(insertIdx, [batchId, subResIdx, rscId]);
                    subResIdx++;
                }
            }
        }

        DataInputStream lastStream = null;
        string lastRscPath = null;
        int curStreamPos = 0;

        for (int index = 0; index < vector.Count; index++)
        {
            try
            {
                int[] resLoadInfo = vector[index];
                int batchId = resLoadInfo[0];
                int subResIdx = resLoadInfo[1];
                int rscId = resLoadInfo[2];
                string rscPath = resourcePaths[rscId];
                int skipOffset = resourceInfo[rscId].SkipOffset;
                int readLength = resourceInfo[rscId].ReadLength;
                if (lastStream == null || !rscPath.Equals(lastRscPath) || curStreamPos > skipOffset)
                {
                    if (lastStream != null)
                    {
                        lastStream.Dispose();
                        lastStream = null;
                    }
                    if (!(skipOffset == -1 || readLength == 0))
                        lastStream = GetStreamForRscId(rscId);
                }
                else
                    ForceSkipBytes(lastStream, skipOffset - curStreamPos);

                bool subResLoadSuccess = false;
                if (subResIdx == -1)
                {
                    // main resource
                    DataInputStream rscStream = skipOffset == -1 ? new DataInputStream(MidLet.System.GetResourceAsStream("/" + rscPath)) : lastStream;
                    if (rscStream == null && rscPath.Trim().Length > 0)
                        throw new IOException("Could not open stream for resource " + rscPath + " (ID " + rscId + ")");
                    for (int rtIdx = 0; rtIdx < resHandlers.Length; rtIdx++)
                    {
                        try
                        {
                            object friendlyArray = resHandlers[rtIdx].ReadResource(
                                readLength != 0 ? rscStream : null,
                                readLength,
                                resourceBatchInfo[batchId].ResType,
                                batchId
                            );
                            if (friendlyArray != null)
                            {
                                loadedResources[batchId] = friendlyArray;
                                subResLoadSuccess = true;
                                lastStream = rscStream;
                            }
                            else
                            {
                                Trace.WriteLine("Failed to load resource batch " + batchId);
                                rtIdx++;
                            }
                        }
                        catch (IOException ex)
                        {
                            Debug.WriteLine(ex);
                            throw new IOException("Failed to read resource " + rscPath + " (ID " + rscId + " skip " + skipOffset + " batch " + batchId + " is cachestream " + (rscStream == lastStream) + ")");
                        }
                    }
                    lastStream = rscStream;
                }
                else if (readLength != 0)
                {
                    for (int i = 0; i < resHandlers.Length; i++)
                    {
                        if (resHandlers[i].LoadResource(lastStream, readLength == -1 ? rscPath : null, readLength, resourceBatchInfo[batchId].ResType, batchId, subResIdx))
                        {
                            subResLoadSuccess = true;
                            break;
                        }
                    }
                }
                if (!subResLoadSuccess && skipOffset != -1)
                    ForceSkipBytes(lastStream, readLength);
                if (lastStream != null)
                {
                    newCurStreamPos = skipOffset + readLength;
                    newLastRscPath = rscPath;
                }
                else
                {
                    newCurStreamPos = curStreamPos;
                    newLastRscPath = lastRscPath;
                }
                curStreamPos = newCurStreamPos;
                lastRscPath = newLastRscPath;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        if (lastStream != null)
        {
            try
            {
                lastStream.Dispose();
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex);
            }
        }
        for (int batchIndex = 0; batchIndex < resLoadQueue.Count; batchIndex++)
            isResourceLoaded[resLoadQueue[batchIndex]] = true;
        resLoadQueue.Clear();
    }

    private static void PreloadResourceImpl(byte[] bytes, int id, string resourcePath)
    {
        if (bytes == null)
        {
            try
            {
                bytes = ReadInputStreamToBytes(MidLet.System.GetResourceAsStream(resourcePath), -1);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        if (id >= 0)
        {
            loadedResources[id] = bytes;
            isResourceLoaded[id] = bytes != null;
        }
    }

    private static byte[] ReadInputStreamToBytes(Stream inputStream, int len)
    {
        byte[] bytes;
        if (len != -1)
        {
            int i = 0;
            bytes = new byte[len];
            while (i < bytes.Length)
                i += inputStream.Read(bytes, i, bytes.Length - i);
            return bytes;
        }
        using MemoryStream result = new();
        bytes = new byte[1024];
        while (true)
        {
            int read = inputStream.Read(bytes);
            if (read <= 0)
                break;
            result.Write(bytes, 0, read);
        }
        return result.ToArray();
    }

    public static void UnloadResource(int resId)
    {
        if (resId >= 0)
        {
            int num = resId;
            if (!resUnloadQueue.Contains(num))
                resUnloadQueue.Add(num);
            resLoadQueue.Remove(num);
        }
    }

    public bool UnloadResource(ResourceType resType, int unloadResId)
    {
        switch (resType)
        {
            case ResourceType.IMAGE:
                byte[] texAtlasInfo = (byte[])loadedResources[unloadResId];
                for (int k = 0; k < imageMaps2.Length; k++)
                {
                    ImageMapEx m2 = imageMaps2[k];
                    if (m2.ResBatchId == unloadResId)
                        m2.Clear();
                }
                short startImageId = GetShortFromByteArray(texAtlasInfo, 0);
                short endImageId = GetShortFromByteArray(texAtlasInfo, 2);
                for (int k = 0; k < imageMaps.Length; k++)
                {
                    ImageMap m = imageMaps[k];
                    if (m.ImageId >= startImageId && m.ImageId < endImageId)
                        m.Clear();
                }
                for (int imgResIdx = startImageId; imgResIdx < endImageId; imgResIdx++)
                {
                    imageResources[imgResIdx]?.Dispose();
                    imageResources[imgResIdx] = null;
                }
                return true;
            case ResourceType.MIDI:
                return true;
            case ResourceType.STRINGS:
                short[] stringDesc = (short[])loadedResources[unloadResId];
                int stringCount = stringDesc[0] + stringDesc[1];
                for (int s2 = stringDesc[0]; s2 < stringCount; s2++)
                {
                    residentStrings[s2 == 1 ? 1 : 0] = null;
                    s2 = (s2 == 1 ? 1 : 0) + 1;
                }
                return true;
            default:
                return false;
        }
    }

    private static void ProcessResourceUnload()
    {
        foreach (int unloadResId in resUnloadQueue)
        {
            if (isResourceLoaded[unloadResId])
            {
                ResourceType resType = resourceBatchInfo[unloadResId].ResType;
                foreach (IResourceHandler handler in resHandlers)
                {
                    if (handler.UnloadResource(resType, unloadResId))
                        break;
                }
                loadedResources[unloadResId] = null;
                isResourceLoaded[unloadResId] = false;
            }
        }
        resUnloadQueue.Clear();
    }

    public static void LoadResidentResSet(int setId)
    {
        short[] arr = residentResMap[setId];
        foreach (short res in arr)
            LoadResource(res);
    }

    private static void UpdateGameLoad()
    {
        gameIsLoading = true;
        new Thread(mInstance.RunLoad)
        {
            Name = "LoadingThread",
            IsBackground = true
        }.Start();
        ReadResourceTable();
        int eventResult = 0;
        int sceneLoadIdx = 0;
        while (true)
        {
            if (sceneLoadIdx < sceneLoadQueueSize || resLoadQueue.Count > 0 || resUnloadQueue.Count > 0)
            {
                //Debug.WriteLine("Begin loading resources: batch remaining " + batchResourceIds.size() + " single remaining " + singleResourceIds.size() + " scenes " + sceneLoaderIdx + "/" + sceneLoaderQueueSize);
                if (resUnloadQueue.Count > 0)
                {
                    ProcessResourceUnload();
                    GC.Collect();
                }
                if (resLoadQueue.Count > 0)
                {
                    ProcessResourceLoad();
                    GC.Collect();
                }
                if (sceneLoadIdx < sceneLoadQueueSize && (eventResult = BounceGame.LoadScene(sceneLoaderQueue[sceneLoadIdx], eventResult)) == 0)
                    sceneLoadIdx++;
                //Debug.WriteLine("Resource load done!");
            }
            else
            {
                lock (loadingMutex)
                {
                    gameIsLoading = false;
                    Monitor.Pulse(loadingMutex);
                }
                sceneLoadQueueSize = 0;
                ResumeRuntime();
                return;
            }
        }
    }

    public static bool IsResourceLoadDone(int resId)
    {
        return resId >= 0 && isResourceLoaded != null && isResourceLoaded[resId];
    }

    public object ReadResource(DataInputStream dis, int readLength, ResourceType type, int resBatchId)
    {
        switch (type)
        {
            case ResourceType.IMAGE:
                // Some sort of image, but not PNG...
                sbyte imageCount = dis.ReadByte();
                short baseImageID = dis.ReadShort();
                short count1 = dis.ReadShort();
                short count2 = dis.ReadShort();
                short imageMapCount = dis.ReadShort();
                for (int i = 0; i < count1; i++)
                    imageMaps2[dis.ReadShort() - imageMaps.Length].Read(resBatchId, dis);
                using (MemoryStream baos = new(count2 + 4))
                {
                    Span<byte> bytes = stackalloc byte[2];
                    BinaryPrimitives.WriteInt16BigEndian(bytes, baseImageID);
                    baos.Write(bytes);
                    BinaryPrimitives.WriteInt16BigEndian(bytes, (short)(baseImageID + imageCount));
                    baos.Write(bytes);
                    for (int dataIdx = 0; dataIdx < count2; dataIdx++)
                        baos.WriteByte((byte)dis.ReadByte());
                    for (int i = 0; i < imageMapCount; i++)
                        imageMaps[dis.ReadShort()].Read(dis);
                    return baos.ToArray();
                }
            case ResourceType.MIDI:
                return new object();
            case ResourceType.STRINGS:
                short stringCount = dis.ReadShort();
                short firstStrId = dis.ReadShort();
                int headerFieldId = 0;
                int sectionEnd = 0;
                int stringSectionSize = -999999;
                int skipToStrStart = -999999;
                while (true)
                {
                    if (headerFieldId < residentStringFieldCount + 1)
                    {
                        if (headerFieldId == 0)
                            skipToStrStart = dis.ReadInt();
                        else
                        {
                            sectionEnd = dis.ReadInt();
                            if (headerFieldId == 1) // first section
                                stringSectionSize = sectionEnd - skipToStrStart;
                        }
                        headerFieldId++;
                    }
                    else
                    {
                        ForceSkipBytes(dis, skipToStrStart);
                        for (int i = 0; i < stringCount; i++)
                        {
                            if (residentStrings[firstStrId + i] == null)
                                residentStrings[firstStrId + i] = dis.ReadUTF();
                            else
                                dis.ReadUTF();
                        }
                        ForceSkipBytes(dis, sectionEnd - (skipToStrStart + stringSectionSize));
                        return new[]
                        {
                            firstStrId, stringCount
                        };
                    }
                }
            default:
                return ReadInputStreamToBytes(dis.Stream, readLength);
        }
    }

    public bool LoadResidentData(DataInputStream dis, int type)
    {
        switch (type)
        {
            case -1:
                imageMaps2 = new ImageMapEx[214];
                imageMaps = new ImageMap[326];
                imageResources = new Image[46];
                for (int mapIdx = 0; mapIdx < imageMaps2.Length; mapIdx++)
                    imageMaps2[mapIdx] = new ImageMapEx();
                for (int mapIdx = 0; mapIdx < imageMaps.Length; mapIdx++)
                    imageMaps[mapIdx] = new ImageMap();
                return false;
            default:
                return false;
            case 4:
                residentStringFieldCount = dis.ReadByte();
                residentStrings = new string[dis.ReadShort()];
                return false;
            case 6: // contains BGM and splash screen
                int resMapGroupCount = dis.ReadShort();
                residentResMap = new short[resMapGroupCount][];
                for (int groupIndex = 0; groupIndex < resMapGroupCount; groupIndex++)
                {
                    int resBatchCount = dis.ReadShort();
                    residentResMap[groupIndex] = new short[resBatchCount];
                    for (int resBatchIndex = 0; resBatchIndex < resBatchCount; resBatchIndex++)
                        residentResMap[groupIndex][resBatchIndex] = dis.ReadShort();
                }
                return true;
            case 7:
                return true;
        }
    }

    private void RunLoad()
    {
        if (!GameThreadStarted)
            return;
        try
        {
            Debug.WriteLine("Starting LoadingThread");
            lock (loadingMutex)
            {
                try
                {
                    Monitor.Wait(loadingMutex, LOADING_WAIT_TIMEOUT);
                    while (gameIsLoading)
                    {
                        UpdateViewport();
                        CallGamePaint(2);
                        Monitor.Wait(loadingMutex, LOADING_WAIT_TIMEOUT);
                    }
                }
                catch (ThreadInterruptedException e)
                {
                    Debug.WriteLine(e);
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
        Debug.WriteLine("LoadingThread ended");
    }

    public static void Initialize()
    {
        try
        {
            Debug.WriteLine("Initializing game");

            GameThreadStarted = true;
            systemEventQueue = new SystemEvent[20];
            systemEventQueueSize = 0;
            sceneLoaderQueue = new GameScene[40];
            sceneLoadQueueSize = 0;
            keyQueue = new KeyCode[20];
            keyQueueSize = 0;
            reqSoftkeyTexts = new string[3];
            softkeyTexts = new string[3];
            softkeyUITypes = new int[3];
            //Display.GetDisplay(MidLet).SetCurrent(this);
            //mInstance.SetFullScreenMode(true);
            UpdateViewport();
            resLoadQueue = [];
            resUnloadQueue = [];
            resHandlers = [mInstance];
            BounceGame = new BounceGame();
            NotifySystemEvent(SystemEvent.START);
            ResumeRuntime();
            MidLet.Initialize();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    public static bool Update()
    {
        try
        {
            currentTime = CurrentTimeMillis();
            if (!isGamePaintEnabled)
                reqSystemGamePause = true;
            UpdateViewport();
            if (systemEventQueueSize > 0)
            {
                lock (systemEventQueue)
                {
                    for (int i = 0; i < systemEventQueueSize; i++)
                    {
                        BounceGame.OnSystemEvent(systemEventQueue[i]);
                        systemEventQueue[i] = 0;
                    }
                    systemEventQueueSize = 0;
                }
                lock (gameMutex)
                {
                    if (music_IdQueuedAfterSysUnpause != -1)
                    {
                        // this is bugged as the menu music will be muted on incoming call
                        // however, the straightforward way to fix it wouldn't work, as sounds are not allowed when this part of the code runs
                        // not sure if I want to rewrite this or preserve the original behavior

                        //if (music_IdBeforeSysPause != music_IdQueuedAfterSysUnpause) { // REMOVED IN 2.0.25
                        PlayMusic(music_IdQueuedAfterSysUnpause, true);
                        //}
                        music_IdQueuedAfterSysUnpause = -1;
                    }
                }
            }
            if (IsGameLoading())
            {
                for (int softkeyIdx = 0; softkeyIdx < reqSoftkeyTexts.Length; softkeyIdx++)
                    softkeyTexts[softkeyIdx] = null;
                UpdateGameLoad();
                currentTime = CurrentTimeMillis();
            }
            // Stuff...
            if (!reqSystemGamePause)
            {
                for (int softkeyIdx = 0; softkeyIdx < reqSoftkeyTexts.Length; softkeyIdx++)
                {
                    if (!ObjectsEquals(softkeyTexts[softkeyIdx], reqSoftkeyTexts[softkeyIdx]))
                    {
                        if (softkeyTexts[softkeyIdx] == null || !softkeyTexts[softkeyIdx].Equals(reqSoftkeyTexts[softkeyIdx]))
                            softkeyTexts[softkeyIdx] = reqSoftkeyTexts[softkeyIdx];
                        else
                            reqSoftkeyTexts[softkeyIdx] = softkeyTexts[softkeyIdx];
                    }
                }
                CallGamePaint(1);
                if (!reqSystemGamePause)
                {
                    GameUpdate();
                    if (reqClose)
                    {
                        Debug.WriteLine("Game is about to close.");
                        return false;
                    }
                    if (typingKeyIsHeld && typingKeyHeldId >= KeyCode.NUM0 && typingKeyHeldId <= KeyCode.NUM9 && CurrentTimeMillis() - typingKeyHoldStartTime > 500)
                        EndTypingKeyHold(typingKeyHeldId);
                    if (reqSystemGamePause)
                        WaitPausedRuntime();
                    else
                        Thread.Sleep(1);
                }
                else
                    WaitPausedRuntime();
            }
            else
                WaitPausedRuntime();
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
        }
        return true;
    }

    public static void Shutdown()
    {
        try
        {
            BounceGame?.Shutdown();

            BounceGame = null;
            StopMusic();

            for (int unloadIdx = 0; unloadIdx < loadedResources.Length; unloadIdx++) // unload all resources
                resUnloadQueue.Add(unloadIdx);
            ProcessResourceUnload();
            
            MidLet = null;
            mInstance = null;
            ResetGlobalState();

            // TEMPORARY
            if (Image.notDisposedCount == 0)
                Debug.WriteLine("IMPORTANT: All images disposed.");
            else
                Debug.WriteLine($"IMPORTANT: {Image.notDisposedCount} Images weren't disposed.");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    private static void WaitPausedRuntime()
    {
        lock (gameMutex)
        {
            if (!IsGameLoading())
            {
                reqSystemGamePause = false;
                NotifySystemEvent(SystemEvent.PAUSE);
            }
            if (midletIsPaused)
            {
                try
                {
                    Monitor.Wait(gameMutex);
                }
                catch (ThreadInterruptedException)
                {
                }
            }
        }
        ResumeRuntime();
    }

    private static void ResumeRuntime()
    {
        lastUpdateTimestamp = 0;
        UpdateDelta = 0;
        ResetHID();
    }

    public static void SetState(GameState state)
    {
        lock (gameMutex)
        {
            Debug.WriteLine("New game state " + state);
            buttonsDown = 0;
            buttonsHeld = 0;
            buttonsHit = 0;
            if (state == GameState.INIT)
            {
                mInstance = new GameRuntime();
                Initialize();
            }
            else if (mInstance != null)
            {
                if (state == GameState.PAUSE || state == GameState.HIDDEN)
                {
                    if (!midletIsPaused)
                    {
                        midletIsPaused = true;
                        reqSystemGamePause = true;
                        int returnMusic = music_IdCurrent;
                        StopMusic(); // since 2.0.25
                        music_IdQueuedAfterSysUnpause = returnMusic;
                        music_IdBeforeSysPause = returnMusic;
                    }
                    else if ((state == GameState.RUN || state == GameState.SHOWN) && midletIsPaused)
                    {
                        Debug.WriteLine("Midlet shown, releasing mutex...");
                        midletIsPaused = false;
                        Monitor.PulseAll(gameMutex);
                    }
                }
            }
            else
                Debug.WriteLine("State change requested, but instance not available!");
        }
    }

    private static void NotifySystemEvent(SystemEvent eventId)
    {
        if (systemEventQueueSize != 20)
        {
            systemEventQueue[systemEventQueueSize] = eventId;
            systemEventQueueSize++;
        }
    }

    private static void UpdateViewport()
    {
        int width = mInstance.GetWidth();
        int height = mInstance.GetHeight();
        if (CurrentWidth != width || CurrentHeight != height)
        {
            CurrentWidth = width;
            CurrentHeight = height;
            if (BounceGame != null)
                NotifySystemEvent(SystemEvent.RESIZE);
        }
    }

    private static void GameUpdate()
    {
        long currentTimeMillis = CurrentTimeMillis();
        if (lastUpdateTimestamp != 0)
        {
            int delta = (int)(currentTimeMillis - lastUpdateTimestamp);
            int deltaPerUpdate = delta / updatesPerDraw;
            UpdateDelta = deltaPerUpdate;
            if (deltaPerUpdate > maxUpdateDelta)
                UpdateDelta = maxUpdateDelta;
            if (UpdateDelta == 0)
            {
                // when the game is too fast (such as after out jademula drawRegion fix)
                // the update delta can sometimes be so small that we can't even notice it
                // if this happens too much, the imprecision will actually slow the game down
                // because of too many subsequent zero deltas. thus, we will skip the update altogether.
                return;
            }
        }
        else
            UpdateDelta = 0;

        lastUpdateTimestamp = currentTimeMillis;
        disableHID = false;
        for (int updateIdx = 0; updateIdx < updatesPerDraw; updateIdx++)
        {
            for (int keyComboIndex = 0; keyComboIndex < keyQueueSize; keyComboIndex++)
            {
                BounceGame.HandleKeyPress(keyQueue[keyComboIndex]);
                keyQueue[keyComboIndex] = KeyCode.INVALID;
            }
            keyQueueSize = 0;
            buttonsHeld = buttonsDown | buttonsHit;
            buttonsHit = 0;
            for (int updateRes = BounceGame.Update(0); updateRes != 0; updateRes = BounceGame.Update(updateRes))
            {
            }
            if (IsGameLoading())
                return;
        }
    }

    private static bool ObjectsEquals(object a, object b)
    {
        if (a == null)
            return b == null;
        return b != null && a.Equals(b);
    }
}
