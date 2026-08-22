namespace BounceTales.Ext.Rsc;

public class ImageMapEx
{
    public short ResBatchId;
    public short Offset;

    public ImageMapEx()
    {
        Clear();
    }

    public void Clear()
    {
        ResBatchId = -1;
        Offset = -1;
    }

    public void Read(int resBatchId, IDataInput dis)
    {
        ResBatchId = (short)resBatchId;
        Offset = (short)(dis.ReadShort() + 4);
    }
}
