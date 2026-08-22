namespace BounceTales;

public static class MessageID
{
    public const short UI_SELECT = 0;
    public const short UI_LEAVE = 1;
    public const short UI_BACK = 2;
    public const short UI_OK = 3;
    public const short UI_YES = 4;
    public const short UI_NO = 5;
    public const short UI_QUIT = 6;
    public const short UI_NEW_GAME = 7;
    public const short DIALOG_NEW_GAME = 8;
    public const short UI_HIGH_SCORES = 9;
    public const short UI_GUIDE = 10;
    public const short UI_CONTINUE = 11;
    public const short EMPTY = 12;
    public const short GUIDE_TEXT_1 = 13;
    public const short GUIDE_TEXT_2 = 14;
    public const short GUIDE_TEXT_3 = 15;
    public const short GUIDE_TEXT_4 = 16;
    public const short GUIDE_TEXT_5 = 17;
    public const short DIALOG_PAUSE_MENU = 18;
    public const short DIALOG_QUIT_GAME = 19;
    public const short UI_RESTART_LEVEL = 20;
    public const short DIALOG_RESTART_LEVEL = 21;
    public const short UI_CONTINUE_LEVEL = 22;
    public const short UI_RETURN_LEVEL_SELECT = 23;
    public const short DIALOG_RETURN_LEVEL_SELECT = 24;
    public const short GAME_PROGRESS_WILL_BE_LOST = 25;
    public const short LEVEL_PROGRESS_WILL_BE_LOST = 26;
    public const short UI_CHAPTERNO_STD = 27;
    public const short UI_CHAPTERNO_BONUS = 28;
    public const short NEED_COLLECT_COUNT = 29;
    public const short BONUS_CHAPTER_UNLOCKED = 30;
    public const short LEVEL_MISTY_MORNING = 31;
    public const short LEVEL_UNFRIENDLY_FRIENDS = 32;
    public const short LEVEL_SEEKING_ANSWERS = 33;
    public const short LEVEL_SECRET_STALKWAY = 34;
    public const short LEVEL_BUMPY_CRACKS = 35;
    public const short LEVEL_INTO_THE_MINES = 36;
    public const short LEVEL_A_GLOOMY_PATH = 37;
    public const short LEVEL_RUMBLING_SOUNDS = 38;
    public const short LEVEL_TUNNEL_OF_TREASURES = 39;
    public const short LEVEL_TRAPPED_IN_MACHINE = 40;
    public const short LEVEL_WICKED_CIRCUS = 41;
    public const short LEVEL_HUNTING_COLOURS = 42;
    public const short LEVEL_ALMOST_THERE = 43;
    public const short LEVEL_FANTASTIC_FAIR = 44;
    public const short LEVEL_FINAL_RIDE = 45;
    public const short CHAPTER_COMPLETE = 46;
    public const short DIALOG_GAME_BEATEN = 47;
    public const short DIALOG_GAME_COMPLETED = 48;
    public const short ALL_LEVELS_BEATEN = 49;
    public const short ALL_LEVELS_COMPLETED = 50;
    public const short TIMER_CHALLENGE = 51;
    public const short COLLECTION_CHALLENGE = 52;
    public const short NO_MEDALS_WON = 53;
    public const short BONUS_CHAPTER_UNLOCKED_DESC = 54;
    public const short NEW_FORME_UNLOCKED = 55;
    public const short NEW_HIGH_SCORE = 56;
    public const short SCORE = 57;
    public const short SCENARIO_L01E01M01 = 58;
    public const short SCENARIO_L01E01M02 = 59;
    public const short SCENARIO_L01E01M03 = 60;
    public const short SCENARIO_L01E02M01 = 61;
    public const short SCENARIO_L01E03M01 = 62;
    public const short SCENARIO_L01E03M02 = 63;
    public const short SCENARIO_L01E03M03 = 64;
    public const short SCENARIO_L01E04M01 = 65;
    public const short SCENARIO_L01E04M02 = 66;
    public const short SCENARIO_L01E04M03 = 67;
    public const short SCENARIO_L02E01M01 = 68;
    public const short SCENARIO_L02E01M02 = 69;
    public const short SCENARIO_L04E01M01 = 70;
    public const short SCENARIO_L04E01M02 = 71;
    public const short SCENARIO_L04E02M01 = 72;
    public const short SCENARIO_L04E03M01 = 73;
    public const short SCENARIO_L04E03M02 = 74;
    public const short SCENARIO_L04E03M03 = 75;
    public const short SCENARIO_L04E03M04 = 76;
    public const short SCENARIO_L05_UNUSED = 77;
    public const short SCENARIO_L08E01M01 = 78;
    public const short SCENARIO_L08E01M02 = 79;
    public const short SCENARIO_L08E01M03 = 80;
    public const short SCENARIO_L08E02M01 = 81;
    public const short SCENARIO_L11_UNUSED = 82;
    public const short SCENARIO_L12E01M01 = 83;
    public const short SCENARIO_L12E01M02 = 84;
    public const short SCENARIO_L12E02M01 = 85;
    public const short SCENARIO_L12E02M02 = 86;
    public const short SCENARIO_L12E02M03 = 87;
    public const short MM_SS = 88;
    public const short GAME_TITLE = 89;
    public const short IS_TEXT_RIGHT_TO_LEFT_RESERVED = 90;
    public const short UI_MORE_GAMES = -1;

