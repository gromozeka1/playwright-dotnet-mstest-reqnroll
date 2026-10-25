namespace Framework.Constants;

public class ScenarioTags
{
    public const string UI = "ui";
    public const string API = "api";

    public const string Local = "local";
    public const string Dev = "dev";
    public const string QA = "qa";
    public const string Stage = "stage";
    public const string Prod = "prod";

    public static readonly HashSet<string> MandatoryTestTypeTags = new HashSet<string> { UI, API };

    public static readonly HashSet<string> MandatoryEnvTags = new HashSet<string> { Dev, QA, Stage, Prod };
}
