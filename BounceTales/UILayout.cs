using System.Diagnostics;
using BounceTales.Ext.Rsc;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class UILayout
{
    // TODO: Convert attributes to an enum.
    public const int ELEMENT_ALIGNMENT = 0;
    public const int TITLE_ALIGNMENT = 1;
    public const int SOFTKEY_BAR = 2;
    public const int PACKED_HEIGHT = 3;
    public const int ANCHOR_CENTER = 4;
    public const int SCROLL_WRAPAROUND = 5;
    public const int TITLE_PADDING_TOP = 6;
    public const int TITLE_PADDING_BOTTOM = 7;
    public const int TITLE_PADDING_SIDE = 8;
    public const int OFFSET_LEFT = 9;
    public const int OFFSET_TOP = 10;
    public const int FIXED_WIDTH = 11;
    public const int FIXED_HEIGHT = 12;
    public const int FONT = 13;
    public const int BLOCK_INCREMENT = 14;
    public const int MARGIN_LEFT = 15;
    public const int MARGIN_RIGHT = 16;
    public const int MARGIN_TOP = 17;
    public const int MARGIN_BOTTOM = 18;
    public const int VERTICAL_SPACING = 19;
    public const int CONTENT_PANE_COLOR = 20;
    public const int SOFTKEY_BAR_COLOR = 21;
    public const int BACKGROUND_COLOR = 22;
    public const int BORDER_COLOR = 23;
    public const int FONT_TEXT_COLOR = 24;
    public const int FONT_SHADOW_COLOR = 25;

    public const int SOFTKEY_BAR_BIT = 16;
    public const int PACKED_HEIGHT_BIT = 32;
    public const int ANCHOR_CENTER_BIT = 64;
    public const int SCROLL_WRAPAROUND_BIT = 128;

    private static readonly int[] childClipTemp = new int[4];

    private static readonly int[] ELEMENT_FLAG_MASKS = [3, 12, 48, 64, 128, 256, 1536, 2048, 4096];
    private static readonly int[] DEFAULT_ELEMENT_ATTRIBUTES = [-1, 7304, -1, -1, 1, 1, 1, 1, 0xFFFFFF, 0, 0, 0x666666, 0, 0, 0x666666, 0xAA7700];

    private static readonly int[] LAYOUT_FLAG_MASKS = [3, 12, 16, 32, 64, 128];
    private static readonly int[] DEFAULT_LAYOUT_ATTRIBUTES = [-1, 8, 2, 2, 2, 0, 0, -1, -1, -1, 20, 12, 12, 2, 6, 1, 0x440000, 0x5E2601, 68, 0xA0A0A0, 0, 0x990000, 0];

    public GameScene UIID = 0;

    public int[] ElemDefaultAttributes;
    public int[] LayoutAttributes;

    private readonly List<UIElement> elements = [];
    private int selectedElemIdx = -1;
    private int focusStartY;

    private TextLabel titleLabel;
    private int titleImageId = -1;
    private int titleAnimTimer;
    private int titleXScroll;
    private int titleScrollDirection = 1;

    private readonly string[] softkeyTexts = new string[3];
    private readonly int[] softkeyActions = new int[3];
    private readonly int[] softkeyTypes = new int[3];

    public static bool AttributeExists(int aid, int[] arr)
    {
        return arr != null && (arr[0] & 1 << aid) != 0;
    }

    private int GetAttribute(int aid)
    {
        return ReadAttribute(aid, LayoutAttributes, 2);
    }

    public void SetElemDefaultAttribute(int aid, int value)
    {
        ElemDefaultAttributes = WriteAttribute(aid, value, ElemDefaultAttributes, 1);
    }

    public void SetAttribute(int aid, int value)
    {
        LayoutAttributes = WriteAttribute(aid, value, LayoutAttributes, 2);
    }

    public static int ReadAttribute(int aid, int[] arr, int type)
    {
        int[] srcArr;
        int[] flagAttrMasks;
        if (type == 1)
        {
            srcArr = DEFAULT_ELEMENT_ATTRIBUTES;
            flagAttrMasks = ELEMENT_FLAG_MASKS;
        }
        else
        {
            srcArr = DEFAULT_LAYOUT_ATTRIBUTES;
            flagAttrMasks = LAYOUT_FLAG_MASKS;
        }
        int flagCount = flagAttrMasks.Length;
        if (AttributeExists(aid, arr))
            srcArr = arr;
        if (aid >= flagCount)
            return srcArr[aid + 2 - flagCount];
        return flagAttrMasks[aid] & srcArr[1];
    }

    public static int[] WriteAttribute(int aid, int value, int[] arr, int type)
    {
        int avalArrSize;
        int[] flagAttrMasks;
        if (type == 1)
        {
            avalArrSize = 16;
            flagAttrMasks = ELEMENT_FLAG_MASKS;
        }
        else
        {
            avalArrSize = 23;
            flagAttrMasks = LAYOUT_FLAG_MASKS;
        }
        if (arr == null)
            arr = new int[avalArrSize];
        else if (arr.Length < avalArrSize)
        {
            int[] iArr3 = new int[avalArrSize];
            arr.CopyTo(iArr3, 0);
            arr = iArr3;
        }
        arr[0] = arr[0] | 1 << aid;
        if (aid < flagAttrMasks.Length)
        {
            arr[1] = arr[1] & ~flagAttrMasks[aid];
            arr[1] = arr[1] | flagAttrMasks[aid] & value;
        }
        else
            arr[aid + 2 - flagAttrMasks.Length] = value;
        return arr;
    }

    public void AddElement(UIElement option)
    {
        int size = elements.Count;
        elements.Add(option);
        option.ParentUI = this;
        if (selectedElemIdx == -1 && IsElemInFocus(size))
            SetSelectedOption(size);
    }

    public void Clear()
    {
        elements.Clear();
        focusStartY = 0;
        selectedElemIdx = -1;
    }

    private UIElement GetSelectedElement()
    {
        UIElement elem = selectedElemIdx != -1 ? elements[selectedElemIdx] : null;
        if (elem == null || !elem.IsEnabled)
            return null;
        return elem;
    }

    public int GetSelectedOption()
    {
        return selectedElemIdx;
    }

    public void SetSelectedOption(int elemIdx)
    {
        if (elemIdx >= elements.Count)
            elemIdx = elements.Count - 1;
        if (elemIdx != selectedElemIdx)
        {
            if (elemIdx == -1 || elements[elemIdx].HasAction())
            {
                //int lastSelOption = selectedElemIdx;
                selectedElemIdx = elemIdx;
                // ?????
                //if (lastSelOption != -1)
                //    elements[lastSelOption];
                //if (elemIdx != -1)
                //    elements[elemIdx];
                if (selectedElemIdx != -1)
                {
                    int startY = GetElemStartY(selectedElemIdx);
                    int endY = elements[selectedElemIdx].GetHeight() + startY;
                    int focusHeight = GetFocusHeight();
                    if (startY < focusStartY)
                        focusStartY = startY;
                    else if (endY > focusStartY + focusHeight)
                        focusStartY = endY - focusHeight;
                }
            }
        }
    }

    private bool IsElemInFocus(int elemIdx)
    {
        UIElement elem = elements[elemIdx];
        int startY = GetElemStartY(elemIdx);
        return startY >= focusStartY && elem.GetHeight() + startY <= focusStartY + GetFocusHeight();
    }

    private int GetElemStartY(int elemIdx)
    {
        int totalHeight = 0;
        for (int i = 0; i < elemIdx; i++)
            totalHeight += elements[i].GetHeight() + GetAttribute(VERTICAL_SPACING);
        return totalHeight;
    }

    private static int GetElemXAnchor(UIElement elem, int parentWidth)
    {
        int anchor = elem.GetAttribute(UIElement.ANCHOR_H);
        if (anchor == 512)
            return parentWidth - elem.GetWidth();
        if (anchor == 1024)
            return parentWidth - elem.GetWidth() >> 1;
        return 0;
    }

    private int GetLayoutVDim(int i)
    {
        switch (i)
        {
            case 1: // max content Y
                int layoutYMax = GameRuntime.CurrentHeight;
                if (GameRuntime.IsScreenPortrait())
                    layoutYMax -= GameRuntime.GetSoftkeyBarHeight();
                if (GetAttribute(PACKED_HEIGHT) == PACKED_HEIGHT_BIT)
                    return Math.Min(GetLayoutVDim(2) + GetTotalHeight() + GetAttribute(MARGIN_TOP) + GetAttribute(MARGIN_BOTTOM), layoutYMax);
                int c = GetAttribute(FIXED_HEIGHT);
                return c <= 0 ? layoutYMax : c;
            case 2: // min content Y
                int titleYEnd = titleLabel != null ? titleLabel.TextBlockHeight : 0;
                int imgHeight = 0;
                if (titleImageId != -1)
                    imgHeight = GameRuntime.GetImageMapParam(titleImageId, ImageMap.Param.HEIGHT);
                return Math.Max(titleYEnd, imgHeight) + GetAttribute(TITLE_PADDING_TOP) + GetAttribute(TITLE_PADDING_BOTTOM);
            case 3:
                return 0;
            case 4: // content max height
                return GetLayoutVDim(1) - GetLayoutVDim(2);
            case 5:
                return GetLayoutVDim(4) - GetAttribute(MARGIN_TOP) - GetAttribute(MARGIN_BOTTOM);
            case 6:
                return GameRuntime.GetImageMapParam(151, ImageMap.Param.HEIGHT) + 1;
            case 7:
                return GameRuntime.GetImageMapParam(150, ImageMap.Param.HEIGHT) + 1;
            default:
                return 0;
        }
    }

    private int GetLayoutHDim()
    {
        int c = GetAttribute(FIXED_WIDTH);
        if (c > 0)
            return c;
        return GameRuntime.CurrentWidth - (GameRuntime.IsScreenLandscape() ? GameRuntime.GetSoftkeyBarWidth() : 0);
    }

    private int GetTotalHeight()
    {
        if (elements.Count <= 0)
            return 0;
        return elements[^1].GetHeight() + GetElemStartY(elements.Count - 1);
    }

    public int GetFocusWidth()
    {
        return GetLayoutHDim() - GetAttribute(MARGIN_LEFT) - GetAttribute(MARGIN_RIGHT);
    }

    private int GetFocusHeight()
    {
        int b = GetLayoutVDim(5);
        return GetTotalHeight() > b ? b - GetLayoutVDim(TITLE_PADDING_TOP) - GetLayoutVDim(7) : b;
    }

    public void ChangeSoftkey(GameRuntime.Softkey softkey, string text, int type)
    {
        if ((softkeyActions[(int)softkey] & 0xFFFF) == 0xFFFF)
        {
            softkeyTexts[(int)softkey] = text;
            softkeyTypes[(int)softkey] = type;
        }
    }

    public void SetSoftkey(GameRuntime.Softkey softkey, string buttonText, int type, GameScene action, bool z)
    {
        softkeyActions[(int)softkey] = 0x110000 | (int)action & 0xFFFF;
        softkeyTexts[(int)softkey] = buttonText;
        softkeyTypes[(int)softkey] = type;
    }

    public void DisableSoftkey(int buttonIdx)
    {
        softkeyActions[buttonIdx] = 0;
        softkeyTexts[buttonIdx] = null;
        softkeyTypes[buttonIdx] = 0;
    }

    public void SetupSoftkeys()
    {
        for (int softkeyIdx = 0; softkeyIdx < 3; softkeyIdx++)
        {
            bool z = (short)softkeyActions[softkeyIdx] == -2;
            UIElement selElem = GetSelectedElement();
            if ((softkeyActions[softkeyIdx] & 0xFFFF) == 0 || (softkeyActions[softkeyIdx] & 0x100000) == 0 || z && selElem == null)
                GameRuntime.SetSoftkey((GameRuntime.Softkey)softkeyIdx, null, 0);
            else
                GameRuntime.SetSoftkey((GameRuntime.Softkey)softkeyIdx, softkeyTexts[softkeyIdx], softkeyTypes[softkeyIdx]);
        }
    }

    public void SetTitle(string str, int bgImage, int i2)
    {
        titleLabel = str == null || str.Length <= 0 ? null : new TextLabel(str, int.MaxValue, GetAttribute(FONT), GetAttribute(ELEMENT_ALIGNMENT) | 256, -1);
        titleImageId = bgImage;
        titleXScroll = 0;
    }

    public void UpdateTitleScroll()
    {
        if (titleLabel != null)
        {
            int titleImageWidth = titleImageId != -1 ? GameRuntime.GetImageMapParam(titleImageId, ImageMap.Param.WIDTH) : 0;
            int c = GetLayoutHDim() - (GetAttribute(TITLE_PADDING_SIDE) << 1);
            int i = GetAttribute(TITLE_ALIGNMENT) != 8 ? c - titleImageWidth : c;
            if (i < titleLabel.TextBlockWidth)
            {
                if (titleAnimTimer >= 3000)
                {
                    if (titleScrollDirection > 0)
                    {
                        titleXScroll = (titleAnimTimer - 3000) * 20 / 1000;
                        if (titleXScroll > titleLabel.TextBlockWidth - i)
                        {
                            titleXScroll = titleLabel.TextBlockWidth - i;
                            titleAnimTimer = 0;
                            titleScrollDirection = -1;
                        }
                    }
                    else
                    {
                        titleXScroll = titleLabel.TextBlockWidth - i - (titleAnimTimer - 3000) * 20 / 1000;
                        if (titleXScroll <= 0)
                        {
                            titleXScroll = 0;
                            titleAnimTimer = 0;
                            titleScrollDirection = 1;
                        }
                    }
                }
                titleAnimTimer += GameRuntime.UpdateDelta;
            }
        }
    }

    public void HandleKeyCode(KeyCode keyCode)
    {
        int i2;
        int i3;
        int resultSoftkey;
        UIElement selElem = GetSelectedElement();
        switch (keyCode)
        {
            case KeyCode.UP:
                if (elements.Count > 0)
                {
                    if (selectedElemIdx != -1)
                    {
                        int i5 = selectedElemIdx - 1;
                        while (true)
                        {
                            if (i5 < 0)
                            {
                                i5 = -1;
                                break;
                            }
                            if (!elements[i5].HasAction())
                                i5--;
                            break;
                        }
                        i3 = i5 != -1 ? elements[i5].GetHeight() + GetElemStartY(i5) < focusStartY - GetAttribute(BLOCK_INCREMENT) ? -1 : i5 : i5;
                    }
                    else
                    {
                        i3 = -1;
                        for (int j = 0; j < elements.Count; ++j)
                        {
                            UIElement f = elements[j];
                            int a2;
                            int n4 = (a2 = GetElemStartY(j)) + f.GetHeight();
                            if (a2 >= focusStartY)
                                break;
                            if (f.HasAction() && n4 > focusStartY - GetAttribute(BLOCK_INCREMENT))
                                i3 = j;
                        }
                    }
                    if (i3 != -1)
                    {
                        SetSelectedOption(i3);
                        return;
                    }
                    if (GetAttribute(SCROLL_WRAPAROUND) == 0 || focusStartY > 0)
                    {
                        focusStartY -= GetAttribute(BLOCK_INCREMENT);
                        if (focusStartY < 0)
                            focusStartY = 0;
                        if (!(selectedElemIdx == -1 || IsElemInFocus(selectedElemIdx)))
                            SetSelectedOption(-1);
                    }
                    else
                    {
                        int d = GetTotalHeight();
                        focusStartY = d - GetFocusHeight();
                        if (d < GetLayoutVDim(5))
                            focusStartY = 0;
                        for (int size = elements.Count - 1; size > 0; size--)
                        {
                            if (!IsElemInFocus(size))
                            {
                                SetSelectedOption(-1);
                                break;
                            }
                            if (elements[size].HasAction())
                            {
                                SetSelectedOption(size);
                                break;
                            }
                        }
                    }
                }
                return;
            case KeyCode.DOWN:
                if (elements.Count > 0)
                {
                    int d2 = GetTotalHeight();
                    int e = GetFocusHeight();
                    if (selectedElemIdx != -1)
                    {
                        int i8 = selectedElemIdx + 1;
                        while (true)
                        {
                            if (i8 >= elements.Count)
                            {
                                i2 = -1;
                                break;
                            }
                            if (elements[i8].HasAction())
                            {
                                i2 = i8;
                                break;
                            }
                            i8++;
                        }
                        if (i2 != -1 && GetElemStartY(i2) > focusStartY + e + GetAttribute(BLOCK_INCREMENT))
                            i2 = -1;
                    }
                    else
                    {
                        i2 = -1;
                        for (int n8 = elements.Count - 1; n8 >= 0; --n8)
                        {
                            UIElement f2 = elements[n8];
                            int n9;
                            if ((n9 = GetElemStartY(n8) + f2.GetHeight()) < focusStartY + e)
                                break;
                            if (f2.HasAction() && n9 <= focusStartY + e + GetAttribute(BLOCK_INCREMENT))
                                i2 = n8;
                        }
                    }
                    if (i2 != -1)
                        SetSelectedOption(i2);
                    else if (GetAttribute(SCROLL_WRAPAROUND) == 0 || focusStartY < d2 - e)
                    {
                        focusStartY += GetAttribute(BLOCK_INCREMENT);
                        if (d2 <= e)
                            focusStartY = 0;
                        else if (focusStartY > d2 - e)
                            focusStartY = d2 - e;
                        if (!(selectedElemIdx == -1 || IsElemInFocus(selectedElemIdx)))
                            SetSelectedOption(-1);
                    }
                    else
                    {
                        focusStartY = 0;
                        for (int j = 0; j < elements.Count - 1; j++)
                        {
                            UIElement fVar3 = elements[j];
                            if (!IsElemInFocus(j))
                            {
                                SetSelectedOption(-1);
                                break;
                            }
                            if (fVar3.HasAction())
                            {
                                SetSelectedOption(j);
                                break;
                            }
                        }
                    }
                }
                return;
            case KeyCode.SOFTKEY_RIGHT:
                resultSoftkey = 1;
                break;
            case KeyCode.SOFTKEY_LEFT:
            case (KeyCode)24:
                resultSoftkey = 2;
                break;
            case KeyCode.SOFTKEY_MIDDLE:
                resultSoftkey = 0;
                break;
            default:
                return;
        }
        if ((softkeyActions[resultSoftkey] & 0x10000) != 0)
        {
            GameScene s = (GameScene)(short)softkeyActions[resultSoftkey];
            if (s != GameScene.SELECTED)
                GameRuntime.BounceGame.ChangeScene(s);
            else if (selElem != null)
                GameRuntime.BounceGame.ChangeScene(selElem.Action);
        }
    }

    public void Draw()
    {
        int lytX;
        int lytY;
        int titleImageX;
        Graphics grp = GameRuntime.GetGraphicsObj();

        int contentH = GetLayoutVDim(2);
        if (GetAttribute(ANCHOR_CENTER) == 64) // anchor to X center of drawable area
            lytX = (GameRuntime.CurrentWidth - GetLayoutHDim() >> 1) + (GameRuntime.GetScreenOrientationFromSoftkeys() == GameRuntime.ScreenOrientation.RLANDSCAPE ? GameRuntime.GetSoftkeyBarWidth() : 0);
        else // anchor relative to left
            lytX = GetAttribute(OFFSET_LEFT) + (GameRuntime.GetScreenOrientationFromSoftkeys() == GameRuntime.ScreenOrientation.RLANDSCAPE ? GameRuntime.GetSoftkeyBarWidth() : 0);

        if (GetAttribute(ANCHOR_CENTER) == 64) // anchor to Y center of drawable area
            lytY = GameRuntime.CurrentHeight - (GameRuntime.IsScreenPortrait() ? GameRuntime.GetSoftkeyBarHeight() : 0) - GetLayoutVDim(1) >> 1;
        else // anchor relative to top
            lytY = GetAttribute(OFFSET_TOP) + (GameRuntime.GetScreenOrientationFromSoftkeys() == GameRuntime.ScreenOrientation.RPORTRAIT ? GameRuntime.GetSoftkeyBarHeight() : 0);

        int lytW = GetLayoutHDim();
        int lytH = GetLayoutVDim(1);
        if (BounceGame.DrawUIGraphics(this, 1, lytX, lytY, lytW, lytH))
        {
            grp.SetColor(GetAttribute(BACKGROUND_COLOR));
            grp.FillRect(lytX, lytY, lytW, lytH);
            grp.SetColor(GetAttribute(BORDER_COLOR));
            grp.DrawRect(lytX, lytY, lytW - 1, lytH - 1);
        }

        if (contentH > 0)
        {
            grp.SetClip(lytX, lytY, lytW, contentH);
            if (BounceGame.DrawUIGraphics(this, 2, lytX, lytY, lytW, contentH))
            {
                grp.SetColor(GetAttribute(20));
                grp.FillRect(lytX, lytY, lytW, contentH);
            }

            grp.SetClip(lytX, lytY, lytW, contentH);
            if (BounceGame.DrawUIGraphics(this, 3, lytX, lytY, lytW, contentH))
            {
                grp.SetClip(lytX, lytY, lytW, contentH);
                int sidePadding = GetAttribute(TITLE_PADDING_SIDE);
                int titleImageWidth = titleImageId != -1 ? GameRuntime.GetImageMapParam(titleImageId, ImageMap.Param.WIDTH) : 0;
                int titleTextWidth = titleLabel != null ? titleLabel.TextBlockWidth : 0;
                int titleMaxWidth = lytW - (sidePadding << 1);
                int i4 = GetAttribute(TITLE_ALIGNMENT) != 8 ? titleMaxWidth - titleImageWidth : titleMaxWidth;
                if (titleImageId != -1)
                {
                    titleImageX = GetAttribute(TITLE_ALIGNMENT) switch
                    {
                        4 => titleMaxWidth - titleImageWidth + sidePadding, // right
                        8 => (titleMaxWidth - titleImageWidth >> 1) + sidePadding, // center
                        _ => sidePadding // left
                    };
                    GameRuntime.DrawImageResAnchored(titleImageX + lytX, GetAttribute(TITLE_PADDING_TOP) + lytY, titleImageId, Graphics.Anchor.TOP | Graphics.Anchor.LEFT);
                }
                if (titleLabel != null)
                {
                    switch (GetAttribute(TITLE_ALIGNMENT))
                    {
                        case 0:
                            sidePadding += titleImageWidth;
                            break;
                        case 4:
                            sidePadding += Math.Max(i4 - titleTextWidth, 0);
                            break;
                        case 8:
                            sidePadding += Math.Max(titleMaxWidth - titleTextWidth, 0) >> 1;
                            break;
                    }
                    int i5 = 0;
                    if (lytW < GameRuntime.CurrentWidth)
                        i5 = GameRuntime.CurrentWidth - lytW - (GameRuntime.IsScreenLandscape() ? GameRuntime.GetSoftkeyBarWidth() : 0) >> 1;
                    grp.SetClip(i5 + (sidePadding - 1), lytY - 1, i4 + 2, contentH + 2);
                    titleLabel.Draw(lytX + sidePadding - titleXScroll,
                        GetAttribute(TITLE_PADDING_TOP) + lytY,
                        GetAttribute(FONT_TEXT_COLOR),
                        GetAttribute(FONT_SHADOW_COLOR)
                    );
                }
            }
        }

        if (GetAttribute(SOFTKEY_BAR) == 16)
        {
            // softkey bar
            int softkeyBarHeight = GameRuntime.GetSoftkeyBarHeight();
            int softkeyBarWidth = GameRuntime.GetSoftkeyBarWidth();
            grp.SetClip(0, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
            grp.SetColor(GetAttribute(SOFTKEY_BAR_COLOR));
            switch (GameRuntime.GetScreenOrientationFromSoftkeys())
            {
                case GameRuntime.ScreenOrientation.RPORTRAIT:
                    grp.FillRect(0, 0, GameRuntime.CurrentWidth, softkeyBarHeight);
                    break;
                case GameRuntime.ScreenOrientation.PORTRAIT:
                    grp.FillRect(0, GameRuntime.CurrentHeight - softkeyBarHeight, GameRuntime.CurrentWidth, softkeyBarHeight);
                    break;
                case GameRuntime.ScreenOrientation.RLANDSCAPE:
                    grp.FillRect(0, 0, softkeyBarWidth, GameRuntime.CurrentHeight);
                    break;
                case GameRuntime.ScreenOrientation.LANDSCAPE:
                    grp.FillRect(GameRuntime.CurrentWidth - softkeyBarWidth, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
                    break;
            }
        }

        grp.SetClip(lytX, lytY + contentH, lytW, lytH - contentH);
        BounceGame.DrawUIGraphics(this, 4, lytX, lytY + contentH, lytW, lytH - contentH);

        int contentX = lytX + GetAttribute(MARGIN_LEFT);
        int contentY = lytY + contentH + GetAttribute(MARGIN_TOP);
        int focusWidth = GetFocusWidth();
        int focusHeight = GetFocusHeight();
        int d2 = GetTotalHeight();
        int b4 = GetLayoutVDim(5);
        grp.SetClip(contentX, contentY, focusWidth, b4);
        BounceGame.DrawUIGraphics(this, 5, contentX, contentY, focusWidth, b4);

        grp.SetClip(contentX, contentY, focusWidth, b4);
        if (focusStartY > 0)
        {
            grp.SetClip(contentX, contentY, focusWidth, GetLayoutVDim(6));
            if (BounceGame.DrawUIGraphics(this, 6, contentX, contentY, focusWidth, GetLayoutVDim(6)))
            {
                // up arrow
                GameRuntime.DrawImageRes(GameRuntime.GetImageMapParam(151, ImageMap.Param.ORIGIN_X) + contentX + (focusWidth - GameRuntime.GetImageMapParam(151, 0) >> 1), GameRuntime.GetImageMapParam(151, ImageMap.Param.ORIGIN_Y) + contentY, 151);
            }
        }
        int b5 = focusHeight < d2 ? contentY + GetLayoutVDim(6) : contentY;
        if (focusStartY < d2 - focusHeight)
        {
            grp.SetClip(contentX, b5 + focusHeight, focusWidth, GetLayoutVDim(7));
            if (BounceGame.DrawUIGraphics(this, 7, contentX, b5 + focusHeight, focusWidth, GetLayoutVDim(7)))
            {
                // down arrow
                GameRuntime.DrawImageRes(GameRuntime.GetImageMapParam(150, ImageMap.Param.ORIGIN_X) + contentX + (focusWidth - GameRuntime.GetImageMapParam(150, 0) >> 1), b5 + focusHeight + 1 + GameRuntime.GetImageMapParam(150, ImageMap.Param.ORIGIN_Y), 150);
            }
        }
        int selectedIdx = selectedElemIdx;
        if (selectedIdx != -1 && elements[selectedIdx].GetAttribute(UIElement.SELECTION_ARROWS) == 2048)
        {
            UIElement selectedElem = elements[selectedIdx];
            int labelX = contentX + GetElemXAnchor(selectedElem, focusWidth);
            int labelY = GetElemStartY(selectedIdx) + b5 - focusStartY;
            int labelW = selectedElem.GetWidth();
            int labelH = selectedElem.GetHeight();
            grp.SetClip(labelX, labelY, labelW, labelH);
            if (BounceGame.DrawUIGraphics(this, 9, labelX, labelY, labelW, labelH))
            {
                grp.SetColor(selectedElem.GetAttribute(UIElement.SELECTION_BACKGROUND_COLOR));
                grp.FillRect(labelX, labelY, labelW, labelH);
                grp.SetColor(selectedElem.GetAttribute(UIElement.SELECTION_BORDER_COLOR));
                grp.DrawRect(labelX, labelY, labelW - 1, labelH - 1);
            }
        }
        for (titleImageX = 0; titleImageX < elements.Count; ++titleImageX)
        {
            UIElement elem = elements[titleImageX];
            int startY = GetElemStartY(titleImageX);
            if (startY > focusStartY + focusHeight) // element out of focus
                break;
            int elemHeight = elem.GetHeight();
            if (startY + elemHeight >= focusStartY)
            {
                int elemX = contentX + GetElemXAnchor(elem, focusWidth);
                int elemY = b5 + startY - focusStartY;
                int elemWidth = elem.GetWidth();
                grp.SetClip(contentX, b5, focusWidth, focusHeight);
                GameRuntime.SetChildClip(elemX, elemY, elemWidth, elemHeight, childClipTemp);
                if (BounceGame.DrawUIGraphics(this, 8, elemX, elemY, elemWidth, elemHeight))
                {
                    elem.Label?.Draw(
                        elemX + elem.GetAttribute(UIElement.MARGIN_LEFT),
                        elemY + elem.GetAttribute(UIElement.MARGIN_TOP),
                        elem.GetColor(titleImageX == selectedElemIdx),
                        elem.GetAttribute(UIElement.FONT_SHADOW_COLOR)
                    );
                }
            }
        }

        if (selectedIdx != -1 && elements[selectedIdx].GetAttribute(UIElement.SELECTION_ARROWS) == 2048)
        {
            UIElement elem = elements[selectedIdx];
            int x = contentX + GetElemXAnchor(elem, focusWidth);
            int y = b5 + GetElemStartY(selectedIdx) - focusStartY;
            grp.SetClip(x, y, elem.GetWidth(), elem.GetHeight());
            // selected item arrows
            BounceGame.DrawUIGraphics(this, 10, x, y, elem.GetWidth(), elem.GetHeight());
        }
    }

    public void LoadFromResource(int resId)
    {
        byte[] layoutData = GameRuntime.GetLoadedResData(resId);
        if (layoutData != null)
        {
            try
            {
                using MemoryStream stream = new(layoutData);
                using DataInputStream dis = new(stream);
                ElemDefaultAttributes = ReadAttributesFromStream(dis, ELEMENT_FLAG_MASKS, 15, 22);
                LayoutAttributes = ReadAttributesFromStream(dis, LAYOUT_FLAG_MASKS, 20, 26);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }

    private static int[] ReadAttributesFromStream(DataInputStream dis, int[] flagMasks, int regularAttrEnd, int colorAttrEnd)
    {
        int existingParamMask = dis.ReadInt();
        int flagCount = flagMasks.Length;
        if (existingParamMask == 0)
            return null;
        int clzIndex = flagCount;
        int paramCount = 0;
        while (existingParamMask >>> clzIndex - 1 != 0)
        {
            paramCount = clzIndex - flagCount;
            clzIndex++;
        }
        int[] result = new int[paramCount + 2];
        result[0] = existingParamMask;
        result[1] = 0;
        if ((0xFFFFFFFF >>> 32 - flagCount & existingParamMask) != 0)
            result[1] = dis.ReadInt();
        for (int paramIdx = 0; paramIdx < paramCount; paramIdx++)
        {
            if ((result[0] & 1 << paramIdx + flagCount) != 0)
                result[paramIdx + 2] = dis.ReadShort();
        }
        for (int colorIdx = regularAttrEnd + 2 - flagCount; colorIdx <= colorAttrEnd + 2 - flagCount && colorIdx < result.Length; colorIdx++)
        {
            int colorRgb555 = result[colorIdx];
            result[colorIdx] = (colorRgb555 & 0x7C00) << 9 | 0 | (colorRgb555 & 0x3E0) << 6 | (colorRgb555 & 31) << 3;
        }
        return result;
    }
}
