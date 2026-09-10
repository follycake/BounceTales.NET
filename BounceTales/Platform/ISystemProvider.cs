using System.Diagnostics;
using System.IO.Compression;

namespace BounceTales.Platform;

public interface ISystemProvider : IProvider
{
    string Locale { get; }
    long CurrentTimeMillis();
    byte[] LoadGameData();
    void SaveGameData(byte[] saveData);
    Stream GetResourceAsStream(string path);
}

public class DefaultSystemProvider : ISystemProvider
{
    public string DataPath { get; set; } = "data/";
    public string JarPath { get; set; } = "game.jar";
    public string SavePath { get; set; } = "save.bin";
    public string Locale { get; set; } = "en-US";
    private Stopwatch _stopwatch;

    public void Initialize()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public long CurrentTimeMillis()
    {
        return _stopwatch.ElapsedMilliseconds;
    }

    public byte[] LoadGameData()
    {
        return File.Exists(SavePath) ? File.ReadAllBytes(SavePath) : null;
    }

    public void SaveGameData(byte[] saveData)
    {
        string dir = Path.GetDirectoryName(SavePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(SavePath, saveData);
    }

    public Stream GetResourceAsStream(string path)
    {
        path = path.TrimStart('/', '\\');
        if (Directory.Exists(DataPath))
        {
            path = Path.Combine(DataPath, path);
            if (!File.Exists(path))
                return null;
            return File.OpenRead(path);
        }
        if (!File.Exists(JarPath))
            return null;
        using ZipArchive zip = ZipFile.OpenRead(JarPath);
        ZipArchiveEntry entry = zip.GetEntry(path);
        if (entry == null)
            return null;
        MemoryStream stream = new();
        using (Stream zipStream = entry.Open())
            zipStream.CopyTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _stopwatch.Stop();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DefaultSystemProvider()
    {
        Dispose(false);
    }
}
