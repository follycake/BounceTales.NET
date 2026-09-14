using System.Diagnostics;
using BounceTales.Platform;

namespace BounceTales;

public static class StringManager
{
    public static readonly string[] LocaleList =
    [
        "af-ZA",
        "am-ET",
        "ar",
        "as-IN",
        "az-AZ",
        "be",
        "bg-BG",
        "bn",
        "bn-BD",
        "bs-BA",
        "ca",
        "cs-CZ",
        "da-DK",
        "de",
        "el-GR",
        "en-US",
        "es-ES",
        "es-US",
        "et-EE",
        "eu",
        "fa",
        "fi-FI",
        "fr",
        "fr-CA",
        "gl",
        "gu-IN",
        "ha",
        "he-IL",
        "hi-IN",
        "hr-HR",
        "hu-HU",
        "hy",
        "id-ID",
        "ig-NG",
        "is-IS",
        "it",
        "ka-GE",
        "kk-KZ",
        "km-KH",
        "kn-IN",
        "ks-IN",
        "ky-KG",
        "ln",
        "lt-LT",
        "lv-LV",
        "mk-MK",
        "ml-IN",
        "mn-MN",
        "mr-IN",
        "ms-MY",
        "nl-NL",
        "no-NO",
        "or-IN",
        "pa",
        "pl-PL",
        "ps",
        "pt-BR",
        "pt-PT",
        "ro-RO",
        "ru-RU",
        "si-LK",
        "sk-SK",
        "sl-SI",
        "sq",
        "sr-YU",
        "st",
        "sv",
        "sw",
        "ta",
        "te-IN",
        "tg-TJ",
        "th-TH",
        "tk",
        "tl-PH",
        "tr-TR",
        "uk-UA",
        "ur",
        "uz-UZ",
        "vi-VN",
        "xh",
        "xx",
        "yo",
        "zh-CN",
        "zh-HK"
    ];
    
    // Removed mInstance and textReader.
    // private static string localeProperty;
    // Removed platform checking, which we do not care about.

    private static byte[] cachedLang;

    public static void Reset()
    {
        cachedLang = null;
    }

    public static string GetMessage(int msgId)
    {
        return GetMessage(msgId, null);
    }

    public static string GetMessage(int msgId, int iparam)
    {
        return GetMessage(msgId, (object)iparam);
    }

    private static readonly string[] tempParam = new string[1];

    public static string GetMessage(int msgId, object param)
    {
        lock (tempParam)
        {
            tempParam[0] = param.ToString();
            return GetMessage(msgId, tempParam);
        }
    }

    public static string GetMessage(int msgId, string[] variables)
    {
        ISystemProvider system = GameRuntime.MidLet.System;
        int offset = 0;
        try
        {
            if (cachedLang == null)
            {
                using Stream langRscStrm = system.GetResourceAsStream("/lang." + system.Locale) ?? system.GetResourceAsStream("/lang.xx");
                if (langRscStrm == null)
                    return "X";
                cachedLang = new byte[langRscStrm.Length];
                langRscStrm.ReadExactly(cachedLang);
            }
            using MemoryStream cachedLangRscStrm = new(cachedLang);
            using DataInputStream textReader = new(cachedLangRscStrm);
            msgId = MessageID.CONST_MESSAGE_MAP[msgId];
            textReader.SkipBytes(msgId * 2);
            // skip to actual message offset
            offset = textReader.ReadUnsignedShort();
            textReader.SkipBytes(offset - msgId * 2 - 2);
            string message = textReader.ReadUTF();
            if (variables != null)
            {
                if (variables.Length == 1)
                    message = FindAndReplace(message, "%U", variables[0]);
                else
                {
                    for (int i = 0; i < variables.Length; i++)
                        message = FindAndReplace(message, "%" + i + "U", variables[i]);
                }
            }
            return message;
        }
        catch (IOException e2)
        {
            Debug.WriteLine(e2);
            //return "E"; // in 2.0.3
            return "E:" + offset; // since 2.0.25
        }
    }

    private static string FindAndReplace(string str, string toFind, string str3)
    {
        int indexOf = str.IndexOf(toFind, StringComparison.InvariantCulture);
        while (indexOf >= 0)
        {
            str = string.Concat(str.AsSpan(0, indexOf), str3, str.AsSpan(toFind.Length + indexOf));
            indexOf = str.IndexOf(toFind, StringComparison.InvariantCulture);
        }
        return str;
    }
}