    public static readonly short[] MESSAGE_MAP_2_0_3 = [48, 44, 43, 46, 49, 45, 47, 36, 84, 19, 18, 17, 4, 12, 13, 11, 14, 15, 83, 80, 37, 85, 38, 39, 87, 2, 3, 35, 16, 0, 1, 20, 24, 25, 32, 26, 27, 28, 29, 33, 30, 31, 21, 22, 34, 23, 86, 81, 82, 5, 6, 8, 7, 41, 9, 10, 40, 42, 51, 52, 53, 50, 54, 55, 56, 57, 58, 59, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 60, 61, 62, 63, 64, 88, 89, 90];
    public static readonly short[] MESSAGE_MAP_2_0_25 = [1, 47, 48, 45, 43, 46, 44, 55, 8, 72, 73, 74, 0, 87, 79, 78, 80, 77, 76, 9, 12, 54, 7, 53, 52, 5, 89, 88, 56, 75, 91, 90, 71, 67, 66, 59, 65, 64, 63, 62, 58, 61, 60, 70, 69, 57, 68, 6, 11, 10, 86, 85, 83, 84, 50, 82, 81, 51, 49, 41, 40, 39, 42, 38, 37, 36, 35, 34, 33, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 32, 31, 30, 29, 28, 4, 3, 2];

    public static readonly short[] CONST_MESSAGE_MAP = [1, 47, 48, 45, 43, 46, 44, 55, 8, 72, 73, 74, 87, 79, 78, 80, 77, 76, 9, 12, 54, 7, 53, 52, 5, 89, 88, 56, 75, 91, 90, 71, 67, 66, 59, 65, 64, 63, 62, 58, 61, 60, 70, 69, 57, 68, 6, 11, 10, 86, 85, 83, 84, 50, 82, 81, 51, 49, 41, 40, 39, 42, 38, 37, 36, 35, 34, 33, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 32, 31, 30, 29, 28, 4, 3, 2];
    public static readonly short[] SCRIPT_MESSAGE_MAP =
    [
        UI_SELECT,
        UI_LEAVE,
        UI_BACK,
        UI_OK,
        UI_YES,
        UI_NO,
        UI_QUIT,
        UI_NEW_GAME,
        DIALOG_NEW_GAME,
        UI_HIGH_SCORES,
        UI_GUIDE,
        UI_CONTINUE,

        UI_MORE_GAMES,

        EMPTY,
        GUIDE_TEXT_1,
        GUIDE_TEXT_2,
        GUIDE_TEXT_3,
        GUIDE_TEXT_4,
        GUIDE_TEXT_5,
        DIALOG_PAUSE_MENU,
        DIALOG_QUIT_GAME,
        UI_RESTART_LEVEL,
        DIALOG_RESTART_LEVEL,
        UI_CONTINUE_LEVEL,
        UI_RETURN_LEVEL_SELECT,
        DIALOG_RETURN_LEVEL_SELECT,
        GAME_PROGRESS_WILL_BE_LOST,
        LEVEL_PROGRESS_WILL_BE_LOST,
        UI_CHAPTERNO_STD,
        UI_CHAPTERNO_BONUS,
        NEED_COLLECT_COUNT,
        BONUS_CHAPTER_UNLOCKED,
        LEVEL_MISTY_MORNING,
        LEVEL_UNFRIENDLY_FRIENDS,
        LEVEL_SEEKING_ANSWERS,
        LEVEL_SECRET_STALKWAY,
        LEVEL_BUMPY_CRACKS,
        LEVEL_INTO_THE_MINES,
        LEVEL_A_GLOOMY_PATH,
        LEVEL_RUMBLING_SOUNDS,
        LEVEL_TUNNEL_OF_TREASURES,
        LEVEL_TRAPPED_IN_MACHINE,
        LEVEL_WICKED_CIRCUS,
        LEVEL_HUNTING_COLOURS,
        LEVEL_ALMOST_THERE,
        LEVEL_FANTASTIC_FAIR,
        LEVEL_FINAL_RIDE,
        CHAPTER_COMPLETE,
        DIALOG_GAME_BEATEN,
        DIALOG_GAME_COMPLETED,
        ALL_LEVELS_BEATEN,
        ALL_LEVELS_COMPLETED,
        TIMER_CHALLENGE,
        COLLECTION_CHALLENGE,
        NO_MEDALS_WON,
        BONUS_CHAPTER_UNLOCKED_DESC,
        NEW_FORME_UNLOCKED,
        NEW_HIGH_SCORE,
        SCORE,
        SCENARIO_L01E01M01,
        SCENARIO_L01E01M02,
        SCENARIO_L01E01M03,
        SCENARIO_L01E02M01,
        SCENARIO_L01E03M01,
        SCENARIO_L01E03M02,
        SCENARIO_L01E03M03,
        SCENARIO_L01E04M01,
        SCENARIO_L01E04M02,
        SCENARIO_L01E04M03,
        SCENARIO_L02E01M01,
        SCENARIO_L02E01M02,
        SCENARIO_L04E01M01,
        SCENARIO_L04E01M02,
        SCENARIO_L04E02M01,
        SCENARIO_L04E03M01,
        SCENARIO_L04E03M02,
        SCENARIO_L04E03M03,
        SCENARIO_L04E03M04,
        SCENARIO_L05_UNUSED,
        SCENARIO_L08E01M01,
        SCENARIO_L08E01M02,
        SCENARIO_L08E01M03,
        SCENARIO_L08E02M01,
        SCENARIO_L11_UNUSED,
        SCENARIO_L12E01M01,
        SCENARIO_L12E01M02,
        SCENARIO_L12E02M01,
        SCENARIO_L12E02M02,
        SCENARIO_L12E02M03,
        MM_SS,
        GAME_TITLE,
        IS_TEXT_RIGHT_TO_LEFT_RESERVED,
        UI_MORE_GAMES
    ];
}
