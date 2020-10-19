using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Newsgirl.Testing;
using Xunit;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]
[assembly: CollectionBehavior(MaxParallelThreads = 16)]
