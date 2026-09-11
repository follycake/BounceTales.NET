using System.Diagnostics;
using BounceTales.Platform;

namespace BounceTales;

public static class StringManager
{
    // Removed mInstance and textReader.
    private static string localeProperty;
    // Removed platform checking, which we do not care about.

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
        localeProperty ??= system.Locale;
        int offset = 0;
        try
        {
            // Removed stream caching.
            using Stream langRscStrm = system.GetResourceAsStream("/lang." + localeProperty) ?? system.GetResourceAsStream("/lang.xx");
            if (langRscStrm == null)
                return "X";
            using DataInputStream textReader = new(langRscStrm);
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
