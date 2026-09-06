namespace BounceTales.Ext.Rsc;

public readonly struct ResidentResHeader(IDataInput input)
{
    public readonly short Type = input.ReadShort();
    public readonly short ResId = input.ReadShort();
}
