namespace BounceTales.Ext.Rsc;

public class ResourceInfo
{
    public string ResourcePath;
    public int SkipOffset;
    public int ReadLength;

    public ResourceInfo()
    {
    }

    public ResourceInfo(IDataInput input) : this(input, false)
    {
    }

    public ResourceInfo(IDataInput input, bool readPath)
    {
        if (readPath)
            ResourcePath = input.ReadUTF();
        SkipOffset = input.ReadInt();
        ReadLength = input.ReadInt();
    }

    public bool Exists()
    {
        return ReadLength != 0;
    }
}
