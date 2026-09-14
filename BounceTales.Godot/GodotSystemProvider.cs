using BounceTales.Platform;
using Godot;
using System.IO;
using FileAccess = Godot.FileAccess;

namespace BounceTales.Godot;

public sealed class GodotSystemProvider : ISystemProvider
{
    public const string JarPath = "user://game.jar";
    public const string SavePath = "user://save.bin";
    
    public string Locale { get; set; } = "en-US";
    public bool EnableCheats { get; set; }
    public bool DebugOverlay { get; set; }
    public bool ObjectDrawDebug { get; set; }
    
    private ulong _startMsec;
    
    public void Initialize()
    {
        _startMsec = Time.GetTicksMsec();
    }
    
    public long CurrentTimeMillis()
    {
        return (long)(Time.GetTicksMsec() - _startMsec);
    }
    
    public byte[] LoadGameData()
    {
        return FileAccess.FileExists(SavePath) ? FileAccess.GetFileAsBytes(SavePath) : null;
    }
    
    public void SaveGameData(byte[] saveData)
    {
        string dir = SavePath.GetBaseDir();
        if (!string.IsNullOrEmpty(dir) && !DirAccess.DirExistsAbsolute(dir))
            DirAccess.MakeDirRecursiveAbsolute(dir);
        using FileAccess f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        f.StoreBuffer(saveData);
    }
    
    public Stream GetResourceAsStream(string path)
    {
        path = path.TrimStart('/', '\\');
        if (!FileAccess.FileExists(JarPath))
            return null;
        using ZipReader reader = new();
        if (reader.Open(JarPath) != Error.Ok)
            return null;
        return new MemoryStream(reader.ReadFile(path, false));
    }
    
    public void Dispose()
    {
    }
}
