using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Newsgirl.Testing;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]
