using Microsoft.VisualStudio.TestTools.UnitTesting;

// Parallelism is not inherited from the package: assembly attributes configure the assembly that
// declares them, so the consuming test assembly declares it. This mirrors what the package README
// asks of a real consumer.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
