using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SelfBalancingRobot.WebUI.Models;
using System.Reflection;

namespace SelfBalancingRobot.WebUI.Configuration;

public class AppSettings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public IMUType IMUType { get; set; }

    public int? IMUAddress { get; set; }
    public int I2CBusId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string> Headers { get; set; }

    private string _Version = string.Empty;

    [JsonIgnore]
    public string Version { get { return _Version; } }

    public AppSettings()
    {
        _Version = Assembly.GetEntryAssembly().GetName().Version.ToString();
    }
}