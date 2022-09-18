namespace ImageIngest.Functions.Extensions;

public static class LongExtensions
{
    public static long Bytes2Megabytes(this long value)
    {
        return value / (1024 * 1024);
    }
    public static long Megabytes2Bytes(this long value)
    {
        return value * (1024 * 1024);
    }

}