using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Dynamic;

namespace SelfBalancingRobot.WebUI.Extensions;

public static class ConfigurationBinder
{
    public static void BindJsonNet<T>(this IConfiguration config, T instance) where T : class
    {
        string objectPath = config.AsEnumerable().Select(x => x.Key).FirstOrDefault();

        var obj = BindToExpandoObject(config, objectPath);
        if (obj == null)
            return;

        var jsonText = JsonConvert.SerializeObject(obj);
        var jObj = JObject.Parse(jsonText);
        if (jObj == null)
            return;

        var jToken = jObj.SelectToken($"*.{GetLastObjectName(objectPath)}");
        if (jToken == null)
            return;

        jToken.Populate<T>(instance);
    }

    private static ExpandoObject BindToExpandoObject(IConfiguration config, string objectPath)
    {
        var result = new ExpandoObject();
        string lastObjectPath = GetLastObjectPath(objectPath);

        // retrieve all keys from your settings
        var configs = config.AsEnumerable();
        configs = configs
            .Select(x => new KeyValuePair<string, string>(x.Key.Replace(lastObjectPath, ""), x.Value))
            .ToArray();

        foreach (var kvp in configs)
        {
            var parent = result as IDictionary<string, object>;
            var path = kvp.Key.Split(':');

            // create or retrieve the hierarchy (keep last path item for later)
            var i = 0;
            for (i = 0; i < path.Length - 1; i++)
            {
                if (!parent.ContainsKey(path[i]))
                {
                    parent.Add(path[i], new ExpandoObject());
                }

                parent = parent[path[i]] as IDictionary<string, object>;
            }

            if (kvp.Value == null)
                continue;

            // add the value to the parent
            // note: in case of an array, key will be an integer and will be dealt with later
            var key = path[i];
            parent.Add(key, kvp.Value);
        }

        // at this stage, all arrays are seen as dictionaries with integer keys
        ReplaceWithArray(null, null, result);

        return result;
    }

    private static string GetLastObjectPath(string objectPath)
    {
        string lastObjectPath = objectPath;
        int indexLastObj;
        if ((indexLastObj = objectPath.LastIndexOf(":")) != -1)
            lastObjectPath = objectPath.Remove(indexLastObj);
        return lastObjectPath;
    }

    private static string GetLastObjectName(string objectPath)
    {
        string lastObjectPath = objectPath;
        int indexLastObj;
        if ((indexLastObj = objectPath.LastIndexOf(":")) != -1)
            lastObjectPath = objectPath.Substring(indexLastObj + 1);
        return lastObjectPath;
    }

    private static void ReplaceWithArray(ExpandoObject parent, string key, ExpandoObject input)
    {
        if (input == null)
            return;

        var dict = input as IDictionary<string, object>;
        var keys = dict.Keys.ToArray();

        // it's an array if all keys are integers
        if (keys.All(k => int.TryParse(k, out var dummy)))
        {
            var array = new object[keys.Length];
            foreach (var kvp in dict)
            {
                array[int.Parse(kvp.Key)] = kvp.Value;
            }

            var parentDict = parent as IDictionary<string, object>;
            parentDict.Remove(key);
            parentDict.Add(key, array);
        }
        else
        {
            foreach (var childKey in dict.Keys.ToList())
            {
                ReplaceWithArray(input, childKey, dict[childKey] as ExpandoObject);
            }
        }
    }

    public static void Populate<T>(this JToken value, T target) where T : class
    {
        using (var sr = value.CreateReader())
        {
            JsonSerializer.CreateDefault().Populate(sr, target); // Uses the system default JsonSerializerSettings
        }
    }
}
