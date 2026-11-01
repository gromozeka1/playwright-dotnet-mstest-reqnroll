using System.Reflection;
using System.Reflection.Emit;
using HardPoint.Testing.Config;
using HardPoint.Testing.Driver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

namespace HardPoint.Testing.Tests;

// The hook reads the process-wide ConfigReader.Settings and keeps a one-time warning flag, so
// these tests must not run in parallel with the other tests that use either.
[TestClass]
[DoNotParallelize]
public sealed class BaseHookTests
{
    [TestInitialize]
    public void SetUp()
    {
        BaseHook.ResetParallelismWarning();
        ConfigReader.Settings = new TestSettings
        {
            BrowserType = "chromium",
            ApplicationUrl = "http://localhost:5168",
        };
    }

    [TestCleanup]
    public void TearDown()
    {
        BaseHook.ResetParallelismWarning();

        // Reload instead of leaving the in-memory settings behind: the ConfigReader tests assert
        // on the re-read (missing-file) behaviour.
        ConfigReader.Reload();
    }

    [TestMethod]
    public void WarnsWhenTheHookAssemblyDeclaresNoParallelizeAttribute()
    {
        var output = new RecordingOutput();

        new Hooks().WarnWhenParallelismIsNotConfigured(output);

        Assert.AreEqual(1, output.Lines.Count, "the warning should be written once");
        StringAssert.Contains(output.Lines[0], "no [assembly: Parallelize] attribute");
        StringAssert.Contains(output.Lines[0], "run serially");
        StringAssert.Contains(output.Lines[0], "warnOnSerialExecution");
    }

    [TestMethod]
    public void WarnsOnlyOncePerRun()
    {
        var output = new RecordingOutput();
        var hooks = new Hooks();

        hooks.WarnWhenParallelismIsNotConfigured(output);
        hooks.WarnWhenParallelismIsNotConfigured(output);

        Assert.AreEqual(1, output.Lines.Count);
    }

    [TestMethod]
    public void DoesNotWarnWhenTheSettingIsOff()
    {
        ConfigReader.Settings.WarnOnSerialExecution = false;
        var output = new RecordingOutput();

        new Hooks().WarnWhenParallelismIsNotConfigured(output);

        Assert.AreEqual(0, output.Lines.Count);
    }

    [TestMethod]
    [DataRow(TraceMode.Off, true, false)]
    [DataRow(TraceMode.Off, false, false)]
    [DataRow(TraceMode.On, true, true)]
    [DataRow(TraceMode.On, false, true)]
    [DataRow(TraceMode.RetainOnFailure, true, true)]
    [DataRow(TraceMode.RetainOnFailure, false, false)]
    public void KeepTraceFollowsTheTraceMode(TraceMode mode, bool failed, bool expected)
        => Assert.AreEqual(expected, BaseHook.KeepTrace(mode, failed));

    [TestMethod]
    public void DeclaresParallelizeIsFalseForAnAssemblyWithoutTheAttribute()
        => Assert.IsFalse(BaseHook.DeclaresParallelize(typeof(BaseHookTests).Assembly));

    [TestMethod]
    public void DeclaresParallelizeIsTrueForAnAssemblyThatDeclaresIt()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"ParallelizeAssembly{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);

        assembly.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(ParallelizeAttribute).GetConstructor(Type.EmptyTypes)!,
            []));

        Assert.IsTrue(BaseHook.DeclaresParallelize(assembly));
    }

    private sealed class RecordingOutput : IReqnrollOutputHelper
    {
        public List<string> Lines { get; } = [];

        public void WriteLine(string message) => Lines.Add(message);

        public void WriteLine(string format, params object[] args) => Lines.Add(string.Format(format, args));

        public void AddAttachment(string filePath)
        {
        }
    }

    // The hook reads the concrete type's assembly, so a derived class in this assembly reports the
    // unit-test assembly — which, like a consumer that forgot the attribute, declares no Parallelize.
    private sealed class Hooks : BaseHook;
}
