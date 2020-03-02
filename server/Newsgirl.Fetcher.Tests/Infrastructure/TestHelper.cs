using System;
using System.IO;
using System.Threading.Tasks;

using ApprovalTests.Core;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Newsgirl.Fetcher.Tests.Infrastructure;
using Xunit;
using Xunit.Sdk;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]

namespace Newsgirl.Fetcher.Tests.Infrastructure
{
    public static class TestHelper
    {
        public static async Task<string> GetResource(string name)
        {
            string content = await File.ReadAllTextAsync($"../../../resources/{name}");

            return content;
        }
    }
    
    public class CustomReporter : IApprovalFailureReporter
    {
        public void Report(string approved, string received)
        {
            string approvedContent = File.Exists(approved) ? File.ReadAllText(approved) : "";
            string receivedContent = File.ReadAllText(received);
            
            try
            {
                Assert.Equal(approvedContent, receivedContent);   
            }
            catch (EqualException ex)
            {
                string message = "Assert.Equal() Failure\n\n";
                message += new string('=', 30) + "\n\n";
                message += $"mv '{received}' '{approved}'\n\n";
                message += new string('=', 30) + "\n\n";
                message += $"touch '{approved}' && kdiff3 '{received}' '{approved}'\n\n";
                message += new string('=', 30) + "\n";
                
                message += "\n\n";
                message += ex.Message;
                
                throw new ApplicationException(message);
            }
        }
    }
}