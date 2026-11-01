namespace HardPoint.Testing.Constants;

/// <summary>
/// The shared scenario tag vocabulary: one test-type tag (<c>@ui</c> or <c>@api</c>) and at least
/// one environment tag, both enforced by <see cref="Driver.BaseHook"/> when the
/// <c>enforceScenarioTags</c> setting is on.
/// </summary>
public class ScenarioTags
{
    /// <summary>The <c>@ui</c> test-type tag: the scenario drives a browser.</summary>
    public const string UI = "ui";

    /// <summary>The <c>@api</c> test-type tag: the scenario only calls HTTP endpoints.</summary>
    public const string API = "api";

    /// <summary>The <c>@local</c> environment tag.</summary>
    public const string Local = "local";

    /// <summary>The <c>@dev</c> environment tag.</summary>
    public const string Dev = "dev";

    /// <summary>The <c>@qa</c> environment tag.</summary>
    public const string QA = "qa";

    /// <summary>The <c>@stage</c> environment tag.</summary>
    public const string Stage = "stage";

    /// <summary>The <c>@prod</c> environment tag.</summary>
    public const string Prod = "prod";

    /// <summary>The test-type tags a scenario must contain one of.</summary>
    public static readonly HashSet<string> MandatoryTestTypeTags = new HashSet<string> { UI, API };

    /// <summary>The environment tags a scenario must contain at least one of.</summary>
    public static readonly HashSet<string> MandatoryEnvTags = new HashSet<string> { Local, Dev, QA, Stage, Prod };
}
