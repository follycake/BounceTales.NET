using System.Diagnostics;
using BounceTales.Ext.Rsc;
using BounceTales.Microedition.Lcdui;

namespace BounceTales;

public sealed class BounceGame
{
    public enum Controller
    {
        NORMAL = 0,
        CANNON = 1,
        DISABLED = 2,
        FROZEN = 3
    }

    public enum PlayerState
    {
        PLAY = 0,
        LOSE = 1,
        WIN = 2,
        LOSE_UPDATE = 3,
        WIN_UPDATE = 4
    }

    public static short[] SIN_COS_TABLE = new short[360];

    public const int CANNON_LEVEL_INDEX = (int)LevelID.LEVEL_IDX_MAX;

    private static readonly short[] LEVEL_RESIDS =
    [
        ResourceID.LEVELS_LEVEL_CAMPAIGN01_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN02_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN03_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN04_RLEF,
        ResourceID.LEVELS_LEVEL_EXTRA01_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN05_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN06_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN07_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN08_RLEF,
        ResourceID.LEVELS_LEVEL_EXTRA02_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN09_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN10_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN11_RLEF,
        ResourceID.LEVELS_LEVEL_CAMPAIGN12_RLEF,
        ResourceID.LEVELS_LEVEL_EXTRA03_RLEF,
        ResourceID.LEVELS_LEVEL_OBJ01_CANNON_RLEF
    ];

    private static readonly short[] LEVEL_NAME_MESSAGE_IDS =
    [
        MessageID.LEVEL_MISTY_MORNING,
        MessageID.LEVEL_UNFRIENDLY_FRIENDS,
        MessageID.LEVEL_SEEKING_ANSWERS,
        MessageID.LEVEL_BUMPY_CRACKS,
        MessageID.LEVEL_SECRET_STALKWAY,
        MessageID.LEVEL_INTO_THE_MINES,
        MessageID.LEVEL_A_GLOOMY_PATH,
        MessageID.LEVEL_RUMBLING_SOUNDS,
        MessageID.LEVEL_TRAPPED_IN_MACHINE,
        MessageID.LEVEL_TUNNEL_OF_TREASURES,
        MessageID.LEVEL_WICKED_CIRCUS,
        MessageID.LEVEL_HUNTING_COLOURS,
        MessageID.LEVEL_ALMOST_THERE,
        MessageID.LEVEL_FINAL_RIDE,
        MessageID.LEVEL_FANTASTIC_FAIR
    ];

    private static readonly short[] LEVEL_COVER_ART_IMAGE_IDS =
    [
        359,
        363,
        364,
        365,
        328,
        366,
        367,
        368,
        369,
        329,
        370,
        360,
        361,
        362,
        330
    ];

    private static readonly short[] LEVEL_EGG_TROPHY_REQUIREMENTS =
    [
        30, 29, 26,
        30, 28, 26,
        30, 25, 21,
        30, 25, 20,
        30, 30, 30,
        30, 28, 25,
        30, 28, 22,
        30, 27, 20,
        30, 27, 24,
        30, 30, 30,
        30, 29, 27,
        30, 28, 19,
        30, 25, 20,
        30, 25, 20,
        30, 30, 30
    ];

    private static readonly short[] LEVEL_TIMER_TROPHY_REQUIREMENTS =
    [
        30, 40, 50,
        36, 45, 55,
        35, 40, 48,
        32, 42, 50,
        9999, 9999, 9999,
        34, 45, 60,
        75, 85, 95,
        42, 46, 55,
        45, 55, 65,
        9999, 9999, 9999,
        45, 50, 58,
        60, 70, 80,
        48, 54, 60,
        24, 34, 44,
        9999, 9999, 9999
    ];

    private static readonly float[] EGG_SCORE_MULTIPLIER_BY_LEVEL =
    [
        0.937f,
        0.969f,
        0.659f,
        0.937f,
        1.412f,
        0.969f,
        2.121f,
        1.298f,
        1.298f,
        1.011f,
        1.298f,
        1.921f,
        1.298f,
        0.712f,
        1.195f
    ];

    private static readonly int[] BONUS_LEVEL_INFO =
    [
        (int)LevelID.SECRET_STALKWAY, 60,
        (int)LevelID.TUNNEL_OF_TREASURES, 200,
        (int)LevelID.FANTASTIC_FAIR, 400
    ];

    private static readonly LevelID[] FORME_UNLOCK_LEVELS = [LevelID.BUMPY_CRACKS, LevelID.TRAPPED_IN_MACHINE];
    private static readonly short[] NUMBER_FONT_IMAGE_IDS = [90, 91, 92, 93, 94, 95, 96, 97, 98, 99];

    private const int PARALLAX_MAX_COUNT = 5;

    private static readonly short[] ALL_PARALLAX_IMAGE_IDS = [388, 373, 145, 313, 265, 157, 174, 55, 345, 243, 78, 317, 176, 267];
    private static readonly short[] f307i = [388];
    private static readonly short[] f311j = [251];
    private static readonly short[] f314k = [252];
    private static readonly short[] f318l = [];
    private static readonly short[] f322m = [373];
    private static readonly short[] f325n = [161];
    private static readonly short[] f328o = [159, 160];
    private static readonly short[] f331p = [];
    private static readonly short[] f334q = [352];
    private static readonly short[] f337r = [113];
    private static readonly short[] f340s = [114];
    private static readonly short[] f343t = [353];

    private static readonly int[] WIN_PARTICLE_IMAGE_IDS = [420, 426, 402, 408];
    private static readonly int[] SPLASH_PARTICLE_IMAGE_IDS = [526, 516, 531, 521];
    private static readonly int[] BUBBLE_PARTICLE_IMAGE_IDS = [536];
    private static readonly int[] COLOR_MACHINE_DESTROY_PARTICLE_IMAGE_IDS = [390, 414, 396];
    private static readonly int[] SUPER_BOUNCE_PARTICLE_IMAGE_IDS = [420, 426];
    private static readonly int[] CANNON_PARTICLE_IMAGE_IDS = [390, 420];
    private static readonly int[] EGG_COLLECT_PARTICLE_IMAGE_IDS = [420, 426];
    private static readonly int[] ENEMY_DEATH_PARTICLE_IMAGE_IDS = [414, 396, 420];
    private static readonly int[] AIR_PARTICLE_IMAGE_IDS = [437, 432];

    private static readonly short[] TROPHY_IMAGE_IDS = [314, 315, 389];
    public static readonly short[] SCRIPT_MESSAGE_IDS = MessageID.SCRIPT_MESSAGE_MAP;

    private static readonly int[] SPLASH_SCREEN_LAYOUT_RESIDS = [ResourceID.GRAPHICS_SPLASHLOGO_RES];
    private static readonly int[] SPLASH_SCREEN_DURATIONS = [3000]; // Original: 3000
    private static readonly int[] SPLASH_BG_COLORS = [0xFFFFFF];
    private static readonly int[] SPLASH_IMAGE_IDS = [1];

    private const int LAYOUT_MAIN_MENU_TITLE_PADDING = 129;
    private const int LAYOUT_DEFAULT_TITLE_PADDING_TOP = 40;
    private const int LAYOUT_DEFAULT_TITLE_PADDING_BOTTOM = 5;
    private const int LAYOUT_DEFAULT_VERTICAL_MARGIN = 56;
    private const int LAYOUT_DEFAULT_HORIZONTAL_MARGIN = 20;
    private const int LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME = 40;

    // State - root
    public static BounceRandom RNG = new();
    
    private static short[] levelSaveData = new short[60];
    public static bool IsSuperBounceUnlocked;

    private static int totalGameTime;
    private int gameMainState = 1;

    private static bool reqQuit; // added in 2.0.25 for more game URL action
    private static bool reqPlayTitleMusic;

    private static int renderClipWidth = GameRuntime.CurrentWidth;
    private static int renderClipHeight = GameRuntime.CurrentHeight;

    // State - text
    private static readonly bool isTextRightToLeft = StringManager.GetMessage(MessageID.IS_TEXT_RIGHT_TO_LEFT_RESERVED).Equals("1");

    // State - layout core
    private readonly UILayout ui = new();
    private UILayout drawUI;

    // State - loading
    private int curSplashId;
    private long splashScreenStartTime;

    private static bool hasLoadingProgressBar;
    private int loadingProgressBar;

    // State - menus
    private static GameScene exitLevelReturnScene = GameScene.MENU_TITLE;
    private static int lastMenuOption;

    private static LevelID selectedLevelId = 0;
    private static LevelID lastSelectedLevelId = 0;

    private static int bookAnimationTime;
    private static int targetBookAnimationTime;

    // State - softkey bar polygon coordinates
    //private static readonly int[] xluSoftkeyBarXs = new int[4];
    //private static readonly int[] xluSoftkeyBarYs = new int[4];

    // State - framebuffers
    public static Graphics BallGraphics;
    public static Image BallFramebuffer;
    public static Color32[] BallFramebufferRGB;

    public static Graphics SpriteOffscreenGraphics;
    public static Image SpriteFB;
    public static Color32[] SpriteFBRGB;

    // State - level
    private bool isLevelActive;
    private static bool isBlockingEvent;

    public static bool ReqCameraSnap;
    public static bool LevelPaused;
    public static LevelID CurrentLevel;

    private static int objectCount;
    private static int eventCount;

    private static EventObject[] events;
    private static GameObject[] levelObjects;
    public static GameObject[] CannonModels;

    public static GameObject RootLevelObj;

    public static BounceObject BounceObj;
    public static CannonObject CurrentCannon;

    private static int bonusLevelEggLimit;

    // State - level progress
    public static int LevelTimer;
    public static int EggCount;

    public static int CheckpointPosX;
    public static int CheckpointPosY;

    public static bool WaterSingletonFlag;

    // State - stolen colors
    private static bool isFlashToOtherColorMode;
    private static bool isColorsAreStolen;
    private static int stolenColorsAnimationCountdown;
    private static int stolenColorsFlashCountdown;

    // Particles
    public static readonly ParticleObject WinParticle = new(20, 0, 0, 0, 0, 35, ParticleObject.Type.SHOWER, WIN_PARTICLE_IMAGE_IDS, 1840, -4);
    public static readonly ParticleObject WaterSplashParticle = new(20, 0, -200, 0, 0, 0, ParticleObject.Type.SPRITE_4DIR, SPLASH_PARTICLE_IMAGE_IDS, 800, 7);
    public static readonly ParticleObject BubbleParticle = new(150, 0, 80, 0, 0, 0, ParticleObject.Type.BUBBLES, BUBBLE_PARTICLE_IMAGE_IDS, 4000, 7);
    public static readonly ParticleObject SuperBounceParticle = new(10, 0, 0, 0, 0, 0, ParticleObject.Type.TRAIL, SUPER_BOUNCE_PARTICLE_IMAGE_IDS, 1000, -5);
    public static readonly ParticleObject CannonParticle = new(10, 0, 0, 0, 0, 30, ParticleObject.Type.SPRITE_RANDOM, CANNON_PARTICLE_IMAGE_IDS, 800, -1);
    public static readonly ParticleObject EggCollectParticle = new(50, 0, 0, 0, 0, 85, ParticleObject.Type.SPRITE_BY_PARTICLE_NO, EGG_COLLECT_PARTICLE_IMAGE_IDS, 540, -2);
    public static readonly ParticleObject EnemyDeathParticle = new(50, 0, 0, 0, 0, 35, ParticleObject.Type.SHOWER, ENEMY_DEATH_PARTICLE_IMAGE_IDS, 1840, -3);
    public static readonly ParticleObject ColorMachineDestroyParticle = new(24, 0, 0, 0, 0, 35, ParticleObject.Type.SHOWER, COLOR_MACHINE_DESTROY_PARTICLE_IMAGE_IDS, 2040, -6);
    public static readonly ParticleObject AirTunnelParticle = new(150, 0, 0, 0, 0, 0, ParticleObject.Type.SPRITE_2DIR, AIR_PARTICLE_IMAGE_IDS, 2000, 15);

    // Extra entities
    public static EggObject EnemyDeadEgg;

    // State - level exit
    public static int ExitWaitTimer;
    public static int DeathBaseY;

    // State - field message
    private static bool isFieldMessageShowing;
    private static string lastFieldMsg;

    private static readonly int[] fieldMessageQueue = new int[5];
    private static int fieldMessagePointer;
    private static string[] fieldMessageParam;

    private bool reqQuitLevelAfterFieldMessage;
    private static bool reqReloadFieldMsg;

    // State - parallax
    private static Image[] parallaxImagesRegColors;
    private static Image[] parallaxImagesStolenColors;

    private static int f240F;

    // array size bugfixed for HD parallaxes
    // another slight optimization: originally the size allocated was 10 (double the parallax count)
    // instead of 6 (parallax count + 1), which was just enough. reducing it changes nothing and descreases memory footprint.
    private static int[] parallaxXOffsets = new int[PARALLAX_MAX_COUNT * ((renderClipWidth + 239) / 240) * ((renderClipHeight + 319) / 320) + 1];
    private static int[] parallaxYOffsets = new int[parallaxXOffsets.Length];
    private static int[] parallaxImageIndices = new int[parallaxYOffsets.Length];

    // State - after level cleared
    private static int calcScore;

    private int timerChallengeTrophy = -1;
    private int collectionChallengeTrophy = -1;

    private bool wasFinalLevelJustBeaten;
    private bool wasSuperBounceJustUnlocked;
    private bool highScoreBeaten;

    static BounceGame()
    {
        GenerateSinCosTable();
    }

    private static void GenerateSinCosTable()
    {
        int curve = 0;
        int tangent = 57 * 360;
        for (int angle = 0; angle < 360; angle++)
        {
            int sin = curve / 57;
            SIN_COS_TABLE[angle] = (short)sin;
            tangent -= sin;
            curve += tangent / 57;
        }
    }

    private static void SetBGColor(int rgb, Graphics graphics)
    {
        if (isColorsAreStolen && !isFlashToOtherColorMode || !isColorsAreStolen && isFlashToOtherColorMode)
        {
            int red = rgb >> 16 & 255;
            int green = rgb >> 8 & 255;
            int blue = rgb & 255;
            rgb = (green + blue >> 1 << 16) + (blue + red >> 1 << 8) + (red + green >> 1);
        }
        graphics.SetColor(rgb);
    }

