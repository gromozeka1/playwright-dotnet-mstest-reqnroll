namespace HardPoint.Testing.Config;

/// <summary>When a Playwright trace is recorded for <c>@ui</c> scenarios.</summary>
public enum TraceMode
{
    /// <summary>Never record a trace.</summary>
    Off,

    /// <summary>Always record a trace and keep it, pass or fail.</summary>
    On,

    /// <summary>Record a trace and keep it only when the scenario fails.</summary>
    RetainOnFailure,
}
