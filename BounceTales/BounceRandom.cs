namespace BounceTales;

// https://github.com/openjdk/jdk/blob/master/src/java.base/share/classes/java/util/Random.java
public sealed class BounceRandom(long seed)
{
    private const long multiplier = 0x5DEECE66DL;
    private const long addend = 0xBL;
    private const long mask = (1L << 48) - 1;

    private long _seed = InitialScramble(seed);

    public BounceRandom() : this(Environment.TickCount64)
    {
    }

    private static long InitialScramble(long seed)
    {
        return (seed ^ multiplier) & mask;
    }

    public void SetSeed(long seed)
    {
        _seed = InitialScramble(seed);
    }

    public int NextInt()
    {
        return Next(32);
    }

    private int Next(int bits)
    {
        long nextSeed = _seed * multiplier + addend & mask;
        _seed = nextSeed;
        return (int)(nextSeed >>> 48 - bits);
    }
}
