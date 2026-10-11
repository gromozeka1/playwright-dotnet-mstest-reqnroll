namespace Framework.Helpers;

public static class TimeConverter
{
    public static float? ToMilliseconds(float? seconds) => seconds * 1000;
}
