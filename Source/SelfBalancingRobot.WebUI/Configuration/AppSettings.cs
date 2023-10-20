using Newtonsoft.Json;
using System.Reflection;

namespace SelfBalancingRobot.WebUI.Configuration
{
    public class AppSettings
    {
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
}
