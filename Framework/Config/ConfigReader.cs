using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Framework.Config;

public static class ConfigReader
{
    private const string _environmentPrefix = "TEST_";

    public static TestSettings Settings { get; set; } = ReadConfig();

    private static TestSettings ReadConfig()
    {
        var configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Unable to determine the executing assembly's directory."), "appsettings.json");
        JsonObject config = JsonNode.Parse(File.ReadAllText(configFilePath))?.AsObject()
            ?? throw new ArgumentException("Configuration file is invalid or missing.");

        ApplyEnvironmentOverrides(config);

        return config.Deserialize<TestSettings>(GetJsonSerializerOptions()) ?? throw new ArgumentException("Configuration file is invalid or missing.");
    }

    // Environment variables named TEST_<Setting> (e.g. TEST_Headless=true) take precedence over appsettings.json.
    private static void ApplyEnvironmentOverrides(JsonObject config)
    {
        foreach (PropertyInfo property in typeof(TestSettings).GetProperties())
        {
            var value = Environment.GetEnvironmentVariable($"{_environmentPrefix}{property.Name}");
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var key = config.Select(p => p.Key).FirstOrDefault(k => string.Equals(k, property.Name, StringComparison.OrdinalIgnoreCase)) ?? property.Name;
            config[key] = property.PropertyType.IsArray
                ? new JsonArray(value.Split(',', StringSplitOptions.TrimEntries).Select(v => (JsonNode?)JsonValue.Create(v)).ToArray())
                : JsonNode.Parse(property.PropertyType == typeof(string) || property.PropertyType == typeof(DriverType) ? JsonSerializer.Serialize(value) : value);
        }
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
