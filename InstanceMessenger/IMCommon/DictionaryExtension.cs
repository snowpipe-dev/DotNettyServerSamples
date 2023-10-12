namespace IMCommon;

public static class DictionaryExtension
{
    public static void Append<K, V>(this Dictionary<K, V> source, Dictionary<K, V> target)
    {
        foreach(var item in target)
        {
            source[item.Key] = item.Value;
        }
    }
}
