using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using ApprovalTests;
using ApprovalTests.Core;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using ApprovalUtilities.Utilities;
using Newtonsoft.Json;
using Xunit;
using Xunit.Sdk;

using Newsgirl.Fetcher.Tests.Infrastructure;
using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;
using Newtonsoft.Json.Linq;
using Npgsql;
using NSubstitute;

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

        public static DateTime Date2000 = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static IDateProvider DateProviderStub
        {
            get
            {
                var dateStub = Substitute.For<IDateProvider>();
                dateStub.Now().Returns(Date2000);
                return dateStub;
            }
        }
        
        public static ILog LogStub
        {
            get
            {
                var logStub = Substitute.For<ILog>();
                return logStub;
            }
        }
        
        public static ITransactionService TransactionServiceStub
        {
            get
            {
                var transactionService = Substitute.For<ITransactionService>();
                transactionService.ExecuteInTransactionAndCommit(Arg.Any<Func<Task>>()).Returns(info => info.Arg<Func<Task>>()());
                transactionService.ExecuteInTransactionAndCommit(Arg.Any<Func<NpgsqlTransaction,Task>>()).Returns(Task.CompletedTask);
                transactionService.ExecuteInTransaction(Arg.Any<Func<NpgsqlTransaction,Task>>()).Returns(Task.CompletedTask);
                
                return transactionService;
            }
        }

        public static IFeedContentProvider TestResourceContentProvider
        {
            get
            {
                var contentProvider = Substitute.For<IFeedContentProvider>();
                contentProvider.GetFeedContent(null)
                    .ReturnsForAnyArgs(info =>
                    {
                        var feedPoco = info.Arg<FeedPoco>();
                        return GetResource(feedPoco.FeedUrl);
                    });

                return contentProvider;
            }
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
                string message = ex.Message + "\n\n";
                message += new string('=', 30) + "\n\n";
                message += $"touch '{approved}' && kdiff3 '{received}' '{approved}'\n\n";
                message += new string('=', 30) + "\n\n";
                message += $"mv '{received}' '{approved}'\n\n";
                message += new string('=', 30);

                var field = typeof(EqualException)
                    .GetField("message", BindingFlags.Instance | BindingFlags.NonPublic);
                
                field.SetValue(ex, message);
                
                throw;
            }
        }
    }

    public static class Snapshot
    {
        private static readonly string[] IgnoredExceptionFields = {
            "InnerException",
            "StackTrace",
            "StackTraceString",
        };
        
        public static void Match<T>(T obj, string[] parameters = null)
        {
            string json = JsonConvert.SerializeObject(obj);
            
            string formatJson = JsonPrettyPrint.FormatJson(json);

            if (parameters != null)
            {
                NamerFactory.AdditionalInformation = string.Join("_", parameters);
            }
            
            Approvals.VerifyWithExtension(formatJson, ".json");
        }
        
        public static void MatchError(Exception exception, string[] parameters = null)
        {
            var exceptions = new List<Exception>();

            while (exception != null)
            {
                exceptions.Add(exception);
                exception = exception.InnerException;
            }

            string json = JsonConvert.SerializeObject(exceptions);

            var jsonExceptions = JArray.Parse(json);

            foreach (var obj in jsonExceptions.Cast<JObject>())
            {
                foreach (string ignoredPropertyName in IgnoredExceptionFields)
                {
                    obj.Property(ignoredPropertyName)?.Remove();
                }
            }

            json = JsonConvert.SerializeObject(jsonExceptions);
            
            string formatJson = JsonPrettyPrint.FormatJson(json);

            if (parameters != null)
            {
                NamerFactory.AdditionalInformation = string.Join("_", parameters);
            }
            
            Approvals.VerifyWithExtension(formatJson, ".json");
        }
    }
    
    public static class JsonPrettyPrint
    {
        private const string IndentString = "  ";
        
        public static string FormatJson(string str)
        {
            var indent = 0;
            
            var quoted = false;
            
            var sb = new StringBuilder();
            
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                
                switch (ch)
                {
                    case '{':
                    {
                        sb.Append(ch);
                        if (!quoted)
                        {
                            if (str[i + 1] != '}')
                            {
                                sb.AppendLine();
                                Enumerable.Range(0, ++indent).ForEach(item => sb.Append(IndentString));
                            }
                        }

                        break;
                    }
                    case '[':
                    {
                        sb.Append(ch);
                        if (!quoted)
                        {
                            if (str[i + 1] != ']')
                            {
                                sb.AppendLine();
                                Enumerable.Range(0, ++indent).ForEach(item => sb.Append(IndentString));
                            }
                        }

                        break;
                    }
                    case '}':
                    {
                        if (!quoted)
                        {
                            if (str[i - 1] != '{')
                            {
                                sb.AppendLine();
                                Enumerable.Range(0, --indent).ForEach(item => sb.Append(IndentString));
                            }
                        }

                        sb.Append(ch);
                        break;
                    }
                    case ']':
                    {
                        if (!quoted)
                        {
                            if (str[i - 1] != '[')
                            {
                                sb.AppendLine();
                                Enumerable.Range(0, --indent).ForEach(item => sb.Append(IndentString));
                            }
                        }

                        sb.Append(ch);
                        break;
                    }
                    case '"':
                    {
                        sb.Append(ch);
                        var escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                        {
                            escaped = !escaped;
                        }

                        if (!escaped)
                        {
                            quoted = !quoted;
                        }

                        break;
                    }
                    case ',':
                    {
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(IndentString));
                        }

                        break;
                    }
                    case ':':
                    {
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.Append(" ");
                        }

                        break;
                    }
                    default:
                    {
                        sb.Append(ch);
                        break;
                    }
                }
            }

            return sb.ToString();
        }         
    }
}
