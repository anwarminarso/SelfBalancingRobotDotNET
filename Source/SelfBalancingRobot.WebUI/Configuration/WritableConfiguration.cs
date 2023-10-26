using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace SelfBalancingRobot.WebUI.Configuration;

public class WritableConfiguration
{
    private readonly IConfiguration configuration;
    private readonly IWebHostEnvironment environment;
    private readonly IServiceProvider provider;
    public WritableConfiguration(IServiceProvider provider, IConfiguration configuration, IWebHostEnvironment environment)
    {
        this.configuration = configuration;
        this.environment = environment;
        this.provider = provider;
    }

    public T GetConfig<T>(string section, string file = "appsettings.json")
        where T : class, new()
    {
        var conf = (IConfigurationRoot)configuration;
        var fileProvider = environment.ContentRootFileProvider;
        var fileInfo = fileProvider.GetFileInfo(file);
        var physicalPath = fileInfo.PhysicalPath;

        var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath));
        var sectionObject = jObject.TryGetValue(section, out JToken _section) ? JsonConvert.DeserializeObject<T>(_section.ToString()) : new T();
        return sectionObject;
    }
    public async Task<T> UpdateAsync<T>(Action<T> applyChanges, string section, string file = "appsettings.json")
        where T : class, new()
    {
        var conf = (IConfigurationRoot)configuration;
        var fileProvider = environment.ContentRootFileProvider;
        var fileInfo = fileProvider.GetFileInfo(file);
        var physicalPath = fileInfo.PhysicalPath;

        var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath));
        var sectionObject = jObject.TryGetValue(section, out JToken _section) ? JsonConvert.DeserializeObject<T>(_section.ToString()) : new T();

        applyChanges(sectionObject);

        jObject[section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
        await File.WriteAllTextAsync(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        conf.Reload();

        return sectionObject;
    }
    public T GetService<T>()
    {
        return provider.GetService<T>();
    }
}