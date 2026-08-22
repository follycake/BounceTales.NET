namespace BounceTales.Ext.Rsc;

public class ResourceBatch
{
    public ResourceType ResType;
    public short MainResId;
    public short[] SubResIds;

    public ResourceBatch(IDataInput input)
    {
        ResType = (ResourceType)input.ReadByte();
        int count = input.ReadByte();
        MainResId = input.ReadShort();
        SubResIds = new short[count];
        for (int i = 0; i < count; i++)
            SubResIds[i] = input.ReadShort();
    }
}
