namespace BounceTales;

public sealed class UIElement
{
    // TODO: Convert attributes to an enum.
    public const int FONT_SHADOW_TYPE = 0;
    public const int TEXT_ALIGNMENT = 1;
    public const int ICON_ALIGNMENT = 2;
    public const int FLAG_3 = 3;
    public const int FLAG_4 = 4;
    public const int AUTO_WIDTH = 5;
    public const int ANCHOR_H = 6;
    public const int SELECTION_ARROWS = 7;
    public const int INHERIT_WIDTH = 8;
    public const int FONT = 9;
    public const int FIXED_WIDTH = 10;
    public const int MARGIN_TOP = 11;
    public const int MARGIN_BOTTOM = 12;
    public const int MARGIN_LEFT = 13;
    public const int MARGIN_RIGHT = 14;
    public const int FONT_TEXT_COLOR = 15;
    public const int FONT_SHADOW_COLOR = 16;
    public const int COLOR_17 = 17;
    public const int FONT_TEXT_COLOR_SELECTED = 18;
    public const int FONT_TEXT_COLOR_DISABLED = 19;
    public const int FONT_TEXT_COLOR_SELECTED_DISABLED = 20;
    public const int SELECTION_BACKGROUND_COLOR = 21;
    public const int SELECTION_BORDER_COLOR = 22;

    public const int AUTO_WIDTH_BIT = 256;
    public const int ANCHOR_H_BIT = 512;
    public const int SELECTION_ARROWS_BIT = 1024;
    public const int INHERIT_WIDTH_BIT = 2048;

    public UILayout ParentUI;
    public bool IsEnabled = true;

    public TextLabel Label;
    public GameScene Action = GameScene.INVALID;
    private int[] attributes;

    public UIElement()
    {
    }

    public UIElement(string str, int iconImageId, UILayout parentUI, GameScene action = GameScene.INVALID)
    {
        ParentUI = parentUI;
        Action = action;
        SetText(str, iconImageId);
    }

    public int GetAttribute(int aid)
    {
        return UILayout.AttributeExists(aid, attributes) || ParentUI == null
                ? UILayout.ReadAttribute(aid, attributes, 1)
                : UILayout.ReadAttribute(aid, ParentUI.ElemDefaultAttributes, 1);
    }

    public void SetAttribute(int aid, int value)
    {
        attributes = UILayout.WriteAttribute(aid, value, attributes, 1);
    }

    public int GetColor(bool selected)
    {
        return GetAttribute(
            IsEnabled ? selected ? FONT_TEXT_COLOR_SELECTED : FONT_TEXT_COLOR : selected ? FONT_TEXT_COLOR_SELECTED_DISABLED : FONT_TEXT_COLOR_DISABLED
        );
    }

    public void SetText(string str, int iconImageId)
    {
        string sanitizedStr = str ?? "";
        TextLabel lastLabel = Label; // ?????
        Label = null;
        Label = lastLabel;
        int availWidth = GetWidth() - (GetAttribute(MARGIN_LEFT) + GetAttribute(MARGIN_RIGHT));
        int flags = 0;
        for (int flagIdx = 0; flagIdx <= INHERIT_WIDTH; flagIdx++)
            flags |= GetAttribute(flagIdx);
        Label = new TextLabel(sanitizedStr, availWidth, GetAttribute(FONT), flags, iconImageId);
    }

    public bool HasAction()
    {
        return Action != GameScene.INVALID;
    }

    public int GetWidth()
    {
        if (GetAttribute(AUTO_WIDTH) == 256)
        {
            if (Label != null)
            {
                return (Label.rawText == null || Label.rawText.Length < 1
                        ? Label.TextBlockWidth - TextLabel.F0a
                        : Label.TextBlockWidth) + GetAttribute(MARGIN_LEFT) + GetAttribute(MARGIN_RIGHT);
            }
            if (ParentUI != null)
                return ParentUI.GetFocusWidth();
        }
        else if (GetAttribute(INHERIT_WIDTH) != 4096)
            return GetAttribute(FIXED_WIDTH);
        else if (ParentUI != null)
            return ParentUI.GetFocusWidth();
        return 0;
    }

    public int GetHeight()
    {
        return (Label != null ? Label.TextBlockHeight : 0) + GetAttribute(MARGIN_TOP) + GetAttribute(MARGIN_BOTTOM);
    }
}
