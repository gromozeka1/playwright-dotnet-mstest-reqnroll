using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Framework.Config;

public static class ConfigReader
{
    public static TestSettings Settings { get; set; } = ReadConfig();

    private static TestSettings ReadConfig()
    {
        var configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Unable to determine the executing assembly's directory."), "appsettings.json");
        var configFile = File.ReadAllText(configFilePath);

        return JsonSerializer.Deserialize<TestSettings>(configFile, GetJsonSerializerOptions()) ?? throw new ArgumentException("Configuration file is invalid or missing.");
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var jsonSerializerSettings = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        jsonSerializerSettings.Converters.Add(new JsonStringEnumConverter());

        return jsonSerializerSettings;
    }
}