    private static int DrawStylizedNumber(int x, int y, int value, Graphics.Anchor anchor, bool allowSingleDigit)
    {
        int newDrawnWidth;
        bool isSingleDigit = value < 10 && allowSingleDigit;
        int drawnWidth = 0;
        int digitIndex = anchor == Graphics.Anchor.LEFT ? 0 : 1; // EXTREMELY hackily coded, only works for 2 digit integers cause it skips the 1st digit for left alignment
        int remainder = value;
        int xOffset = x;
        while (digitIndex < 2)
        {
            if (remainder == 0)
            {
                if (digitIndex == 1)
                    GameRuntime.DrawImageResAnchored(xOffset, y, NUMBER_FONT_IMAGE_IDS[0], Graphics.Anchor.TOP | Graphics.Anchor.RIGHT);
                newDrawnWidth = drawnWidth + GameRuntime.GetImageMapParam(NUMBER_FONT_IMAGE_IDS[0], ImageMap.Param.WIDTH);
            }
            else
            {
                newDrawnWidth = drawnWidth;
                while (remainder != 0)
                {
                    short imageId = NUMBER_FONT_IMAGE_IDS[remainder % 10];
                    if (digitIndex == 1)
                        GameRuntime.DrawImageResAnchored(xOffset - newDrawnWidth, y, imageId, Graphics.Anchor.TOP | Graphics.Anchor.RIGHT);
                    remainder /= 10;
                    newDrawnWidth += GameRuntime.GetImageMapParam(imageId, ImageMap.Param.WIDTH);
                }
            }
            if (digitIndex == 0)
            {
                xOffset += newDrawnWidth;
                newDrawnWidth = 0;
                remainder = value;
            }
            digitIndex++;
            drawnWidth = newDrawnWidth;
        }
        if (!isSingleDigit)
            return drawnWidth;
        GameRuntime.DrawImageResAnchored(xOffset - drawnWidth, y, NUMBER_FONT_IMAGE_IDS[0], Graphics.Anchor.TOP | Graphics.Anchor.RIGHT); // leading zero
        return GameRuntime.GetImageMapParam(NUMBER_FONT_IMAGE_IDS[0], ImageMap.Param.WIDTH) + drawnWidth;
    }

    private static void DrawLevelSelectUI(int x, int y, LevelID levelId, int bottomY, int i5)
    {
        int b = GetLevelType(levelId);
        if (b == 0)
            GameRuntime.DrawImageRes(x, y, 8);
        else if (b == 2)
            GameRuntime.DrawImageRes(x, y, 380);
        GameRuntime.DrawImageRes(x, y, LEVEL_COVER_ART_IMAGE_IDS[(int)levelId]);
        string[] printfParams = new string[1];
        GameRuntime.SetTextStyle(-3, 1);
        GameRuntime.SetTextColor(0, 0);
        if (!IsLevelUnlocked(levelId))
        {
            // Level not unlocked
            GameRuntime.DrawImageRes(x, y, 149);
            if (IsBonusLevel(levelId))
            {
                //Required eggs for unlock
                int bonusLevelRequirement = 0;
                for (int i = 0; i < BONUS_LEVEL_INFO.Length; i += 2)
                {
                    if (levelId == (LevelID)BONUS_LEVEL_INFO[i])
                        bonusLevelRequirement = BONUS_LEVEL_INFO[i + 1];
                }
                string unlockRequirementMsg = StringManager.GetMessage(MessageID.NEED_COLLECT_COUNT, bonusLevelRequirement);
                int a2 = GameRuntime.GetStrRenderWidth(-3, unlockRequirementMsg, 0, unlockRequirementMsg.Length) + 23 + 5;
                int i8 = (GameRuntime.CurrentWidth >> 1) - (a2 >> 1);
                int i9 = a2 + i8;
                if (isTextRightToLeft)
                {
                    GameRuntime.DrawImageResAnchored(i9, i5, 102, Graphics.Anchor.RIGHT | Graphics.Anchor.TOP);
                    GameRuntime.DrawText(unlockRequirementMsg, 0, unlockRequirementMsg.Length, i8, i5, 20);
                }
                else
                {
                    GameRuntime.DrawImageResAnchored(i8, i5, 102, Graphics.Anchor.LEFT | Graphics.Anchor.TOP);
                    GameRuntime.DrawText(unlockRequirementMsg, 0, unlockRequirementMsg.Length, i8 + 23 + 5, i5, 20);
                }
            }
        }
        else
        {
            // Level stats
            int a3 = GameRuntime.GetFontHeight(-3) + 1;
            int a4 = GameRuntime.GetFontHeight(-3) + 23 + 3;
            string a5 = StringManager.GetMessage(MessageID.SCORE, "9999");
            int a6 = GameRuntime.GetStrRenderWidth(-3, a5, 0, a5.Length);
            int myScore = GetLevelLocalHighScore(levelId);
            if (myScore <= 0)
                myScore = 0;
            int i11 = (GameRuntime.CurrentWidth >> 1) - (a6 >> 1) - 6;
            int a7 = GameRuntime.GetStrRenderWidth(-3, "00/00", 0, "00/00".Length) + 23 + 11;
            string a8 = StringManager.GetMessage(MessageID.SCORE, myScore);
            if (isTextRightToLeft)
                GameRuntime.DrawText(a8, 0, a8.Length, i11 + a7 + 23, i5, 24);
            else
                GameRuntime.DrawText(a8, 0, a8.Length, i11, i5, 20);
            if (!IsBonusLevel(levelId))
            {
                printfParams[0] = GetLevelEggCount(levelId) + "/30";
                string str = printfParams[0];
                if (isTextRightToLeft)
                {
                    GameRuntime.DrawImageResAnchored(i11 + a7, i5 + a3, 102, Graphics.Anchor.LEFT | Graphics.Anchor.TOP);
                    GameRuntime.DrawText(str, 0, str.Length, i11 - 11 + a7, i5 + a3 + 11, 10);
                }
                else
                {
                    GameRuntime.DrawImageResAnchored(i11, i5 + a3, 102, Graphics.Anchor.LEFT | Graphics.Anchor.TOP);
                    GameRuntime.DrawText(str, 0, str.Length, i11 + 23 + 11, i5 + a3 + 11, 6);
                }
            }
            if (WasLevelBeaten(LevelID.GAME_CLEAR_LEVEL) && !IsBonusLevel(levelId))
            {
                int d = GetCollectionChallengeRank(levelId);
                int i12 = d > -1 ? TROPHY_IMAGE_IDS[d] : d;
                int e = GetTimerChallengeRank(levelId);
                int i13 = e > -1 ? TROPHY_IMAGE_IDS[e] : e;
                string timer = FormatGameTimer(GetLevelClearTime(levelId));
                if (isTextRightToLeft)
                {
                    GameRuntime.DrawImageResAnchored(i11 + a7, i5 + a4, 103, Graphics.Anchor.LEFT | Graphics.Anchor.TOP);
                    GameRuntime.DrawText(timer, 0, timer.Length, i11 - 11 + a7, i5 + a4 + 11, 10);
                }
                else
                {
                    GameRuntime.DrawImageResAnchored(i11, i5 + a4, 103, Graphics.Anchor.LEFT | Graphics.Anchor.TOP);
                    GameRuntime.DrawText(timer, 0, timer.Length, i11 + 23 + 11, i5 + a4 + 11, 6);
                }
                int a9 = GameRuntime.GetStrRenderWidth(-3, "00:00", 0, "00:00".Length) + 23 + 11;
                if (isTextRightToLeft)
                {
                    if (i12 > -1)
                        GameRuntime.DrawImageResAnchored(i11, i5 + a3, i12, Graphics.Anchor.RIGHT | Graphics.Anchor.TOP);
                    if (i13 > -1)
                        GameRuntime.DrawImageResAnchored(i11, i5 + a4, i13, Graphics.Anchor.LEFT | Graphics.Anchor.RIGHT);
                }
                else
                {
                    if (i12 > -1)
                        GameRuntime.DrawImageResAnchored(i11 + a9 + 11, i5 + a3, i12, Graphics.Anchor.LEFT | Graphics.Anchor.TOP);
                    if (i13 > -1)
                        GameRuntime.DrawImageResAnchored(a9 + i11 + 11, i5 + a4, i13, Graphics.Anchor.LEFT | Graphics.Anchor.TOP);
                }
            }
        }
        GameRuntime.SetTextStyle(-2, 3);
        GameRuntime.SetTextColor(0, 0xFF7800);
        GameRuntime.SetTextColor(1, 0);
        if (IsBonusLevel(levelId))
        {
            string numStr = StringManager.GetMessage(MessageID.UI_CHAPTERNO_BONUS, GetLevelChapterNumber(levelId));
            GameRuntime.DrawText(numStr, 0, numStr.Length, x, bottomY - GameRuntime.GetFontHeight(GameRuntime.GetCurrentFont()), 33);
        }
        else
        {
            string numStr = StringManager.GetMessage(MessageID.UI_CHAPTERNO_STD, GetLevelChapterNumber(levelId));
            GameRuntime.DrawText(numStr, 0, numStr.Length, x, bottomY - GameRuntime.GetFontHeight(GameRuntime.GetCurrentFont()), 33);
        }
        string levelName = StringManager.GetMessage(LEVEL_NAME_MESSAGE_IDS[(int)levelId]);
        GameRuntime.DrawText(levelName, 0, levelName.Length, x, bottomY, (int)(Graphics.Anchor.BOTTOM | Graphics.Anchor.HCENTER));
    }

    private static void DrawBookFrame(int xpos, int ypos, Graphics graphics)
    {
        GameRuntime.DrawImageRes(xpos, ypos, 331);
        int yparam = GameRuntime.GetCompoundSpriteParamEx(331, 0);
        int xEnd = yparam >> 16;
        short yEnd = (short)(yparam & 0xFFFF);
        int xparam = GameRuntime.GetCompoundSpriteParamEx(331, 1);
        int xStart = xparam >> 16;
        short yStart = (short)(xparam & 0xFFFF);
        graphics.SetColor(0xFBF7E3);
        graphics.FillRect(xpos + xStart, ypos + yStart, xEnd - xStart, yEnd - yStart);
        graphics.SetColor(0);
        graphics.FillRect(xpos + xStart - 2, ypos + yStart, 4, yEnd - yStart);
    }

    private static string GetLevelChapterNumber(LevelID levelId)
    {
        int countOfBonusChaptersBefore = 0;
        int bonusStructIdx = 0;
        while (bonusStructIdx < BONUS_LEVEL_INFO.Length && levelId >= (LevelID)BONUS_LEVEL_INFO[bonusStructIdx])
        {
            countOfBonusChaptersBefore++;
            bonusStructIdx += 2;
        }
        return IsBonusLevel(levelId) ? countOfBonusChaptersBefore.ToString() : ((int)levelId + 1 - countOfBonusChaptersBefore).ToString();
    }

    private static bool IsBonusLevel(LevelID i)
    {
        for (int i2 = 0; i2 < BONUS_LEVEL_INFO.Length; i2 += 2)
        {
            if (i == (LevelID)BONUS_LEVEL_INFO[i2])
                return true;
        }
        return false;
    }

    private static int GetLevelMusicID()
    {
        switch (CurrentLevel)
        {
            case LevelID.SECRET_STALKWAY:
            case LevelID.TUNNEL_OF_TREASURES:
            case LevelID.FANTASTIC_FAIR:
                return ResourceID.AUDIO_BGM_LEVEL_BONUS_MID;
            case LevelID.BUMPY_CRACKS:
            case LevelID.TRAPPED_IN_MACHINE:
            case LevelID.FINAL_RIDE:
                return ResourceID.AUDIO_BGM_LEVEL_BOSS_MID;
        }
        switch (GetLevelType(CurrentLevel))
        {
            case 0:
                return ResourceID.AUDIO_BGM_LEVEL_ACT01_MID;
            case 1:
                return ResourceID.AUDIO_BGM_LEVEL_ACT02_MID;
            default:
                return ResourceID.AUDIO_BGM_LEVEL_ACT03_MID;
        }
    }

    public static void PushFieldMessage(int msgId)
    {
        if (fieldMessagePointer < 5)
        {
            fieldMessageQueue[fieldMessagePointer] = msgId;
            fieldMessagePointer++;
        }
    }

    private void PopFieldMessage()
    {
        if (!isFieldMessageShowing && fieldMessagePointer > 0)
        {
            SetUI((GameScene)34);
            isBlockingEvent = true;
            isFieldMessageShowing = true;
            for (int i = 0; i < 4; i++)
                fieldMessageQueue[i] = fieldMessageQueue[i + 1];
            fieldMessagePointer--;
        }
    }
    
    public static PlayerState CurrentPlayerState
    {
        get => (PlayerState)EventObject.EventVars[0];
        set => EventObject.EventVars[0] = (int)value;
    }

    public static Controller CurrentControllerState
    {
        get => (Controller)EventObject.EventVars[1];
        set => EventObject.EventVars[1] = (int)value;
    }

    private static void UpdateLevelStats(LevelID levelId, short eggCount, short clearTime, short score)
    {
        int saveDataOffset = (int)levelId << 2;
        if (eggCount > levelSaveData[saveDataOffset])
            levelSaveData[saveDataOffset] = eggCount;
        if (clearTime < levelSaveData[saveDataOffset + 1])
            levelSaveData[saveDataOffset + 1] = clearTime;
        if (score > levelSaveData[saveDataOffset + 2])
            levelSaveData[saveDataOffset + 2] = score;
        if (score > levelSaveData[saveDataOffset + 3])
            levelSaveData[saveDataOffset + 3] = score;
    }

    private static void DrawTranslucentSoftkeyBar(Graphics graphics)
    {
        int skbHeight = GameRuntime.GetSoftkeyBarHeight();
        int width = GameRuntime.CurrentWidth;
        int height = GameRuntime.CurrentHeight - skbHeight;
        /*xluSoftkeyBarXs[0] = 0;
        xluSoftkeyBarYs[0] = height;
        xluSoftkeyBarXs[1] = 0 + width;
        xluSoftkeyBarYs[1] = height;
        xluSoftkeyBarXs[2] = 0 + width;
        xluSoftkeyBarYs[2] = height + skbHeight;
        xluSoftkeyBarXs[3] = 0;
        xluSoftkeyBarYs[3] = skbHeight + height;
        directGraphics.FillPolygon(xluSoftkeyBarXs, xluSoftkeyBarYs, 4, 0x55000000);*/
        graphics.FillRect(0, height, width, skbHeight, new Color32(0, 0, 0, 0x55));
    }

    public static void DrawSoftkeyUI(string str, int type, int xpos, int ypos, int flags)
    {
        if (type == 1)
            GameRuntime.DrawImageResAnchored(xpos, ypos, ((Graphics.Anchor)flags | Graphics.Anchor.TOP) == Graphics.Anchor.TOP ? 151 : 150, (Graphics.Anchor)flags);
        else
        {
            GameRuntime.SetTextStyle(-3, 3);
            GameRuntime.SetTextColor(0, 0xFF7800);
            GameRuntime.SetTextColor(1, 0);
            GameRuntime.DrawText(str, 0, str.Length, xpos, ypos, flags);
        }
    }

