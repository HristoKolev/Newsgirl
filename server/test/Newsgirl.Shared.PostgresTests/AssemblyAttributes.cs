using ApprovalTests.Reporters;
using Newsgirl.Testing;
using Xunit;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: CollectionBehavior(MaxParallelThreads = 16)]
