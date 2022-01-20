using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Xdxd.DotNet.Testing;
using Xunit;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]
[assembly: CollectionBehavior(MaxParallelThreads = 32)]
[assembly: TestConfig("../../test-config.json")]