    private static void UpdateLevelStartSoftkeyByUnlock(UILayout ui)
    {
        if (IsLevelUnlocked(selectedLevelId))
            ui.ChangeSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_SELECT), 0);
        else
            ui.ChangeSoftkey(GameRuntime.Softkey.CENTER, null, 0);
    }

    private static void CycleLevelSelectLeft(UILayout layout, bool updateSoftkeys)
    {
        if (selectedLevelId != 0)
        {
            bool fast = lastSelectedLevelId > selectedLevelId;
            lastSelectedLevelId = selectedLevelId;
            selectedLevelId--;
            if (bookAnimationTime == 650 || fast)
                bookAnimationTime = 0;
            targetBookAnimationTime = 650;
            if (updateSoftkeys)
                UpdateLevelStartSoftkeyByUnlock(layout);
        }
    }

    private static void CycleLevelSelectRight(UILayout layout, bool updateSoftkeys)
    {
        if (selectedLevelId != (LevelID)14)
        {
            bool fast = lastSelectedLevelId < selectedLevelId;
            lastSelectedLevelId = selectedLevelId;
            selectedLevelId++;
            if (bookAnimationTime == 0 || fast)
                bookAnimationTime = 650;
            targetBookAnimationTime = 0;
            if (updateSoftkeys)
                UpdateLevelStartSoftkeyByUnlock(layout);
        }
    }

    private static void ClearUIBackground(Graphics graphics)
    {
        graphics.SetColor(0x3F1A01);
        graphics.FillRect(0, 0, GameRuntime.CurrentWidth >> 1, GameRuntime.CurrentHeight);
        graphics.SetColor(0x5E2601);
        graphics.FillRect(GameRuntime.CurrentWidth >> 1, 0, GameRuntime.CurrentWidth >> 1, GameRuntime.CurrentHeight);
        GameRuntime.DrawImageRes(0, 0, 4);
        GameRuntime.DrawImageRes(0, 0, 74);
        GameRuntime.DrawImageRes(0, 0, 75);
        GameRuntime.DrawImageRes(0, GameRuntime.CurrentHeight, 3);
    }

    private static int[] ArraysCopyOf(int[] src, int newSize)
    {
        int[] newArr = new int[newSize];
        src.CopyTo(newArr, 0);
        return newArr;
    }

    // For high resolution parallaxes.
    private static void CheckReallocParallax(int maxAllocSize)
    {
        if (parallaxImageIndices.Length < maxAllocSize)
        {
            parallaxImageIndices = ArraysCopyOf(parallaxImageIndices, maxAllocSize);
            parallaxXOffsets = ArraysCopyOf(parallaxXOffsets, maxAllocSize);
            parallaxYOffsets = ArraysCopyOf(parallaxYOffsets, maxAllocSize);
        }
    }

    private static void DrawBGParallax(short[] imageIDs, int moveSpeedNum, int moveSpeedDenom, int x, int xRange, int y, int yRange, int count, int stripeFillColor, Graphics graphics)
    {
        parallaxXOffsets[0] = 0;
        int firstYOffset = 0;
        if (yRange != 0)
            firstYOffset = RNG.NextInt() % yRange;
        parallaxYOffsets[0] = firstYOffset + y;
        for (int i = 1; i < count + 1; i++)
        {
            int stepFromLast = 0;
            if (xRange != 0)
                stepFromLast = Math.Abs(RNG.NextInt() % xRange);
            parallaxXOffsets[i] = stepFromLast + parallaxXOffsets[i - 1] + x;
            int yOffset = 0;
            if (yRange != 0)
                yOffset = RNG.NextInt() % yRange;
            parallaxYOffsets[i] = yOffset + y;
            parallaxImageIndices[i] = Math.Abs(RNG.NextInt() % imageIDs.Length);
        }
        int parallaxGroupWidth = parallaxXOffsets[count];
        int baseX = parallaxGroupWidth - (((GameObject.CameraMatrix.TranslationX >> 16) + 33000) * GameObject.ScreenSpaceMatrix.M00 >> 16) * moveSpeedNum / moveSpeedDenom % parallaxGroupWidth;
        int baseY = GameRuntime.CurrentHeight + ((GameObject.CameraMatrix.TranslationY - f240F >> 16) * GameObject.ScreenSpaceMatrix.M00 >> 16) * moveSpeedNum / moveSpeedDenom;
        for (int i = 0; i < count; i++)
        {
            if (i < 2)
                GameRuntime.DrawImageRes(parallaxXOffsets[i] + baseX, parallaxYOffsets[i] + baseY, imageIDs[parallaxImageIndices[i]]);
            if (i > 2)
                GameRuntime.DrawImageRes(parallaxXOffsets[i] + baseX - (parallaxGroupWidth << 1), parallaxYOffsets[i] + baseY, imageIDs[parallaxImageIndices[i]]);
            GameRuntime.DrawImageRes(parallaxXOffsets[i] + baseX - parallaxGroupWidth, parallaxYOffsets[i] + baseY, imageIDs[parallaxImageIndices[i]]);
        }
        if (stripeFillColor != -1)
        {
            SetBGColor(stripeFillColor, graphics);
            graphics.FillRect(0, parallaxYOffsets[0] + baseY, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight - (baseY + parallaxYOffsets[0]));
        }
    }

    public static bool DrawUIGraphics(UILayout ui, int type, int xpos, int ypos, int width, int height)
    {
        GameRuntime.SetBacklight(true);
        Graphics grp = GameRuntime.GetGraphicsObj();
        int delta = GameRuntime.UpdateDelta * GameRuntime.GetUpdatesPerDraw();
        if ((ui.UIID == GameScene.INFO_FIELD_MESSAGE
             || ui.UIID == GameScene.MENU_PAUSE
             || ui.UIID == GameScene.CONFIRM_RESTART_LEVEL
             || ui.UIID == GameScene.CONFIRM_RETURN_LEVEL_SELECT
             || ui.UIID == GameScene.CONFIRM_EXIT_LEVEL) && type == 1)
        {
            // Pause menu background
            /*int[] xpoints = GeometryObject.TEMP_QUAD_XS;
            int[] ypoints = GeometryObject.TEMP_QUAD_YS;
            xpoints[0] = xpos;
            ypoints[0] = ypos;
            xpoints[1] = xpos + width;
            ypoints[1] = ypos;
            xpoints[2] = xpos + width;
            ypoints[2] = ypos + height;
            xpoints[3] = xpos;
            ypoints[3] = ypos + height;
            directGraphics.FillPolygon(xpoints, ypoints, 4, 0x55000000);*/
            grp.FillRect(xpos, ypos, width, height, new Color32(0, 0, 0, 0x55));
            GameRuntime.DrawImageRes(xpos, ypos, 311);
            GameRuntime.DrawImageRes(xpos + width, ypos, 312);
            GameRuntime.DrawImageRes(xpos, ypos + height, 309);
            GameRuntime.DrawImageRes(xpos + width, ypos + height, 310);
            DrawTranslucentSoftkeyBar(grp);
            return false;
        }
        if (ui.UIID == GameScene.MENU_LEVEL_SELECT)
        {
            if (type == 1)
            {
                ClearUIBackground(grp);
                int screenCX = GameRuntime.CurrentWidth >> 1;
                int screenCY = GameRuntime.CurrentHeight >> 1;
                int b = (short)GameRuntime.GetCompoundSpriteParamEx(331, 2) + screenCY;
                int b2 = (short)GameRuntime.GetCompoundSpriteParamEx(331, 3) + screenCY;

                // since 2.0.25
                GameRuntime.SetTextStyle(-3, 3);
                int sanityHeight = 10 + GameRuntime.GetFontHeight(GameRuntime.GetCurrentFont()) * 3;
                if (sanityHeight > b)
                    b = sanityHeight + 2;

                DrawBookFrame(screenCX, screenCY, grp);
                if (selectedLevelId != 0)
                    GameRuntime.DrawImageResAnchored(3, screenCY, 326, Graphics.Anchor.VCENTER | Graphics.Anchor.LEFT); // left arrow
                if (selectedLevelId != LevelID.LEVEL_IDX_MAX - 1)
                    GameRuntime.DrawImageResAnchored(GameRuntime.CurrentWidth - 3, screenCY, 2, Graphics.Anchor.VCENTER | Graphics.Anchor.RIGHT); // right arrow
                LevelID topPageLevel = 0;
                LevelID bottomPageLevel = 0;
                if (bookAnimationTime < targetBookAnimationTime)
                {
                    bookAnimationTime += delta;
                    if (bookAnimationTime > targetBookAnimationTime)
                        bookAnimationTime = targetBookAnimationTime;
                    topPageLevel = selectedLevelId;
                    bottomPageLevel = lastSelectedLevelId;
                }
                else if (bookAnimationTime > targetBookAnimationTime)
                {
                    bookAnimationTime -= delta;
                    if (bookAnimationTime < targetBookAnimationTime)
                        bookAnimationTime = targetBookAnimationTime;
                    topPageLevel = lastSelectedLevelId;
                    bottomPageLevel = selectedLevelId;
                }
                if (bookAnimationTime > 400 && bookAnimationTime < 650)
                {
                    // grab page end
                    DrawLevelSelectUI(screenCX, screenCY, topPageLevel, b, b2);
                    GameRuntime.DrawAnimatedImageRes(screenCX, screenCY, 442, (bookAnimationTime - 400 << 1) / 250);
                }
                else if (bookAnimationTime > 400 || bookAnimationTime <= 0)
                {
                    // idle
                    DrawLevelSelectUI(screenCX, screenCY, selectedLevelId, b, b2);
                }
                else
                {
                    int i12 = screenCX - 119 - 239 + 22;
                    int i13 = screenCX + 120 - 30;
                    int i14 = screenCX - 119 + 22;
                    int pageSplitXStart = i12 + (i13 - 25 - i12) * bookAnimationTime / 400;
                    int pageSplitXEnd = (i13 - i14) * bookAnimationTime / 400 + i14;
                    int i17 = screenCY - 158 - 3;
                    grp.SetClip(0, 0, pageSplitXStart + 3, GameRuntime.CurrentHeight);
                    DrawLevelSelectUI(screenCX, screenCY, topPageLevel, b, b2);
                    grp.SetClip(pageSplitXEnd - 2, 0, GameRuntime.CurrentWidth - pageSplitXEnd + 2, GameRuntime.CurrentHeight);
                    DrawLevelSelectUI(screenCX, screenCY, bottomPageLevel, b, b2);
                    grp.SetClip(0, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
                    GameRuntime.DrawImageRes(pageSplitXStart, i17, 377);
                    grp.SetColor(0xEAE6CC);
                    int i18 = pageSplitXEnd - pageSplitXStart - 12 - 13;
                    grp.FillRect(pageSplitXStart + 12, i17, i18, 295);
                    grp.SetColor(0);
                    grp.FillRect(pageSplitXStart + 12, i17, i18, 1);
                    grp.FillRect(pageSplitXStart + 12, i17 + 307 - 1 - 12, i18, 1);
                    GameRuntime.DrawImageRes(i18 + pageSplitXStart + 12, i17, 378);
                    GameRuntime.DrawImageRes(screenCX, screenCY, 376);
                }
                GameRuntime.SetTextStyle(-3, 3);
                GameRuntime.SetTextColor(0, 0xFF7800);
                GameRuntime.SetTextColor(1, 0);
                GameRuntime.DrawImageRes(9, 9, 102);
                string stringBuffer = GetTotalEggCount() + "/450";
                GameRuntime.DrawText(stringBuffer, 0, stringBuffer.Length, 41, 10, 20);
                DrawTranslucentSoftkeyBar(grp);
            }
            return false;
        }
        if (ui.UIID == GameScene.MENU_PAUSE
            || ui.UIID == GameScene.CONFIRM_RESTART_LEVEL
            || ui.UIID == GameScene.CONFIRM_RETURN_LEVEL_SELECT
            || ui.UIID == GameScene.CONFIRM_EXIT_LEVEL
            || ui.UIID == (GameScene)15
            || ui.UIID == GameScene.INFO_FIELD_MESSAGE
            || type != 1)
        {
            if (type == 1)
                DrawTranslucentSoftkeyBar(grp);
            if (type == 4 || type == 2 || type == 9)
                return false;
            if (ui.UIID == GameScene.MENU_PAUSE
                || ui.UIID == GameScene.CONFIRM_RESTART_LEVEL
                || ui.UIID == GameScene.CONFIRM_RETURN_LEVEL_SELECT
                || ui.UIID == GameScene.CONFIRM_EXIT_LEVEL
                || ui.UIID == GameScene.INFO_FIELD_MESSAGE
                || type != 10)
            {
                return true;
            }
            grp.SetClip(0, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
            int selArrowDisp = SIN_COS_TABLE[(int)((GameRuntime.CurrentTimeMillis() >> 1) % 360)] * 5 / 360;
            GameRuntime.DrawImageResAnchored(
                xpos - 5 - 4 + selArrowDisp,
                (height >> 1) + ypos,
                2,
                Graphics.Anchor.RIGHT | Graphics.Anchor.VCENTER
            ); // selection arrow L
            GameRuntime.DrawImageResAnchored(
                xpos + width + 5 + 4 - selArrowDisp,
                (height >> 1) + ypos,
                326,
                Graphics.Anchor.LEFT | Graphics.Anchor.VCENTER
            ); // selection arrow R
            return false;
        }
        ClearUIBackground(grp);
        int i20 = GameRuntime.CurrentWidth >> 1;
        int i21 = GameRuntime.CurrentHeight >> 1;
        if (ui.UIID == GameScene.MENU_TITLE)
        {
            int b3 = GameRuntime.GetCompoundSpriteParamEx(332, 0);
            int i22 = b3 >> 16;
            short s = (short)b3;
            int b4 = GameRuntime.GetCompoundSpriteParamEx(332, 1);
            int i23 = b4 >> 16;
            int i24 = i20 - 117;
            int i25 = i21 - 157;
            int i26 = i20 + i23 - i24;
            grp.SetColor(0x644330);
            grp.FillRect(i20 + i22, i21 + s, i23 - i22, (short)b4 - s);
            GameRuntime.DrawImageRes(i20, i21, 332);
            grp.SetColor(0x55270F);
            grp.DrawRect(i24 + 2, i25 + 2, i26 - 4, 296);
            grp.DrawRect(i24 + 3, i25 + 3, i26 - 6, 294);
            grp.SetColor(0x371909);
            grp.DrawRect(i24, i25, i26, 300);
            grp.DrawRect(i24 + 1, i25 + 1, i26 - 2, 298);
            GameRuntime.DrawImageResTransformed(i20 + i26 - 117 + 2, i21 - 157 - 2, 12, Graphics.Anchor.TOP | Graphics.Anchor.RIGHT, Sprite.Transform.ROT270);
            GameRuntime.DrawImageResAnchored(i20 + i26 - 117 + 2, i21 + 300 - 157 + 2, 12, Graphics.Anchor.BOTTOM | Graphics.Anchor.RIGHT);
            GameRuntime.DrawImageResAnchored(i24 - 5, i21 - 75, 15, Graphics.Anchor.LEFT | Graphics.Anchor.VCENTER);
            GameRuntime.DrawImageResAnchored(i24 - 5, i21 + 75, 15, Graphics.Anchor.LEFT | Graphics.Anchor.VCENTER);
        }
        else
            DrawBookFrame(i20, i21, grp);
        DrawTranslucentSoftkeyBar(grp);
        return false;
    }

    public static int GetSoftkeyBarSize()
    {
        return GameRuntime.GetFontHeight(-3) + 4;
    }

    public static int GetLevelType(LevelID levelId)
    {
        int result = 2;
        if (levelId <= LevelID.TUNNEL_OF_TREASURES)
            result = 1;
        if (levelId <= LevelID.SECRET_STALKWAY)
            return 0;
        return result;
    }

    private static string FormatGameTimer(int seconds)
    {
        int mm = seconds / 60;
        int ss = seconds % 60;
        return ss < 10 ? mm + ":0" + ss : mm + ":" + ss;
    }

    private void SetIngameHID()
    {
        drawUI = null;
        GameRuntime.ResetSoftkeys();
        GameRuntime.SetSoftkey(GameRuntime.Softkey.RIGHT, "", 1);
        GameRuntime.InitHID(GameRuntime.ControlMode.GAME);
        GameRuntime.ResetHID();
    }

    private void DrawLoadingBar(Graphics graphics)
    {
        if (hasLoadingProgressBar)
        {
            graphics.SetColor(0x703005);
            graphics.FillRect(0, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
            graphics.SetColor(0);
            graphics.FillRect((GameRuntime.CurrentWidth - 60) / 2, (GameRuntime.CurrentHeight - 10) / 2, 60, 10);
            graphics.SetColor(0x471D00);
            graphics.FillRect((GameRuntime.CurrentWidth - 60) / 2 + 1, (GameRuntime.CurrentHeight - 10) / 2 + 1, 58, 8);
            graphics.SetColor(0xAEE13C);
            graphics.FillRect((GameRuntime.CurrentWidth - 60) / 2 + 1, (GameRuntime.CurrentHeight - 10) / 2 + 1, loadingProgressBar * 60 / 20 - 2, 8);
            loadingProgressBar++;
            if (loadingProgressBar > 20)
            {
                loadingProgressBar = 0;
            }
            return;
        }
        graphics.SetColor(0xFFFFFF);
        graphics.FillRect(0, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
    }

    private static bool CheckSuperBounceUnlocked()
    {
        for (LevelID i = 0; i < LevelID.LEVEL_IDX_MAX; i++)
        {
            if (GetLevelEggCount(i) < LEVEL_EGG_TROPHY_REQUIREMENTS[(int)i * 3] || GetLevelClearTime(i) > LEVEL_TIMER_TROPHY_REQUIREMENTS[(int)i * 3])
                return false;
        }
        return true;
    }

    public static bool WasLevelBeaten(LevelID levelId)
    {
        int clearTime = GetLevelClearTime(levelId);
        return clearTime > 0 && clearTime < 9999;
    }

    private static bool IsLevelUnlocked(LevelID levelId)
    {
        return GetLevelClearTime(levelId) > 0;
    }

    private static void UnlockLevel(LevelID levelId)
    {
        if (GetLevelClearTime(levelId) == 0)
            SetLevelClearTime(levelId, 9999);
    }

    private static void DebugLevelUnlock(LevelID levelId)
    {
        int status = GetLevelClearTime(levelId);
        if (status == 0 || status == 9999)
            SetLevelClearTime(levelId, 300);
    }

    private static int GetTotalEggCount()
    {
        int totalEggs = 0;
        for (LevelID levelIdx = 0; levelIdx < LevelID.LEVEL_IDX_MAX; levelIdx++)
            totalEggs += GetLevelEggCount(levelIdx);
        return totalEggs;
    }

    private static void SetLevelEggCount(LevelID levelId, int eggCount)
    {
        levelSaveData[(int)levelId << 2] = (short)eggCount;
    }

    private static void SetLevelClearTime(LevelID levelId, int clearTime)
    {
        levelSaveData[((int)levelId << 2) + 1] = (short)clearTime;
    }

    private static int GetLevelEggCount(LevelID levelId)
    {
        return levelSaveData[(int)levelId << 2];
    }

    private static int GetLevelClearTime(LevelID levelId)
    {
        return levelSaveData[((int)levelId << 2) + 1];
    }

    private static short GetLevelLocalHighScore(LevelID levelId)
    {
        return levelSaveData[((int)levelId << 2) + 2];
    }

    private static short GetLevelGlobalHighScore(LevelID levelId)
    {
        return levelSaveData[((int)levelId << 2) + 3];
    }

    private static int GetCollectionChallengeRank(LevelID levelId)
    {
        int collectedEggs = GetLevelEggCount(levelId);
        if (collectedEggs >= LEVEL_EGG_TROPHY_REQUIREMENTS[(int)levelId * 3])
            return 2;
        if (collectedEggs >= LEVEL_EGG_TROPHY_REQUIREMENTS[(int)levelId * 3 + 1])
            return 1;
        if (collectedEggs >= LEVEL_EGG_TROPHY_REQUIREMENTS[(int)levelId * 3 + 2])
            return 0;
        return -1;
    }

    private static int GetTimerChallengeRank(LevelID levelId)
    {
        int clearTime = GetLevelClearTime(levelId);
        if (clearTime <= LEVEL_TIMER_TROPHY_REQUIREMENTS[(int)levelId * 3])
            return 2;
        if (clearTime <= LEVEL_TIMER_TROPHY_REQUIREMENTS[(int)levelId * 3 + 1])
            return 1;
        if (clearTime <= LEVEL_TIMER_TROPHY_REQUIREMENTS[(int)levelId * 3 + 2])
            return 0;
        return -1;
    }

    public static int GetUnlockedFormeCount()
    {
        int i = 0;
        if (WasLevelBeaten(FORME_UNLOCK_LEVELS[0]) || CurrentLevel == FORME_UNLOCK_LEVELS[0] && EventObject.EventVars[2] > 0)
            i = 1;
        if (WasLevelBeaten(FORME_UNLOCK_LEVELS[1]) || CurrentLevel == FORME_UNLOCK_LEVELS[1] && EventObject.EventVars[2] > 0)
            return 2;
        return i;
    }

    private static void DeserializeSaveData(byte[] save)
    {
        for (int saveDataInIdx = 0, saveDataOutIdx = 0; saveDataOutIdx < levelSaveData.Length; saveDataOutIdx++, saveDataInIdx += 2)
            levelSaveData[saveDataOutIdx] = GameObject.ReadShort(save, saveDataInIdx);
    }
    
    private static void ClearSaveData()
    {
        Array.Clear(levelSaveData);
    }
    
    private static void SerializeSaveData()
    {
        byte[] saveData = new byte[levelSaveData.Length << 1];
        for (int i = 0; i < levelSaveData.Length; i++)
        {
            saveData[i << 1] = (byte)(levelSaveData[i] >> 8);
            saveData[(i << 1) + 1] = (byte)levelSaveData[i];
        }
        GameRuntime.SaveToRecordStore(saveData);
    }

    private static void InitStolenColorData() // inlined in 2.0.25
    {
        stolenColorsAnimationCountdown = 0;
        stolenColorsFlashCountdown = 0;
        isFlashToOtherColorMode = false;
        isColorsAreStolen = false;
        try
        {
            if (parallaxImagesStolenColors != null)
            {
                foreach (Image image in parallaxImagesStolenColors)
                    image?.Dispose();
            }
            parallaxImagesRegColors = new Image[ALL_PARALLAX_IMAGE_IDS.Length];
            parallaxImagesStolenColors = new Image[ALL_PARALLAX_IMAGE_IDS.Length];
            for (int i = 0; i < ALL_PARALLAX_IMAGE_IDS.Length; i++)
            {
                if (GameRuntime.GetImageResource(ALL_PARALLAX_IMAGE_IDS[i]) != null)
                {
                    Image regColors = GameRuntime.GetImageResource(ALL_PARALLAX_IMAGE_IDS[i]);
                    parallaxImagesRegColors[i] = regColors;
                    Color32[] stolenRGB = new Color32[regColors.Width * regColors.Height];
                    regColors.GetRGB(stolenRGB);
                    for (int rgbIdx = 0; rgbIdx < stolenRGB.Length; rgbIdx++) // TODO: Tidy up this mess
                    {
                        int rgb = stolenRGB[rgbIdx].ToARGB();
                        int r = rgb >> 16 & 255;
                        int g = rgb >> 8 & 255;
                        int b = rgb & 255;
                        stolenRGB[rgbIdx] = Color32.FromARGB((rgb >>> 24 << 24) + (g + b >> 1 << 16) + (b + r >> 1 << 8) + (r + g >> 1));
                    }
                    parallaxImagesStolenColors[i] = Image.CreateImage(stolenRGB, regColors.Width, regColors.Height);
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    private static void ResetParallaxStolenColors()
    {
        for (int i = 0; i < ALL_PARALLAX_IMAGE_IDS.Length; i++)
        {
            if (!(GameRuntime.GetImageResource(ALL_PARALLAX_IMAGE_IDS[i]) == null || parallaxImagesRegColors[i] == null))
                GameRuntime.ReplaceImageResource(ALL_PARALLAX_IMAGE_IDS[i], parallaxImagesRegColors[i]);
        }
    }

    public static int GetStolenColorIfApplicable(int argb)
    {
        // 10 - false
        // 01 - false
        // 11 - true
        // 00 - true
        if (isColorsAreStolen && isFlashToOtherColorMode || !isColorsAreStolen && !isFlashToOtherColorMode)
            return argb;
        int i2 = argb >> 16 & 255;
        int i3 = argb >> 8 & 255;
        int i4 = argb & 255;
        return (argb >>> 24 << 24) + (i3 + i4 >> 1 << 16) + (i4 + i2 >> 1 << 8) + (i2 + i3 >> 1);
    }

    private void UpdateLoadingScreen()
    {
        if (curSplashId + 1 >= SPLASH_SCREEN_LAYOUT_RESIDS.Length)
            GameRuntime.StartLoadScene(GameScene.CALL_TITLE_MENU);
        else if (GameRuntime.IsResourceLoadDone(SPLASH_SCREEN_LAYOUT_RESIDS[curSplashId + 1]))
        {
            hasLoadingProgressBar = true;
            curSplashId++;
            splashScreenStartTime = GameRuntime.CurrentTimeMillis();
            if (curSplashId - 1 > -1)
                GameRuntime.UnloadResource(SPLASH_SCREEN_LAYOUT_RESIDS[curSplashId - 1]);
        }
    }

    private void LevelEnded()
    {
        ResetParallaxStolenColors();
        isLevelActive = false;
        GameRuntime.StartLoadScene(GameScene.EXIT_LEVEL);
        exitLevelReturnScene = GameScene.INFO_CHAPTER_COMPLETE;
        CurrentPlayerState = PlayerState.PLAY;
    }

    private static void UnloadLevel()
    {
        BallFramebuffer?.Dispose();
        BallFramebuffer = null;
        BallGraphics = null;
        BallFramebufferRGB = null;

        SpriteFB?.Dispose();
        SpriteFB = null;
        SpriteOffscreenGraphics = null;
        SpriteFBRGB = null;

        //GeometryObject.TEMP_QUAD_XS = null;
        //GeometryObject.TEMP_QUAD_YS = null;
        RootLevelObj = null;
        GameObject.CameraTarget = null;
        BounceObj = null;
        events = null;
        CurrentCannon = null;
        parallaxImagesRegColors = null;

        if (parallaxImagesStolenColors != null)
        {
            foreach (Image image in parallaxImagesStolenColors)
                image?.Dispose();
        }
        parallaxImagesStolenColors = null;

        GameRuntime.UnloadResource(ResourceID.GRAPHICS_BALLHIGHLIGHT_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_BALLPARTS_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJDOOR_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJLEVER_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJSIGNBOARD_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJFRIEND_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJSTONEWALL_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_UIPAUSEMENU_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_ENEMY00CANDLE_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_ENEMY02MOLE_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJHYPNOTOID_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJSPIKE_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJEGG_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_PARTICLESPLASH_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_PARTICLECOMMON_RES);
        GameRuntime.UnloadResource(ResourceID.GRAPHICS_BALLBUMPYCRACKS_RES);
        switch (GetLevelType(CurrentLevel))
        {
            case 0:
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_LEVELACT01_RES);
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJCOLORMACHINE_RES);
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJCOLORMACHINEBROKEN_RES);
                break;
            case 1:
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_LEVELACT02_RES);
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJCOLORMACHINE_RES);
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJCOLORMACHINEBROKEN_RES);
                break;
            case 2:
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_LEVELACT03_RES);
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJCANNON_RES);
                // bugfix: this resource is not unloaded in the original game, causing a resource leak
                GameRuntime.UnloadResource(ResourceID.GRAPHICS_OBJCOLORMACHINE_RES);
                break;
        }
        GameRuntime.LoadResource(ResourceID.GRAPHICS_UIMAINMENU_RES);
        GameRuntime.LoadResource(ResourceID.GRAPHICS_UILEVELSELECT_RES);
    }

    private void SetUI(GameScene uiID)
    {
        Debug.WriteLine("Bounce SetUI " + uiID);
        ui.Clear();
        ui.DisableSoftkey(0);
        ui.DisableSoftkey(1);
        ui.LayoutAttributes = null;
        ui.ElemDefaultAttributes = null;
        ui.UIID = uiID;
        ui.SetElemDefaultAttribute(UIElement.FONT, -2);
        ui.SetAttribute(UILayout.FONT, -2);
        switch (uiID)
        {
            case GameScene.MENU_HIGH_SCORES: // high scores list
                ui.LoadFromResource(36);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, LAYOUT_DEFAULT_TITLE_PADDING_TOP);
                ui.SetAttribute(UILayout.TITLE_PADDING_BOTTOM, LAYOUT_DEFAULT_TITLE_PADDING_BOTTOM);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_VERTICAL_MARGIN);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.OFFSET_LEFT, LAYOUT_DEFAULT_HORIZONTAL_MARGIN - 2);

                // HD
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, 4);
                ui.SetAttribute(UILayout.FIXED_WIDTH, 240 - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, 320 - LAYOUT_DEFAULT_VERTICAL_MARGIN - 36);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);

                ui.SetElemDefaultAttribute(3, 0);
                ui.SetElemDefaultAttribute(2, 32);
                ui.SetAttribute(UILayout.BLOCK_INCREMENT, GameRuntime.GetFontHeight(-3) << 1);
                ui.SetTitle(StringManager.GetMessage(MessageID.UI_HIGH_SCORES), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_BACK), 0, GameScene.MENU_TITLE, true);
                bool hasAnyHighScore = false;
                for (LevelID levelIdx = 0; levelIdx < LevelID.LEVEL_IDX_MAX; levelIdx++)
                {
                    if (GetLevelGlobalHighScore(levelIdx) > 0)
                        hasAnyHighScore = true;
                }
                if (hasAnyHighScore)
                {
                    for (LevelID levelIdx = 0; levelIdx < LevelID.LEVEL_IDX_MAX; levelIdx++)
                    {
                        int highScore = Math.Max((short)0, GetLevelGlobalHighScore(levelIdx));
                        string chapterNoStr;
                        if (IsBonusLevel(levelIdx))
                            chapterNoStr = StringManager.GetMessage(MessageID.UI_CHAPTERNO_BONUS, GetLevelChapterNumber(levelIdx));
                        else
                            chapterNoStr = StringManager.GetMessage(MessageID.UI_CHAPTERNO_STD, GetLevelChapterNumber(levelIdx));
                        int separatorImageId = 89;
                        if (levelIdx == 0)
                            separatorImageId = -1;
                        ui.AddElement(new UIElement(chapterNoStr + "\n" + highScore, separatorImageId, ui));
                    }
                }
                else
                    ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.EMPTY), -1, ui));
                break;
            case GameScene.MENU_TITLE: // main menu
                ui.LoadFromResource(37);
                ui.SetElemDefaultAttribute(UIElement.FONT, -2); // font
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetElemDefaultAttribute(UIElement.AUTO_WIDTH, UIElement.AUTO_WIDTH_BIT);
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, LAYOUT_MAIN_MENU_TITLE_PADDING);
                ui.SetAttribute(UILayout.SCROLL_WRAPAROUND, UILayout.SCROLL_WRAPAROUND_BIT);

                // HD
                ui.SetAttribute(UILayout.MARGIN_TOP, 14);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, 320);

                ui.SetElemDefaultAttribute(UIElement.FONT_TEXT_COLOR_SELECTED, 0xFF7800);
                ui.SetTitle("", -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_SELECT), 0, GameScene.SELECTED, true);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_LEAVE), 0, GameScene.QUIT_GAME, false);
                if (IsLevelUnlocked(LevelID.UNFRIENDLY_FRIENDS))
                {
                    ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_CONTINUE), -1, ui, GameScene.MENU_LEVEL_SELECT));
                    ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_NEW_GAME), -1, ui, GameScene.MENU_NEW_GAME));
                }
                else
                {
                    ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_NEW_GAME), -1, ui, GameScene.MENU_LEVEL_SELECT));
                    selectedLevelId = 0;
                }
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_HIGH_SCORES), -1, ui, GameScene.MENU_HIGH_SCORES));
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_GUIDE), -1, ui, GameScene.MENU_GUIDE));
                //if (moreGamesStatus && MessageID.UI_MORE_GAMES > 0) // since 2.0.25
                //    ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_MORE_GAMES), -1, ui, (int)GameScene.OPEN_MORE_GAMES_URL));
                ui.SetSelectedOption(lastMenuOption);
                break;
            case GameScene.MENU_LEVEL_SELECT: // level select
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_SELECT), 0, GameScene.ENTER_LEVEL, true);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_BACK), 0, GameScene.MENU_TITLE, true);
                UpdateLevelStartSoftkeyByUnlock(ui);
                break;
            case GameScene.MENU_NEW_GAME: // start new game
                ui.LoadFromResource(37);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                /*this.ui.setAttribute(UILayout.FIXED_WIDTH, (GameRuntime.currentWidth - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1)) + 4);
                this.ui.setAttribute(UILayout.OFFSET_LEFT, 18);*/ //added in 2.0.25, removed for HD
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, LAYOUT_DEFAULT_TITLE_PADDING_TOP);
                ui.SetAttribute(UILayout.TITLE_PADDING_BOTTOM, LAYOUT_DEFAULT_TITLE_PADDING_BOTTOM);

                // HD
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, 0);
                ui.SetAttribute(UILayout.MARGIN_BOTTOM, 180);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);
                ui.SetAttribute(UILayout.PACKED_HEIGHT, UILayout.PACKED_HEIGHT_BIT);

                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_NEW_GAME), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_YES), 0, GameScene.START_NEW_GAME, false);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_NO), 0, GameScene.MENU_TITLE, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.GAME_PROGRESS_WILL_BE_LOST), -1, ui));
                break;
            case GameScene.MENU_GUIDE: // guide
                ui.LoadFromResource(36);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, LAYOUT_DEFAULT_TITLE_PADDING_TOP);
                ui.SetAttribute(UILayout.TITLE_PADDING_BOTTOM, LAYOUT_DEFAULT_TITLE_PADDING_BOTTOM);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_VERTICAL_MARGIN);

                // HD
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, 4);
                ui.SetAttribute(UILayout.FIXED_WIDTH, 240 - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, 320 - LAYOUT_DEFAULT_VERTICAL_MARGIN - 36);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);

                ui.SetAttribute(UILayout.OFFSET_LEFT, LAYOUT_DEFAULT_HORIZONTAL_MARGIN - 2);
                ui.SetElemDefaultAttribute(3, 0);
                ui.SetElemDefaultAttribute(2, 32);
                ui.SetAttribute(UILayout.BLOCK_INCREMENT, GameRuntime.GetFontHeight(-3) << 1);
                ui.SetTitle(StringManager.GetMessage(MessageID.UI_GUIDE), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_BACK), 0, GameScene.MENU_TITLE, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.GUIDE_TEXT_1), -1, ui));
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.GUIDE_TEXT_2), 102, ui));
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.GUIDE_TEXT_3), -1, ui));
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.GUIDE_TEXT_4), 371, ui));
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.GUIDE_TEXT_5), 372, ui));
                break;
            case GameScene.CONFIRM_QUIT_GAME: // quit game
                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_QUIT_GAME), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_YES), 0, GameScene.QUIT_GAME, false);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_NO), 0, GameScene.MENU_TITLE, true);
                break;
            case GameScene.MENU_PAUSE: // pause menu
                ui.LoadFromResource(37);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetElemDefaultAttribute(UIElement.AUTO_WIDTH, 256);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentWidth - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);
                ui.SetAttribute(UILayout.PACKED_HEIGHT, UILayout.PACKED_HEIGHT_BIT);
                ui.SetAttribute(UILayout.SCROLL_WRAPAROUND, UILayout.SCROLL_WRAPAROUND_BIT);
                ui.SetElemDefaultAttribute(UIElement.FONT_TEXT_COLOR_SELECTED, 0xFF7800);
                ui.SetAttribute(UILayout.SOFTKEY_BAR, 0);
                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_PAUSE_MENU), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_SELECT), 0, GameScene.SELECTED, true);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_QUIT), 0, GameScene.CONFIRM_EXIT_LEVEL, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_CONTINUE_LEVEL), -1, ui, GameScene.UNPAUSE_LEVEL));
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_RESTART_LEVEL), -1, ui, GameScene.CONFIRM_RESTART_LEVEL));
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.UI_RETURN_LEVEL_SELECT), -1, ui, GameScene.CONFIRM_RETURN_LEVEL_SELECT));
                ui.SetSelectedOption(lastMenuOption);
                break;
            case GameScene.CONFIRM_RESTART_LEVEL: // confirm restart level
                ui.LoadFromResource(37);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetElemDefaultAttribute(UIElement.AUTO_WIDTH, 256);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);
                ui.SetAttribute(UILayout.PACKED_HEIGHT, UILayout.PACKED_HEIGHT_BIT);
                ui.SetAttribute(UILayout.SOFTKEY_BAR, 0);
                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_RESTART_LEVEL), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_YES), 0, GameScene.RESTART_LEVEL, false);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_NO), 0, GameScene.MENU_PAUSE, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.LEVEL_PROGRESS_WILL_BE_LOST), -1, ui));
                break;
            case GameScene.CONFIRM_RETURN_LEVEL_SELECT: // confirm return to level select
                ui.LoadFromResource(37);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                exitLevelReturnScene = GameScene.MENU_LEVEL_SELECT;
                ui.SetElemDefaultAttribute(UIElement.AUTO_WIDTH, UIElement.AUTO_WIDTH_BIT);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);
                ui.SetAttribute(UILayout.PACKED_HEIGHT, UILayout.PACKED_HEIGHT_BIT);
                ui.SetAttribute(UILayout.SOFTKEY_BAR, 0);
                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_RETURN_LEVEL_SELECT), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_YES), 0, GameScene.EXIT_LEVEL, false);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_NO), 0, GameScene.MENU_PAUSE, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.LEVEL_PROGRESS_WILL_BE_LOST), -1, ui));
                break;
            case GameScene.CONFIRM_EXIT_LEVEL: // confirm quit level
                ui.LoadFromResource(37);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                exitLevelReturnScene = GameScene.MENU_TITLE;
                ui.SetElemDefaultAttribute(UIElement.AUTO_WIDTH, UIElement.AUTO_WIDTH_BIT);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);
                ui.SetAttribute(UILayout.PACKED_HEIGHT, UILayout.PACKED_HEIGHT_BIT);
                ui.SetAttribute(UILayout.SOFTKEY_BAR, 0);
                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_QUIT_GAME), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_YES), 0, GameScene.EXIT_LEVEL, false);
                ui.SetSoftkey(GameRuntime.Softkey.RIGHT, StringManager.GetMessage(MessageID.UI_NO), 0, GameScene.MENU_PAUSE, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.LEVEL_PROGRESS_WILL_BE_LOST), -1, ui));
                break;
            case GameScene.INFO_CHAPTER_COMPLETE: // chapter complete
                ui.LoadFromResource(36);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, LAYOUT_DEFAULT_TITLE_PADDING_TOP);
                ui.SetAttribute(UILayout.TITLE_PADDING_BOTTOM, LAYOUT_DEFAULT_TITLE_PADDING_BOTTOM);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_VERTICAL_MARGIN);
                ui.SetAttribute(UILayout.OFFSET_LEFT, LAYOUT_DEFAULT_HORIZONTAL_MARGIN - 2);
                ui.SetTitle(StringManager.GetMessage(MessageID.CHAPTER_COMPLETE), -1, 1);

                // HD
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, 4);
                ui.SetAttribute(UILayout.FIXED_WIDTH, 240 - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, 320 - LAYOUT_DEFAULT_VERTICAL_MARGIN - 36);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);

                if (wasSuperBounceJustUnlocked)
                    ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_OK), 0, GameScene.INFO_GAME_COMPLETED, true);
                else if (wasFinalLevelJustBeaten)
                    ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_OK), 0, GameScene.INFO_GAME_BEATEN, true);
                else
                    ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_OK), 0, GameScene.MENU_LEVEL_SELECT, true);

                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.SCORE, calcScore), -1, ui));
                string eggsCollectedStr = EggCount + "/" + bonusLevelEggLimit;
                UIElement eggsCollectedUI = new(eggsCollectedStr, 102, ui);
                if (isTextRightToLeft)
                {
                    eggsCollectedUI.SetAttribute(UIElement.ICON_ALIGNMENT, 16);
                    eggsCollectedUI.SetText(eggsCollectedStr, 102);
                }
                ui.AddElement(eggsCollectedUI);
                string timerStr = FormatGameTimer(LevelTimer / 1000);
                UIElement timerUI = new(timerStr, 103, ui);
                if (isTextRightToLeft)
                {
                    timerUI.SetAttribute(UIElement.ICON_ALIGNMENT, 16);
                    timerUI.SetText(timerStr, 103);
                }
                ui.AddElement(timerUI);
                if (highScoreBeaten)
                {
                    UIElement newHighScoreText = new(StringManager.GetMessage(MessageID.NEW_HIGH_SCORE), -1, ui);
                    newHighScoreText.SetAttribute(UIElement.TEXT_ALIGNMENT, 8);
                    newHighScoreText.SetText(StringManager.GetMessage(MessageID.NEW_HIGH_SCORE), -1);
                    ui.AddElement(newHighScoreText);
                }
                if (WasLevelBeaten(LevelID.GAME_CLEAR_LEVEL) && !wasFinalLevelJustBeaten && !IsBonusLevel(CurrentLevel))
                {
                    bool anyMedalsWon = false;
                    if (timerChallengeTrophy >= 0)
                    {
                        short timerTrophyImageId = TROPHY_IMAGE_IDS[timerChallengeTrophy];
                        anyMedalsWon = true;
                        UIElement timerChallengeText = new(StringManager.GetMessage(MessageID.TIMER_CHALLENGE), timerTrophyImageId, ui);
                        timerChallengeText.SetAttribute(UIElement.ICON_ALIGNMENT, isTextRightToLeft ? 0 : 16); // since 2.0.25
                        timerChallengeText.SetAttribute(UIElement.FLAG_3, 64);
                        timerChallengeText.SetAttribute(UIElement.TEXT_ALIGNMENT, 8);
                        timerChallengeText.SetText(StringManager.GetMessage(MessageID.TIMER_CHALLENGE), timerTrophyImageId);
                        ui.AddElement(timerChallengeText);
                    }
                    if (collectionChallengeTrophy >= 0)
                    {
                        short collectionTrophyImageId = TROPHY_IMAGE_IDS[collectionChallengeTrophy];
                        anyMedalsWon = true;
                        UIElement collectionChallengeText = new(StringManager.GetMessage(MessageID.COLLECTION_CHALLENGE), collectionTrophyImageId, ui);
                        collectionChallengeText.SetAttribute(UIElement.ICON_ALIGNMENT, isTextRightToLeft ? 0 : 16); // since 2.0.25
                        collectionChallengeText.SetAttribute(UIElement.FLAG_3, 64);
                        collectionChallengeText.SetAttribute(UIElement.TEXT_ALIGNMENT, 8);
                        collectionChallengeText.SetText(StringManager.GetMessage(MessageID.COLLECTION_CHALLENGE), collectionTrophyImageId);
                        ui.AddElement(collectionChallengeText);
                    }
                    if (!anyMedalsWon)
                    {
                        UIElement noMedalsWonText = new(StringManager.GetMessage(MessageID.NO_MEDALS_WON), -1, ui);
                        noMedalsWonText.SetAttribute(UIElement.TEXT_ALIGNMENT, 8);
                        noMedalsWonText.SetText(StringManager.GetMessage(MessageID.NO_MEDALS_WON), -1);
                        ui.AddElement(noMedalsWonText);
                    }
                }
                wasFinalLevelJustBeaten = false;
                wasSuperBounceJustUnlocked = false;
                timerChallengeTrophy = -1;
                collectionChallengeTrophy = -1;
                highScoreBeaten = false;
                CycleLevelSelectRight(ui, false);
                break;
            case GameScene.INFO_GAME_BEATEN: // all levels beaten
                ui.LoadFromResource(36);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, LAYOUT_DEFAULT_TITLE_PADDING_TOP);
                ui.SetAttribute(UILayout.TITLE_PADDING_BOTTOM, LAYOUT_DEFAULT_TITLE_PADDING_BOTTOM);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_VERTICAL_MARGIN);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.OFFSET_LEFT, LAYOUT_DEFAULT_HORIZONTAL_MARGIN - 2);

                // HD
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, 4);
                ui.SetAttribute(UILayout.FIXED_WIDTH, 240 - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, 320 - LAYOUT_DEFAULT_VERTICAL_MARGIN - 36);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);

                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_GAME_BEATEN), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_OK), 0, GameScene.MENU_LEVEL_SELECT, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.ALL_LEVELS_BEATEN), -1, ui));
                break;
            case GameScene.INFO_GAME_COMPLETED: // all levels completed
                ui.LoadFromResource(36);
                ui.SetElemDefaultAttribute(UIElement.FONT, -3);
                ui.SetAttribute(UILayout.FONT, -2);
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, LAYOUT_DEFAULT_TITLE_PADDING_TOP);
                ui.SetAttribute(UILayout.TITLE_PADDING_BOTTOM, LAYOUT_DEFAULT_TITLE_PADDING_BOTTOM);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_VERTICAL_MARGIN);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.OFFSET_LEFT, LAYOUT_DEFAULT_HORIZONTAL_MARGIN - 2);

                // HD
                ui.SetAttribute(UILayout.TITLE_PADDING_TOP, 4);
                ui.SetAttribute(UILayout.FIXED_WIDTH, 240 - (LAYOUT_DEFAULT_HORIZONTAL_MARGIN << 1) + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, 320 - LAYOUT_DEFAULT_VERTICAL_MARGIN - 36);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);

                ui.SetTitle(StringManager.GetMessage(MessageID.DIALOG_GAME_COMPLETED), -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_OK), 0, GameScene.MENU_LEVEL_SELECT, true);
                ui.AddElement(new UIElement(StringManager.GetMessage(MessageID.ALL_LEVELS_COMPLETED), -1, ui));
                break;
            case GameScene.INFO_FIELD_MESSAGE: // field message
                ui.SetElemDefaultAttribute(UIElement.FONT, -1);
                ui.SetAttribute(UILayout.FONT, -1);
                ui.SetAttribute(UILayout.MARGIN_LEFT, 2);
                ui.SetAttribute(UILayout.MARGIN_RIGHT, 2);
                ui.SetAttribute(UILayout.FIXED_WIDTH, GameRuntime.CurrentWidth - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME + 4);
                ui.SetAttribute(UILayout.FIXED_HEIGHT, GameRuntime.CurrentHeight - LAYOUT_DEFAULT_HORIZONTAL_MARGIN_INGAME);
                ui.SetAttribute(UILayout.ANCHOR_CENTER, UILayout.ANCHOR_CENTER_BIT);
                ui.SetAttribute(UILayout.PACKED_HEIGHT, UILayout.PACKED_HEIGHT_BIT);
                ui.SetAttribute(UILayout.SOFTKEY_BAR, 0);
                ui.SetTitle("", -1, 1);
                ui.SetSoftkey(GameRuntime.Softkey.CENTER, StringManager.GetMessage(MessageID.UI_OK), 0, GameScene.CLOSE_FIELD_MESSAGE, true);
                if (reqReloadFieldMsg)
                {
                    ui.AddElement(new UIElement(lastFieldMsg, -1, ui));
                    reqReloadFieldMsg = false;
                }
                else
                {
                    string fieldMsg;
                    if (fieldMessageParam != null)
                    {
                        fieldMsg = StringManager.GetMessage(fieldMessageQueue[0], fieldMessageParam);
                        fieldMessageParam = null;
                    }
                    else
                        fieldMsg = StringManager.GetMessage(fieldMessageQueue[0]);
                    if (fieldMsg.EndsWith("\n\n"))
                        fieldMsg = fieldMsg[..^2];
                    else if (fieldMsg.StartsWith("\n\n"))
                        fieldMsg = fieldMsg[2..];
                    lastFieldMsg = fieldMsg;
                    ui.AddElement(new UIElement(fieldMsg, -1, ui));
                }
                break;
        }
        drawUI = ui;
        ui.SetupSoftkeys();
        GameRuntime.InitHID(GameRuntime.ControlMode.GAME);
        GameRuntime.ResetHID();
    }

    private static void UpdateEvents()
    {
        if (isBlockingEvent)
        {
            EventObject.UpdateEvents(events);
            isBlockingEvent = false;
        }
        else
        {
            RootLevelObj.UpdateBBox();
            for (GameObject obj = RootLevelObj; obj != null; obj = obj.GetNextNodeDescendToChildren(RootLevelObj))
                obj.UpdatePhysics();
            EventObject.CheckBounceEventTrigger(events, BounceObj);
            int stolenColorsBeforeScript = EventObject.EventVars[7];
            EventObject.UpdateEvents(events);
            if (EventObject.EventVars[7] != stolenColorsBeforeScript)
            {
                isFlashToOtherColorMode = false;
                stolenColorsAnimationCountdown = 2000;
                stolenColorsFlashCountdown = 0;
            }
            for (GameObject collider = RootLevelObj; collider != null; collider = collider.GetNextNodeDescendToChildren(RootLevelObj))
                collider.CheckCollisions(RootLevelObj);
            if (ReqCameraSnap)
            {
                GameObject.CameraVelocityX = 0;
                GameObject.CameraVelocityY = 0;
                GameObject.UpdateCamera(true);
                ReqCameraSnap = false;
            }
            else
                GameObject.UpdateCamera(false);
        }
    }

    public int Update(int lastUpdateRes)
    {
        if (reqQuit)
            GameRuntime.Quit();
        if (reqPlayTitleMusic)
        {
            GameRuntime.PlayMusic(ResourceID.AUDIO_BGM_TITLE_MID, true);
            reqPlayTitleMusic = false;
        }
        if (drawUI != null)
        {
            drawUI.SetupSoftkeys();
            drawUI.UpdateTitleScroll();
        }
        else if (gameMainState == 4)
        {
            if (LevelPaused)
            {
                SetUI(GameScene.MENU_PAUSE);
                GameRuntime.StopMusic();
            }
            else if (reqReloadFieldMsg)
            {
                SetUI(GameScene.INFO_FIELD_MESSAGE);
                isBlockingEvent = true;
                isFieldMessageShowing = true;
            }
            else
            {
                totalGameTime += GameRuntime.UpdateDelta;
                WaterSingletonFlag = false;
                switch (CurrentPlayerState)
                {
                    case PlayerState.PLAY:
                        BounceObj.ZCoord = 0;
                        if (CurrentControllerState != Controller.FROZEN)
                            LevelTimer += GameRuntime.UpdateDelta;
                        if (CurrentControllerState == Controller.NORMAL)
                        {
                            bool bounceMoving;
                            if (GameRuntime.CheckButton(KeyCode.LEFT))
                            {
                                BounceObj.MoveLeft();
                                bounceMoving = true;
                            }
                            else
                                bounceMoving = false;
                            if (GameRuntime.CheckButton(KeyCode.RIGHT))
                            {
                                BounceObj.MoveRight();
                                bounceMoving = true;
                            }
                            if (GameRuntime.CheckButton(KeyCode.NUM5) || GameRuntime.CheckButton(KeyCode.NUM2)
                                                                      || GameRuntime.CheckButton(KeyCode.UP) || GameRuntime.CheckButton(KeyCode.SOFTKEY_MIDDLE))
                            {
                                BounceObj.Jump(false);
                                bounceMoving = true;
                            }
                            if (bounceMoving)
                            {
                                if (BounceObj.EyeFrame != 1)
                                    BounceObj.EyeFrame = 0;
                                BounceObj.IdleAnimStartTimer = 3000;
                            }
                        }
                        else if (CurrentControllerState == Controller.CANNON)
                        {
                            if (GameRuntime.CheckButton(KeyCode.UP))
                                CurrentCannon.RotateUp();
                            if (GameRuntime.CheckButton(KeyCode.DOWN))
                                CurrentCannon.RotateDown();
                            if (GameRuntime.CheckButton(KeyCode.NUM5) || GameRuntime.CheckButton(KeyCode.SOFTKEY_MIDDLE))
                                CurrentCannon.Fire();
                        }
                        int i2 = EventObject.EventVars[4];
                        UpdateEvents();
                        if (EventObject.EventVars[4] == 0 && i2 != 0)
                            BounceObj.ResetPhysics();
                        if (BounceObj.LocalObjectMatrix.TranslationY < RootLevelObj.AllBBox.MinY && CurrentPlayerState == PlayerState.WIN)
                            CurrentPlayerState = PlayerState.LOSE; // sanity death boundary
                        if (EggCount == bonusLevelEggLimit && IsBonusLevel(CurrentLevel))
                            CurrentPlayerState = PlayerState.WIN;
                        EventObject.EventVars[3] = (int)BounceObj.BallForme;
                        EventObject.EventVars[4] = (int)BounceObj.CurVelocity;
                        EventObject.EventVars[5] = (int)BounceObj.CurXVelocity;
                        EventObject.EventVars[6] = (int)BounceObj.CurYVelocity;
                        if (stolenColorsAnimationCountdown > 0)
                        {
                            stolenColorsAnimationCountdown -= GameRuntime.UpdateDelta;
                            stolenColorsFlashCountdown -= GameRuntime.UpdateDelta;
                            if (stolenColorsFlashCountdown <= 0)
                            {
                                stolenColorsFlashCountdown = Math.Abs(RNG.NextInt() % 200 + 300);
                                isFlashToOtherColorMode = true;
                            }
                            if (stolenColorsAnimationCountdown <= 0)
                            {
                                stolenColorsAnimationCountdown = 0;
                                stolenColorsFlashCountdown = 0;
                                isFlashToOtherColorMode = false;
                                isColorsAreStolen = !isColorsAreStolen;
                            }
                            for (int i = 0; i < ALL_PARALLAX_IMAGE_IDS.Length; i++)
                            {
                                if (GameRuntime.GetImageResource(ALL_PARALLAX_IMAGE_IDS[i]) != null)
                                {
                                    if ((!isColorsAreStolen || isFlashToOtherColorMode) && (isColorsAreStolen || !isFlashToOtherColorMode))
                                    {
                                        if (parallaxImagesRegColors[i] != null)
                                            GameRuntime.ReplaceImageResource(ALL_PARALLAX_IMAGE_IDS[i], parallaxImagesRegColors[i]);
                                    }
                                    else if (parallaxImagesStolenColors[i] != null)
                                        GameRuntime.ReplaceImageResource(ALL_PARALLAX_IMAGE_IDS[i], parallaxImagesStolenColors[i]);
                                }
                            }
                        }
                        break;
                    case PlayerState.LOSE:
                        GameRuntime.PlayMusic(ResourceID.AUDIO_ME_LOSE_MID, false);
                        CurrentPlayerState = PlayerState.LOSE_UPDATE;
                        ExitWaitTimer = 3000;
                        DeathBaseY = BounceObj.LocalObjectMatrix.TranslationY;
                        /*if (currentLevel == LevelID.FINAL_RIDE) { // removed in 2.0.25
                            EventObject.finalBossTimer = 0;
                        }*/
                        break;
                    case PlayerState.WIN:
                        GameRuntime.PlayMusic(ResourceID.AUDIO_ME_WIN_MID, false);
                        CurrentPlayerState = PlayerState.WIN_UPDATE;
                        ExitWaitTimer = 3000;
                        BounceObj.ResetPhysics();
                        if (IsBonusLevel(CurrentLevel))
                            BounceObj.EnablePhysics = false;
                        WinParticle.EmitCircle(20, BounceObj.LocalObjectMatrix.TranslationX, BounceObj.LocalObjectMatrix.TranslationY, 740, 0, 1840, 230);
                        break;
                    case PlayerState.LOSE_UPDATE: // dying -> return to checkpoint
                        ExitWaitTimer -= GameRuntime.UpdateDelta;
                        BounceObj.UpdateDeathAnimation();
                        if (ExitWaitTimer <= 0)
                        {
                            BounceObj.SetPosXY(CheckpointPosX, CheckpointPosY);
                            ReqCameraSnap = true;
                            CurrentPlayerState = PlayerState.PLAY;
                            BounceObj.FadeColor = Color32.Black;
                            GameRuntime.PlayMusic(GetLevelMusicID(), true);
                        }
                        break;
                    case PlayerState.WIN_UPDATE: // exit level and update stats
                        ExitWaitTimer -= GameRuntime.UpdateDelta;
                        BounceObj.Jump(true);
                        UpdateEvents();
                        if (ExitWaitTimer <= 0)
                        {
                            int e = GetTimerChallengeRank(CurrentLevel);
                            int d = GetCollectionChallengeRank(CurrentLevel);
                            short highScoreBefore = GetLevelGlobalHighScore(CurrentLevel);
                            bool finalRideBeatenBefore = WasLevelBeaten(LevelID.FINAL_RIDE);
                            bool superBounceUnlockedBefore = CheckSuperBounceUnlocked();
                            short levelClearTimeSeconds = (short)(LevelTimer / 1000);
                            if (levelClearTimeSeconds < 1)
                                levelClearTimeSeconds = 1;
                            int finalScore = (int)(EggCount * 10000 * EGG_SCORE_MULTIPLIER_BY_LEVEL[(int)CurrentLevel] / levelClearTimeSeconds);
                            if (finalScore > 32767)
                                finalScore = 32767;
                            calcScore = (short)finalScore;
                            UpdateLevelStats(CurrentLevel, (short)EggCount, levelClearTimeSeconds, (short)calcScore);
                            if (!IsBonusLevel(CurrentLevel))
                            {
                                LevelID nextLevel = CurrentLevel + 1;
                                if (IsBonusLevel(nextLevel))
                                    nextLevel++;
                                if (nextLevel <= LevelID.FANTASTIC_FAIR && !IsLevelUnlocked(nextLevel))
                                    UnlockLevel(nextLevel);
                            }
                            int bonusChapterNo = 0;
                            for (int bonusLevelIdx = 0; bonusLevelIdx < BONUS_LEVEL_INFO.Length; bonusLevelIdx += 2)
                            {
                                if (GetTotalEggCount() >= BONUS_LEVEL_INFO[bonusLevelIdx + 1]) // required egg count for unlock
                                {
                                    LevelID bonusLevelLevelId = (LevelID)BONUS_LEVEL_INFO[bonusLevelIdx];
                                    if (!IsLevelUnlocked(bonusLevelLevelId))
                                    {
                                        UnlockLevel(bonusLevelLevelId);
                                        bonusChapterNo = (bonusLevelIdx >> 1) + 1;
                                    }
                                }
                            }
                            if (!finalRideBeatenBefore && WasLevelBeaten(LevelID.GAME_CLEAR_LEVEL))
                                wasFinalLevelJustBeaten = true;
                            if (!superBounceUnlockedBefore && CheckSuperBounceUnlocked())
                                wasSuperBounceJustUnlocked = true;
                            int e2 = GetTimerChallengeRank(CurrentLevel);
                            if (e2 > e)
                                timerChallengeTrophy = e2;
                            int d2 = GetCollectionChallengeRank(CurrentLevel);
                            if (d2 > d)
                                collectionChallengeTrophy = d2;
                            if (GetLevelGlobalHighScore(CurrentLevel) > highScoreBefore)
                                highScoreBeaten = true;
                            SerializeSaveData();
                            if (bonusChapterNo > 0)
                            {
                                reqQuitLevelAfterFieldMessage = true;
                                fieldMessageParam = [bonusChapterNo.ToString()];
                                PushFieldMessage(MessageID.BONUS_CHAPTER_UNLOCKED);
                            }
                            else
                                LevelEnded();
                        }
                        break;
                }
                PopFieldMessage();
            }
        }
        return 0;
    }

    public int Paint(int lastPaintResult, int paintMode)
    {
        short[] sArr;
        short[] sArr2;
        short[] sArr3;
        Graphics grp = GameRuntime.GetGraphicsObj();
        if (gameMainState == 2) // splash screen/loading
        {
            if (curSplashId < 0)
            {
                DrawLoadingBar(grp);
                UpdateLoadingScreen();
            }
            else
            {
                grp.SetColor(SPLASH_BG_COLORS[curSplashId]);
                grp.FillRect(0, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
                int splashImageId = SPLASH_IMAGE_IDS[curSplashId];
                GameRuntime.DrawImageRes(
                    (GameRuntime.CurrentWidth - GameRuntime.GetImageMapParam(splashImageId, ImageMap.Param.WIDTH)) / 2 + GameRuntime.GetImageMapParam(splashImageId, ImageMap.Param.ORIGIN_X),
                    (GameRuntime.CurrentHeight - GameRuntime.GetImageMapParam(splashImageId, ImageMap.Param.HEIGHT)) / 2 + GameRuntime.GetImageMapParam(splashImageId, ImageMap.Param.ORIGIN_Y),
                    splashImageId
                );
                if (GameRuntime.CurrentTimeMillis() - splashScreenStartTime > SPLASH_SCREEN_DURATIONS[curSplashId])
                    UpdateLoadingScreen();
            }
            return 0;
        }
        if (paintMode == 1)
        {
            if (gameMainState == 4) // in-game
            {
                GameRuntime.SetBacklight(true);
                Graphics graphics = GameRuntime.GetGraphicsObj();
                graphics.SetClip(0, 0, renderClipWidth, renderClipHeight);
                int levelType = GetLevelType(CurrentLevel);
                short[] sArr4 = f307i;
                short[] sArr5 = f311j;
                short[] sArr6 = f314k;
                short[] fixedPosParallaxes = f318l;
                int i4 = 5413606;
                int i5 = 7460351;
                if (levelType == 1)
                {
                    short[] sArr8 = f322m;
                    short[] sArr9 = f325n;
                    short[] sArr10 = f328o;
                    fixedPosParallaxes = f331p;
                    i4 = 7263689;
                    i5 = 10485759;
                    sArr = sArr10;
                    sArr2 = sArr9;
                    sArr3 = sArr8;
                }
                else if (levelType == 2)
                {
                    short[] sArr11 = f334q;
                    short[] sArr12 = f337r;
                    short[] sArr13 = f340s;
                    fixedPosParallaxes = f343t;
                    i4 = 7737588;
                    i5 = 131610;
                    sArr = sArr13;
                    sArr2 = sArr12;
                    sArr3 = sArr11;
                }
                else
                {
                    sArr = sArr6;
                    sArr2 = sArr5;
                    sArr3 = sArr4;
                }
                if (IsBonusLevel(CurrentLevel))
                {
                    i4 = 0xB400BD;
                    i5 = 0xFD8C13;
                }
                int bgStripeW = GameRuntime.CurrentWidth;
                int bgHeight = GameRuntime.CurrentHeight;
                int bgStripeH = GameRuntime.CurrentHeight / 40;
                int i9 = i4 >> 16 & 255;
                int i10 = i4 >> 8 & 255;
                int i11 = i4 & 255;
                int i12 = 65536 / bgHeight;
                int i13 = ((i5 >> 16 & 255) - i9) * i12 * bgStripeH;
                int i14 = ((i5 >> 8 & 255) - i10) * i12 * bgStripeH;
                int i15 = ((i5 & 255) - i11) * i12 * bgStripeH;
                int i16 = i9 << 16;
                int i17 = i10 << 16;
                int i18 = i11 << 16;
                for (int bgY = 0; bgY < bgHeight; bgY += bgStripeH)
                {
                    SetBGColor((i16 >> 16 << 16) + (i17 >> 16 << 8) + (i18 >> 16), graphics);
                    graphics.FillRect(0, bgY + 0, bgStripeW, bgStripeH);
                    i16 += i13;
                    i17 += i14;
                    i18 += i15;
                }
                // bugfix: on high resolutions, not enough parallaxes were generated, resulting in early culling
                // we compensate this by generating more parallaxes for larger screen sizes
                int pscale = (renderClipWidth + 239) / 240 * ((renderClipHeight + 319) / 320);
                CheckReallocParallax(pscale * PARALLAX_MAX_COUNT + 1);
                RNG.SetSeed((int)CurrentLevel + 1);
                if (levelType == 2)
                {
                    DrawBGParallax(fixedPosParallaxes, 5, 100, 600, 100, -80, 50, 2 * pscale, -1, graphics);
                    DrawBGParallax(sArr, 20, 100, GameRuntime.GetImageMapParam(sArr[0], ImageMap.Param.WIDTH), 0, -50, 0, 3 * pscale, 0x15113C, graphics);
                    DrawBGParallax(sArr2, 50, 100, GameRuntime.GetImageMapParam(sArr2[0], ImageMap.Param.WIDTH), 0, -30, 0, 3 * pscale, 0x02021A, graphics);
                }
                else
                {
                    DrawBGParallax(sArr, 20, 100, 200, 140, -200, 200, PARALLAX_MAX_COUNT * pscale, -1, graphics);
                    DrawBGParallax(sArr2, 50, 100, 100, 70, -70, 300, PARALLAX_MAX_COUNT * pscale, -1, graphics);
                    DrawBGParallax(sArr3, 80, 100, 150, 100, -70, 300, PARALLAX_MAX_COUNT * pscale, -1, graphics);
                }
                RNG.SetSeed(GameRuntime.CurrentTimeMillis());
                GameObject.DrawSceneTree(RootLevelObj, graphics);
                graphics.SetClip(0, 0, GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
                GameRuntime.SetTextStyle(-3, 1);
                GameRuntime.SetTextColor(0, 0);
                GameRuntime.DrawImageRes(0, 0, 102);
                int eggLimitX = DrawStylizedNumber(32, 7, EggCount, Graphics.Anchor.LEFT, false) + 32;
                GameRuntime.DrawImageRes(eggLimitX, 7, 101); // divider slash (0/30)
                DrawStylizedNumber(eggLimitX + 9, 7, bonusLevelEggLimit, Graphics.Anchor.LEFT, false);
                GameRuntime.DrawImageRes(GameRuntime.CurrentWidth, 0, 103);
                int levelSecondTimer = LevelTimer / 1000;
                int levelTimeMinutes = levelSecondTimer / 60;
                int levelTimeSeconds = levelSecondTimer % 60;
                int timerSecondsX = GameRuntime.CurrentWidth - 25 - 6 - 1;
                int timerMinutesX = timerSecondsX - DrawStylizedNumber(timerSecondsX, 7, levelTimeSeconds, Graphics.Anchor.RIGHT, true);
                GameRuntime.DrawImageResAnchored(timerMinutesX, 7, 100, Graphics.Anchor.TOP | Graphics.Anchor.RIGHT); // divider colon (00:00)
                DrawStylizedNumber(timerMinutesX - 5, 7, levelTimeMinutes, Graphics.Anchor.RIGHT, false);
                isFlashToOtherColorMode = false;
            }
            drawUI?.Draw();
        }
        else if (paintMode == 2) // loading
            DrawLoadingBar(grp);
        return 0;
    }

    public int LoadScene(GameScene sceneId, int sceneResult)
    {
        if (sceneId != GameScene.MENU_TITLE && sceneId != GameScene.MENU_PAUSE)
            lastMenuOption = 0;
        switch (sceneId)
        {
            case GameScene.ENTRYPOINT:
                GameRuntime.StartLoadScene(GameScene.INIT);
                return 0;
            case GameScene.LOAD_SAVE_DATA:
                if (sceneResult >= 0 && sceneResult < SPLASH_SCREEN_LAYOUT_RESIDS.Length)
                {
                    if (sceneResult == 0)
                    {
                        curSplashId = -1;
                        gameMainState = 2;

                        drawUI = null;
                        GameRuntime.ResetSoftkeys(); // not present in the original game, disables softkeys on soft-reset
                    }
                    GameRuntime.LoadResource(SPLASH_SCREEN_LAYOUT_RESIDS[sceneResult]);
                    return sceneResult + 1;
                }
                if (sceneResult != SPLASH_SCREEN_LAYOUT_RESIDS.Length)
                    return 0;
                Debug.WriteLine("Loading saved data...");
                UnloadLevel();
                if (GameRuntime.IsMusicEnabled())
                    GameRuntime.LoadResidentResSet(0);
                byte[] savedData = GameRuntime.LoadFromRecordStore();
                if (savedData != null)
                {
                    DeserializeSaveData(savedData);
                    if (!WasLevelBeaten(LevelID.GAME_CLEAR_LEVEL))
                    {
                        for (LevelID levelId = LevelID.LEVEL_IDX_MAX - 1; levelId >= 0; levelId--)
                        {
                            if (!IsBonusLevel(levelId) && IsLevelUnlocked(levelId))
                            {
                                selectedLevelId = levelId;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ClearSaveData();
                    UnlockLevel(0);
                }
                Debug.WriteLine("Saved data loaded");
                return 0;
            case GameScene.INIT:
                if (sceneResult == 0)
                    return 1;
                if (sceneResult == 1)
                    return 2;
                if (sceneResult != 2)
                    return 0;
                if (drawUI == null || drawUI.UIID == (GameScene)14)
                {
                    GameRuntime.SetMusicEnabled(true);
                    GameRuntime.StartLoadScene(GameScene.LOAD_SAVE_DATA);
                }
                else
                    SetUI(GameScene.MENU_SOFTLOCK); // forbid soft-resetting
                return 0;
            case GameScene.EXIT_LEVEL: // exit level
                if (sceneResult == 0)
                {
                    GameRuntime.StopMusic();
                    UnloadLevel();
                    return 1;
                }
                if (sceneResult != 1)
                    return 0;
                gameMainState = 3;
                SetUI(exitLevelReturnScene);
                GameRuntime.PlayMusic(ResourceID.AUDIO_BGM_TITLE_MID, true);
                return 0;
            case GameScene.LOAD_LEVEL:
            case (GameScene)15: // load level
                if (sceneResult == 0)
                {
                    GameRuntime.StopMusic();
                    GameRuntime.UnloadResource(ResourceID.GRAPHICS_UIMAINMENU_RES);
                    GameRuntime.UnloadResource(ResourceID.GRAPHICS_UILEVELSELECT_RES);
                    GameRuntime.LoadResource(LEVEL_RESIDS[(int)CurrentLevel]);
                    GameRuntime.LoadResource(LEVEL_RESIDS[CANNON_LEVEL_INDEX]);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_BALLHIGHLIGHT_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_BALLPARTS_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJDOOR_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJLEVER_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJSIGNBOARD_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJFRIEND_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJSTONEWALL_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_UIPAUSEMENU_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_ENEMY00CANDLE_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_ENEMY02MOLE_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJHYPNOTOID_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJSPIKE_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_PARTICLESPLASH_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_PARTICLECOMMON_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_BALLBUMPYCRACKS_RES);
                    GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJEGG_RES);
                    switch (GetLevelType(CurrentLevel))
                    {
                        case 0:
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_LEVELACT01_RES);
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJCOLORMACHINE_RES);
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJCOLORMACHINEBROKEN_RES);
                            break;
                        case 1:
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_LEVELACT02_RES);
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJCOLORMACHINE_RES);
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJCOLORMACHINEBROKEN_RES);
                            break;
                        case 2:
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_LEVELACT03_RES);
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJCANNON_RES);
                            // This resource is used for the hypnotoid beam. However, it isn't properly unloaded when the level ends in the original game.
                            GameRuntime.LoadResource(ResourceID.GRAPHICS_OBJCOLORMACHINE_RES);
                            break;
                    }
                    BallFramebuffer?.Dispose();
                    BallFramebuffer = Image.CreateRenderImage(BounceObject.BALL_DIMENS_SCREENSPACE[0] * 3, BounceObject.BALL_DIMENS_SCREENSPACE[0] * 3);
                    BallGraphics = BallFramebuffer.GetGraphics();
                    BallFramebufferRGB = new Color32[BallFramebuffer.Width * BallFramebuffer.Height];

                    SpriteFB?.Dispose();
                    SpriteFB = Image.CreateRenderImage(112, 26);
                    SpriteOffscreenGraphics = SpriteFB.GetGraphics();
                    SpriteFBRGB = new Color32[SpriteFB.Width * SpriteFB.Height];
                    return 1;
                }
                if (sceneResult != 1)
                    return 0;
                if (!isLevelActive)
                {
                    isLevelActive = true;
                    LevelPaused = false;
                    LoadLevel();
                }
                gameMainState = 4;
                SetIngameHID();
                if (!LevelPaused)
                    GameRuntime.PlayMusic(GetLevelMusicID(), true);
                return 0;
            case GameScene.START_NEW_GAME: // start NG+
                for (int saveIndex = 0; saveIndex < levelSaveData.Length; saveIndex++)
                {
                    if ((saveIndex + 1) % 4 != 0)
                        levelSaveData[saveIndex] = 0;
                }
                UnlockLevel(0);
                selectedLevelId = 0;
                SerializeSaveData();
                isLevelActive = false;
                SetUI(GameScene.MENU_LEVEL_SELECT);
                return 0;
            case (GameScene)21:
            case GameScene.MENU_GUIDE:
                lastMenuOption = ui.GetSelectedOption();
                SetUI(sceneId);
                return 0;
            case (GameScene)27:
                return 0;
            case GameScene.CALL_TITLE_MENU: // go to main menu
                if (sceneResult == 0)
                {
                    GameRuntime.UnloadResource(SPLASH_SCREEN_LAYOUT_RESIDS[^1]);
                    return 1;
                }
                if (sceneResult != 1)
                    return 0;
                gameMainState = 3;
                GameRuntime.PlayMusic(ResourceID.AUDIO_BGM_TITLE_MID, true);
                SetUI(GameScene.MENU_TITLE);
                return 0;
            default:
                return 0;
        }
    }

    private static void LoadLevel()
    {
        GameRuntime.SetUpdatesPerDraw(2);
        GameRuntime.SetMaxUpdateDelta(150 / GameRuntime.GetUpdatesPerDraw());

        // since 2.0.25
        for (int i = 0; i < 5; i++)
            fieldMessageQueue[i] = -1;
        fieldMessagePointer = 0;
        isFieldMessageShowing = false;
        reqReloadFieldMsg = false;

        IsSuperBounceUnlocked = CheckSuperBounceUnlocked();
        GameObject.CameraBounceFactor = 90;
        GameObject.CameraStabilizeSpeed = 140;
        LevelTimer = 0;
        totalGameTime = 0;
        EggCount = 0;
        calcScore = 0;
        isFieldMessageShowing = false;
        WaterSingletonFlag = false;
        isBlockingEvent = false;
        GameObject.SetScreenSpaceMatrixByWindow(GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
        BounceObject.UpdateScreenSpaceConstants();

        byte[] cannonLevel = GameRuntime.GetLoadedResData(LEVEL_RESIDS[CANNON_LEVEL_INDEX]);
        CannonModels = new GameObject[GameObject.ReadShort(cannonLevel, 8)];
        LevelKey cmnKey;
        short cmnObjectId = 0;
        int maxVerticesPerObj = 0;
        int pos = 14;
        while ((cmnKey = (LevelKey)cannonLevel[pos++]) != LevelKey.END)
        {
            short dataSize = GameObject.ReadShort(cannonLevel, pos);
            pos += 2;
            switch (cmnKey)
            {
                case LevelKey.GEOMETRY:
                    GeometryObject geometry = new();
                    geometry.SetObjectId(cmnObjectId);
                    geometry.ReadData(cannonLevel, pos);
                    if (geometry.GetVertexCount() > maxVerticesPerObj)
                        maxVerticesPerObj = geometry.GetVertexCount();
                    CannonModels[cmnObjectId++] = geometry;
                    break;
            }
            pos += dataSize;
        }

        byte[] levelData = GameRuntime.GetLoadedResData(LEVEL_RESIDS[(int)CurrentLevel]);
        if (levelData == null)
            return;

        objectCount = GameObject.ReadShort(levelData, 8);
        levelObjects = new GameObject[objectCount];
        eventCount = GameObject.ReadShort(levelData, 12);
        events = new EventObject[eventCount];

        int currentBytePos = 8 + 6 + 1;
        LevelKey key = (LevelKey)levelData[8 + 6];
        short objID = 0;
        int eventId = 0;
        bonusLevelEggLimit = 0;

        while (key != LevelKey.END)
        {
            short dataLength = GameObject.ReadShort(levelData, currentBytePos);
            int afterHeaderPos = currentBytePos + 2;
            switch (key)
            {
                case LevelKey.GEOMETRY:
                    GeometryObject geometry = new();
                    geometry.SetObjectId(objID);
                    geometry.ReadData(levelData, afterHeaderPos);
                    if (geometry.GetVertexCount() > maxVerticesPerObj)
                        maxVerticesPerObj = geometry.GetVertexCount();
                    levelObjects[objID] = geometry;
                    break;
                case (LevelKey)5:
                case (LevelKey)7:
                default:
                    levelObjects[objID] = GameObject.CreateDummy();
                    levelObjects[objID].SetObjectId(objID);
                    levelObjects[objID].ReadData(levelData, afterHeaderPos);
                    break;
                case LevelKey.EVENT:
                    EventObject objEv = new();
                    objEv.SetObjectId(objID);
                    objEv.ReadData(levelData, afterHeaderPos);
                    levelObjects[objID] = objEv;
                    events[eventId++] = objEv;
                    break;
                case LevelKey.PLAYER:
                    BounceObject player = new(true);
                    BounceObj = player;
                    player.SetObjectId(objID);
                    player.ReadData(levelData, afterHeaderPos);
                    player.Initialize();
                    levelObjects[objID] = player;
                    CheckpointPosX = player.LocalObjectMatrix.TranslationX;
                    CheckpointPosY = player.LocalObjectMatrix.TranslationY;
                    break;
                case LevelKey.SPRITE:
                    levelObjects[objID] = new SpriteObject();
                    levelObjects[objID].SetObjectId(objID);
                    levelObjects[objID].ReadData(levelData, afterHeaderPos);
                    levelObjects[objID].Initialize();
                    break;
                case LevelKey.WATER:
                    WaterObject water = new();
                    water.SetObjectId(objID);
                    water.ReadData(levelData, afterHeaderPos);
                    water.Initialize();
                    if (water.vertexCount > maxVerticesPerObj)
                        maxVerticesPerObj = water.vertexCount;
                    levelObjects[objID] = water;
                    break;
                case LevelKey.CANNON:
                    CannonObject cannon = new();
                    cannon.SetObjectId(objID);
                    cannon.ReadData(levelData, afterHeaderPos);
                    cannon.Initialize();
                    levelObjects[objID] = cannon;
                    break;
                case LevelKey.TRAMPOLINE:
                    TrampolineObject jumpPad = new();
                    jumpPad.SetObjectId(objID);
                    jumpPad.ReadData(levelData, afterHeaderPos);
                    jumpPad.Initialize();
                    levelObjects[objID] = jumpPad;
                    break;
                case LevelKey.EGG:
                    EggObject egg = new();
                    egg.SetObjectId(objID);
                    egg.ReadData(levelData, afterHeaderPos);
                    egg.Initialize();
                    levelObjects[objID] = egg;
                    bonusLevelEggLimit++;
                    break;
                case LevelKey.FRIEND:
                    BounceObject friend = new(false);
                    friend.SetObjectId(objID);
                    friend.ReadData(levelData, afterHeaderPos);
                    friend.Initialize();
                    levelObjects[objID] = friend;
                    break;
                case LevelKey.ENEMY:
                    EnemyObject enemy = new();
                    enemy.SetObjectId(objID);
                    enemy.ReadData(levelData, afterHeaderPos);
                    enemy.Initialize();
                    levelObjects[objID] = enemy;
                    bonusLevelEggLimit++;
                    break;
            }
            objID++;
            int nextKeyIndex = afterHeaderPos + dataLength;
            currentBytePos = nextKeyIndex + 1;
            key = (LevelKey)levelData[nextKeyIndex];
        }

        for (int i = 0; i < levelObjects.Length; i++)
        {
            if (levelObjects[i] == null)
                Trace.WriteLine("Object with ID " + i + " is ABSENT!!");
        }
        GameObject.MakeObjectLinks(levelObjects);

        // BUGFIX: BounceObject physics only work if the object isn't parented to anything
        for (int i = 0; i < levelObjects.Length; i++)
        {
            if (levelObjects[i].GetObjType() == BounceObject.TYPEID)
                levelObjects[i].MakeIndependent();
        }

        RootLevelObj = levelObjects[0];
        levelObjects = null;

        EnemyDeadEgg = new EggObject();
        EnemyDeadEgg.SetObjectId(objID++);
        EnemyDeadEgg.SetParent(RootLevelObj);
        EnemyDeadEgg.LocalObjectMatrix.M00 = LP32.ONE;
        EnemyDeadEgg.LocalObjectMatrix.M01 = 0;
        EnemyDeadEgg.LocalObjectMatrix.TranslationX = int.MaxValue;
        EnemyDeadEgg.LocalObjectMatrix.M10 = 0;
        EnemyDeadEgg.LocalObjectMatrix.M11 = LP32.ONE;
        EnemyDeadEgg.LocalObjectMatrix.TranslationY = int.MaxValue;
        EnemyDeadEgg.RenderCalcMatrix = EnemyDeadEgg.LocalObjectMatrix;
        EnemyDeadEgg.Initialize();

        BubbleParticle.SetObjectId(objID++);
        BubbleParticle.AttachToObject(RootLevelObj);
        WaterSplashParticle.SetObjectId(objID++);
        WaterSplashParticle.AttachToObject(RootLevelObj);
        CannonParticle.SetObjectId(objID++);
        CannonParticle.AttachToObject(RootLevelObj);
        EggCollectParticle.SetObjectId(objID++);
        EggCollectParticle.AttachToObject(RootLevelObj);
        EnemyDeathParticle.SetObjectId(objID++);
        EnemyDeathParticle.AttachToObject(RootLevelObj);
        WinParticle.SetObjectId(objID++);
        WinParticle.AttachToObject(RootLevelObj);
        SuperBounceParticle.SetObjectId(objID++);
        SuperBounceParticle.AttachToObject(RootLevelObj);
        ColorMachineDestroyParticle.SetObjectId(objID++);
        ColorMachineDestroyParticle.AttachToObject(RootLevelObj);
        AirTunnelParticle.SetObjectId(objID++);
        AirTunnelParticle.AttachToObject(RootLevelObj);

        GameObject.AllocateRenderPool(objID); // okay to be a bit much, it's just a pointer array
        //GeometryObject.TEMP_QUAD_XS = new int[maxVerticesPerObj];
        //GeometryObject.TEMP_QUAD_YS = new int[maxVerticesPerObj];
        EventObject.EventVars = new int[72];
        CurrentPlayerState = PlayerState.PLAY;
        CurrentControllerState = Controller.NORMAL;
        EventObject.EventVars[2] = 0;
        EventObject.EventVars[7] = 0;
        GameRuntime.UnloadResource(LEVEL_RESIDS[(int)CurrentLevel]);
        GameRuntime.UnloadResource(LEVEL_RESIDS[CANNON_LEVEL_INDEX]);
        GameObject.CameraTarget = BounceObj;
        GameObject.SnapCameraToTarget();
        f240F = GameObject.CameraMatrix.TranslationY;
        InitStolenColorData(); // inlined in 2.0.25
        WaterSplashParticle.ParticleCount = -1;
        BubbleParticle.ParticleCount = -1;
        CannonParticle.ParticleCount = -1;
        EggCollectParticle.ParticleCount = -1;
        EnemyDeathParticle.ParticleCount = -1;
        WinParticle.ParticleCount = -1;
        SuperBounceParticle.ParticleCount = -1;
        ColorMachineDestroyParticle.ParticleCount = -1;
        AirTunnelParticle.ParticleCount = -1;
        BounceObj.FadeColor = Color32.Black;
        bool noBonusLevelsBeaten = true;
        for (int bonusLevelIdx = 0; bonusLevelIdx < BONUS_LEVEL_INFO.Length; bonusLevelIdx += 2)
        {
            if (WasLevelBeaten((LevelID)BONUS_LEVEL_INFO[bonusLevelIdx]))
                noBonusLevelsBeaten = false;
        }
        if (noBonusLevelsBeaten && IsBonusLevel(CurrentLevel))
            PushFieldMessage(MessageID.GUIDE_TEXT_3); // bonus chapter guide
        BounceObj.EyeFrame = 0;
        BounceObj.IdleAnimStartTimer = 3000;
    }

    public void HandleKeyPress(KeyCode keyCode)
    {
        if (drawUI != null)
        {
            UILayout uiLayout = drawUI;
            if (uiLayout.UIID == GameScene.MENU_LEVEL_SELECT)
            {
                // Stuff...
                switch (keyCode)
                {
                    case KeyCode.LEFT:
                        CycleLevelSelectLeft(uiLayout, true);
                        break;
                    case KeyCode.RIGHT:
                        CycleLevelSelectRight(uiLayout, true);
                        break;
                }
            }
            uiLayout.HandleKeyCode(keyCode);
        }
        else if (gameMainState == 2)
            UpdateLoadingScreen();
        else if (gameMainState == 4)
        {
            // Stuff...
            switch (keyCode)
            {
                case KeyCode.SOFTKEY_RIGHT:
                    LevelPaused = true;
                    break;
                case KeyCode.STAR:
                    if (CurrentControllerState == Controller.NORMAL && CurrentPlayerState != PlayerState.LOSE_UPDATE)
                        BounceObj.CycleForme();
                    break;
            }
        }
    }

    public void OnSystemEvent(GameRuntime.SystemEvent eventId)
    {
        if (eventId == GameRuntime.SystemEvent.START)
        {
            GameRuntime.LoadResource(ResourceID.GRAPHICS_UIARROWS_RES); // game UI
            GameRuntime.LoadResource(ResourceID.GRAPHICS_UILEVELSTATS_RES);
            GameRuntime.LoadResource(ResourceID.GRAPHICS_UINUMBERFONT_RES);
            GameRuntime.LoadResource(ResourceID.LAYOUT_MENULAYOUTA_RES);
            GameRuntime.LoadResource(ResourceID.LAYOUT_INFOLAYOUT_RES);
            GameRuntime.LoadResource(-1);
            GameRuntime.LoadResource(-2);
            GameRuntime.LoadResource(-3);
            GameRuntime.StartLoadScene(GameScene.ENTRYPOINT);
        }
        else if (eventId == GameRuntime.SystemEvent.PAUSE && gameMainState == 4)
        {
            LevelPaused = true;
            if (isFieldMessageShowing)
            {
                reqReloadFieldMsg = true;
                isFieldMessageShowing = false;
                SetIngameHID();
            }
            else if (!reqReloadFieldMsg)
                lastFieldMsg = null;
        }
        else if (eventId == GameRuntime.SystemEvent.RESIZE) // added for resizing support
        {
            if (GameRuntime.CurrentWidth > 0 && GameRuntime.CurrentHeight > 0)
            {
                renderClipWidth = GameRuntime.CurrentWidth;
                renderClipHeight = GameRuntime.CurrentHeight;
                GameObject.SetScreenSpaceMatrixByWindow(GameRuntime.CurrentWidth, GameRuntime.CurrentHeight);
                BounceObject.UpdateScreenSpaceConstants();
            }
        }
    }

    public void Shutdown()
    {
        if (isLevelActive)
        {
            ResetParallaxStolenColors();
            UnloadLevel();
            isLevelActive = false;
        }
    }

    public void ChangeScene(GameScene sceneId)
    {
        if (sceneId != GameScene.MENU_TITLE && sceneId != GameScene.MENU_PAUSE)
            lastMenuOption = 0;
        switch (sceneId)
        {
            case GameScene.ENTRYPOINT: // not present in original game, added for debug
            case GameScene.INIT:
            case GameScene.EXIT_LEVEL:
            case GameScene.LOAD_LEVEL:
            case GameScene.START_NEW_GAME:
            case (GameScene)15:
            case (GameScene)21:
            case GameScene.MENU_GUIDE:
            case (GameScene)27:
                GameRuntime.StartLoadScene(sceneId);
                break;
            case GameScene.MENU_HIGH_SCORES:
            case GameScene.MENU_NEW_GAME:
            case GameScene.CONFIRM_RESTART_LEVEL:
            case GameScene.CONFIRM_RETURN_LEVEL_SELECT:
            case GameScene.CONFIRM_EXIT_LEVEL:
                lastMenuOption = ui.GetSelectedOption();
                goto case GameScene.MENU_TITLE;
            case GameScene.MENU_TITLE:
            case GameScene.MENU_LEVEL_SELECT:
            case (GameScene)19:
            case (GameScene)23:
            case GameScene.CONFIRM_QUIT_GAME:
            case GameScene.MENU_PAUSE:
            case (GameScene)26:
            case GameScene.INFO_CHAPTER_COMPLETE:
            case GameScene.INFO_GAME_BEATEN:
            case GameScene.INFO_GAME_COMPLETED:
                SetUI(sceneId);
                break;
            case GameScene.QUIT_GAME:
                GameRuntime.Quit();
                break;
            case GameScene.CLOSE_FIELD_MESSAGE: // field message advance
                isFieldMessageShowing = false;
                SetIngameHID();
                if (reqQuitLevelAfterFieldMessage)
                {
                    reqQuitLevelAfterFieldMessage = false;
                    LevelEnded();
                }
                break;
            case (GameScene)13:
                break;
            case GameScene.RESTART_LEVEL: // restart level
                ResetParallaxStolenColors();
                goto case GameScene.ENTER_LEVEL; // fall through
            case GameScene.ENTER_LEVEL:
                if (IsLevelUnlocked(selectedLevelId)) // enter level
                {
                    bookAnimationTime = targetBookAnimationTime;
                    isLevelActive = false;
                    CurrentLevel = selectedLevelId;
                    GameRuntime.StartLoadScene(GameScene.LOAD_LEVEL); // load level
                }
                break;
            case GameScene.UNPAUSE_LEVEL: // unpause
                LevelPaused = false;
                SetIngameHID();
                if (!LevelPaused)
                    GameRuntime.PlayMusic(GetLevelMusicID(), true);
                break;
            case GameScene.OPEN_MORE_GAMES_URL: // since 2.0.25
                reqPlayTitleMusic = true;
                GameRuntime.StopMusic();
                try
                {
                    //if (GameRuntime.mMidLet.platformRequest(moreGamesURL))
                    reqQuit = true;
                }
                catch (Exception)
                {
                }
                break;
        }
    }
}
