namespace BounceTales;

public interface IDataInput
{
    sbyte ReadByte();
    short ReadShort();
    int ReadInt();
    string ReadUTF();
    int SkipBytes(int n);
}

// https://github.com/openjdk-mirror/jdk7u-jdk/blob/master/src/share/classes/java/io/DataInputStream.java
public sealed class DataInputStream(Stream stream) : IDataInput, IDisposable
{
    public Stream Stream { get; } = stream;

    public sbyte ReadByte()
    {
        int ch = Stream.ReadByte();
        if (ch < 0)
            throw new EndOfStreamException();
        return (sbyte)ch;
    }

    public int ReadInt()
    {
        int ch1 = Stream.ReadByte();
        int ch2 = Stream.ReadByte();
        int ch3 = Stream.ReadByte();
        int ch4 = Stream.ReadByte();
        if ((ch1 | ch2 | ch3 | ch4) < 0)
            throw new EndOfStreamException();
        return (ch1 << 24) + (ch2 << 16) + (ch3 << 8) + (ch4 << 0);
    }

    public short ReadShort()
    {
        int ch1 = Stream.ReadByte();
        int ch2 = Stream.ReadByte();
        if ((ch1 | ch2) < 0)
            throw new EndOfStreamException();
        return (short)((ch1 << 8) + (ch2 << 0));
    }

    public ushort ReadUnsignedShort()
    {
        int ch1 = Stream.ReadByte();
        int ch2 = Stream.ReadByte();
        if ((ch1 | ch2) < 0)
            throw new EndOfStreamException();
        return (ushort)((ch1 << 8) + (ch2 << 0));
    }

    public string ReadUTF()
    {
        int utflen = ReadUnsignedShort();
        byte[] bytearr = new byte[utflen];
        char[] chararr = new char[utflen];
        Stream.ReadExactly(bytearr);

        int c, char2, char3;
        int count = 0;
        int chararrCount = 0;
        while (count < utflen)
        {
            c = bytearr[count] & 0xFF;
            if (c > 127)
                break;
            count++;
            chararr[chararrCount++] = (char)c;
        }

        while (count < utflen)
        {
            c = bytearr[count] & 0xFF;
            switch (c >> 4)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                    count++;
                    chararr[chararrCount++] = (char)c;
                    break;
                case 12:
                case 13:
                    count += 2;
                    if (count > utflen)
                        throw new InvalidDataException("Malformed input: partial character at end");
                    char2 = bytearr[count - 1];
                    if ((char2 & 0xC0) != 0x80)
                        throw new InvalidDataException("Malformed input around byte " + count);
                    chararr[chararrCount++] = (char)((c & 0x1F) << 6 | char2 & 0x3F);
                    break;
                case 14:
                    count += 3;
                    if (count > utflen)
                        throw new InvalidDataException("Malformed input: partial character at end");
                    char2 = bytearr[count - 2];
                    char3 = bytearr[count - 1];
                    if ((char2 & 0xC0) != 0x80 || (char3 & 0xC0) != 0x80)
                        throw new InvalidDataException("Malformed input around byte " + (count - 1));
                    chararr[chararrCount++] = (char)((c & 0x0F) << 12 | (char2 & 0x3F) << 6 | (char3 & 0x3F) << 0);
                    break;
                default:
                    throw new InvalidDataException("Malformed input around byte " + count);
            }
        }

        return new string(chararr, 0, chararrCount);
    }

    public int SkipBytes(int n)
    {
        long start = Stream.Position;
        long pos = Stream.Seek(n, SeekOrigin.Current);
        return (int)(pos - start);
    }

    public void Dispose()
    {
        Stream.Dispose();
    }
}
