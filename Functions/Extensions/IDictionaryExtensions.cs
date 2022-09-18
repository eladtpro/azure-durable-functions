namespace ImageIngest.Functions.Extensions;

public static class IDictionaryExtensions
{
    public static T GetValue<T>(this IDictionary<string, string> dictionary, string key, T defaultValue = default(T))
    {
        if (dictionary.TryGetValue(key, out string value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch { }
        }
        return defaultValue;
    }
}