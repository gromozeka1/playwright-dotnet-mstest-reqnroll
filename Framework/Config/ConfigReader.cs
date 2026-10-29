using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Framework.Config;

public static class ConfigReader
{
    private const string _environmentPrefix = "TEST_";
    private const string _localConfigFileName = "appsettings.json";
    private const string _ciConfigFileName = "appsettings.CI.json";
    private static readonly string[] _pathSeparator = ["__"];

    public static TestSettings Settings { get; set; } = ReadConfig();

    private static TestSettings ReadConfig()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Unable to determine the executing assembly's directory.");
        var configFilePath = Path.Combine(assemblyDirectory, SelectConfigFileName(assemblyDirectory));
        JsonObject config = JsonNode.Parse(File.ReadAllText(configFilePath))?.AsObject()
            ?? throw new ArgumentException("Configuration file is invalid or missing.");

        ApplyEnvironmentOverrides(config);

        return config.Deserialize<TestSettings>(GetJsonSerializerOptions()) ?? throw new ArgumentException("Configuration file is invalid or missing.");
    }

    // Local runs use appsettings.json; CI runs (CI=true, as set by the CI server) use appsettings.CI.json,
    // which runs the browser headless. Falls back to the local file when the CI file is missing.
    private static string SelectConfigFileName(string assemblyDirectory)
    {
        var ciEnvironment = Environment.GetEnvironmentVariable("CI");
        var isCiRun = !string.IsNullOrWhiteSpace(ciEnvironment)
            && (ciEnvironment.Equals("true", StringComparison.OrdinalIgnoreCase) || ciEnvironment == "1");

        return isCiRun && File.Exists(Path.Combine(assemblyDirectory, _ciConfigFileName))
            ? _ciConfigFileName
            : _localConfigFileName;
    }

    // Environment variables named TEST_<Setting> (e.g. TEST_Headless=true) take precedence over the config file.
    // Nested settings are addressed either by their leaf name alone (TEST_Headless -> browserOptions.headless)
    // or by the full path of C# property names separated by __ (TEST_APIRequestNewContextOptions__BaseURL).
    private static void ApplyEnvironmentOverrides(JsonObject config)
    {
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var name = entry.Key as string;
            if (name is null || !name.StartsWith(_environmentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var setting = name[_environmentPrefix.Length..];
            var value = entry.Value as string;
            if (setting.Length == 0 || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (TryResolveSettingLocation(config, setting, out var owner, out var key, out var property))
            {
                owner[key] = CreateValueNode(property, owner[key], value);
            }
        }
    }

    private static bool TryResolveSettingLocation(JsonObject config, string setting, out JsonObject owner, out string key, out PropertyInfo? property)
    {
        var segments = setting.Split(_pathSeparator, StringSplitOptions.None);

        // 1. Full path of C# property names, translated to their serialized keys (which may differ in casing or name).
        var path = FindPropertyPath(typeof(TestSettings), segments);
        if (path is not null && TryWalkJson(config, path, out owner, out key))
        {
            property = path[^1];
            return true;
        }

        // 2. Bare leaf name: TEST_Headless resolves to the single nested "headless" key wherever it lives.
        property = null;
        if (segments.Length == 1)
        {
            var match = FindJsonEntryByKey(config, segments[0]);
            if (match is not null)
            {
                (owner, key) = match.Value;
                property = FindLeafProperty(typeof(TestSettings), segments[0]);
                return true;
            }
        }

        owner = null!;
        key = string.Empty;
        return false;
    }

    // Walks TestSettings following the given property-name segments (case-insensitive) and returns the leaf property.
    private static PropertyInfo[]? FindPropertyPath(Type type, string[] segments)
    {
        var path = new List<PropertyInfo>(segments.Length);

        foreach (var segment in segments)
        {
            var property = type.GetProperties().FirstOrDefault(p =>
                string.Equals(p.Name, segment, StringComparison.OrdinalIgnoreCase) ||
                p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name is { } jsonName &&
                string.Equals(jsonName, segment, StringComparison.OrdinalIgnoreCase));

            if (property is null)
            {
                return null;
            }

            path.Add(property);
            type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (path.Count < segments.Length && IsLeafType(type))
            {
                return null;
            }
        }

        return path.ToArray();
    }

    // Walks the JSON tree translating each C# property of the path to its serialized key.
    private static bool TryWalkJson(JsonObject node, PropertyInfo[] path, out JsonObject owner, out string key)
    {
        owner = node;
        key = string.Empty;

        for (var i = 0; i < path.Length; i++)
        {
            var candidate = path[i].GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? path[i].Name;
            var match = owner.Select(p => p.Key).FirstOrDefault(k =>
                string.Equals(k, candidate, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                return false;
            }

            if (i < path.Length - 1)
            {
                if (owner[match] is not JsonObject child)
                {
                    return false;
                }

                owner = child;
            }

            key = match;
        }

        return true;
    }

    // Finds the first entry in the tree (depth-first) whose key matches the name case-insensitively.
    private static (JsonObject Owner, string Key)? FindJsonEntryByKey(JsonObject node, string name)
    {
        foreach (var entry in node)
        {
            if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return (node, entry.Key);
            }

            if (entry.Value is JsonObject child)
            {
                var nested = FindJsonEntryByKey(child, name);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    // Finds a leaf property with the given name anywhere in the TestSettings object graph, used to type the override.
    private static PropertyInfo? FindLeafProperty(Type type, string name)
    {
        foreach (var property in type.GetProperties())
        {
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (IsLeafType(propertyType))
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property;
                }

                continue;
            }

            var nested = FindLeafProperty(propertyType, name);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static bool IsLeafType(Type type) => type == typeof(string) || type.IsEnum || type.IsValueType || IsCollection(type);

    // Settings such as BrowserTypeLaunchOptions.Args are IEnumerable<string> rather than string[].
    private static bool IsCollection(Type type) => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

    private static JsonNode CreateValueNode(PropertyInfo? property, JsonNode? existing, string value)
    {
        if (property is null)
        {
            // No C# property matched the key, so keep the type the JSON file already uses.
            return existing is JsonValue currentValue && currentValue.TryGetValue<string>(out _)
                ? JsonValue.Create(value)
                : JsonNode.Parse(value) ?? JsonValue.Create(value);
        }

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (IsCollection(type))
        {
            return new JsonArray(value.Split(',', StringSplitOptions.TrimEntries).Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());
        }

        if (type == typeof(string) || type.IsEnum)
        {
            return JsonValue.Create(value);
        }

        return JsonNode.Parse(value) ?? JsonValue.Create(value);
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
