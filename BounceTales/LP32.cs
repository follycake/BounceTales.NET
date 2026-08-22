namespace BounceTales;

public static class LP32
{
    public const int ONE = 65536;

    public const float LP32_SCALE = 65536.0f;
    public const double LP32_SCALE_D = 65536.0d;

    public static float LP32ToFP32(int lp32)
    {
        return lp32 / LP32_SCALE;
    }

    public static int FP32ToLP32(float fp32)
    {
        return (int)(fp32 * LP32_SCALE);
    }

    public static int FP64ToLP32(double fp64)
    {
        return (int)(fp64 * LP32_SCALE_D);
    }

    public static int Int32ToLP32(int int32)
    {
        return int32 << 16;
    }
}
