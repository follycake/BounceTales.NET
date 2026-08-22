namespace BounceTales.Ext.Rsc;

public class ImageMap
{
    public enum Param
    {
        WIDTH = 0,
        HEIGHT = 1,
        ORIGIN_X = 2,
        ORIGIN_Y = 3,
        ATLAS_X = 4,
        ATLAS_Y = 5,
        IMAGE_ID = 6
    }

    public sbyte Width;
    public sbyte Height;
    public sbyte OriginX;
    public sbyte OriginY;

    public sbyte AtlasX;
    public sbyte AtlasY;
    public sbyte ImageId;

    public ImageMap()
    {
        Clear();
    }

    public void Clear()
    {
        Width = -1;
        Height = -1;
        OriginX = -1;
        OriginY = -1;
        AtlasX = -1;
        AtlasY = -1;
        ImageId = -1;
    }

    public void Read(IDataInput dis)
    {
        Width = dis.ReadByte();
        Height = dis.ReadByte();
        OriginX = dis.ReadByte();
        OriginY = dis.ReadByte();
        AtlasX = dis.ReadByte();
        AtlasY = dis.ReadByte();
        ImageId = dis.ReadByte();
    }

    public int GetParam(Param paramId) => paramId switch
    {
        Param.ATLAS_X => AtlasX & 0xFF,
        Param.ATLAS_Y => AtlasY & 0xFF,
        Param.WIDTH => Width & 0xFF,
        Param.HEIGHT => Height & 0xFF,
        Param.ORIGIN_X => OriginX,
        Param.ORIGIN_Y => OriginY,
        Param.IMAGE_ID => ImageId & 0xFF,
        _ => throw new ArgumentException("Param wrong " + paramId),
    };
}
