namespace IMCommon;

static public class DictionaryExtension
{
    static public void Append<K, V>(this Dictionary<K, V> source, Dictionary<K, V> target)
    {
        foreach(var item in target)
        {
            source[item.Key] = item.Value;
        }
    }
}
