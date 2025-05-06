using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string jsonString)
{
    // Handle both wrapped and raw array formats
    if (jsonString.TrimStart().StartsWith("["))
    {
        // Raw array (e.g., "[{...}, {...}]")
        return JsonUtility.FromJson<T[]>(jsonString);
    }
    else
    {
        // Wrapped array (e.g., "{\"Items\": [{...}, {...}]}")
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(jsonString);
        return wrapper.Items;
    }
}

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}