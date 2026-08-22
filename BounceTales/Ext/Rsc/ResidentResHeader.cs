namespace BounceTales.Ext.Rsc;

public class ResidentResHeader(IDataInput input)
{
    public short Type = input.ReadShort();
    public short ResId = input.ReadShort();
}
