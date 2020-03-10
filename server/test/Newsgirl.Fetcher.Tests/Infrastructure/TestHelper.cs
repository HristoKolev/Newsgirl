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
using Newtonsoft.Json.Serialization;
using Npgsql;
using NSubstitute;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]

namespace Newsgirl.Fetcher.Tests.Infrastructure
{
    public static class TestHelper
    {
        // ReSharper disable once InconsistentNaming
        private static TestConfig _testConfig;
        private static readonly object TestConfigSync = new object();

        public static async Task<string> GetResource(string name)
        {
            string content = await File.ReadAllTextAsync($"../../../resources/{name}");

            return content;
        }
        
        public static async Task<string> GetSql(string name)
        {
            string content = await File.ReadAllTextAsync($"../../../sql/{name}");

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
        
        public static TestConfig TestConfig
        {
            get
            {
                if (_testConfig == null)
                {
                    lock (TestConfigSync)
                    {
                        if (_testConfig == null)
                        {
                            string json = File.ReadAllText("../../../test-config.json");
                            _testConfig = JsonConvert.DeserializeObject<TestConfig>(json);
                        }
                    }
                }

                return _testConfig;
            }
        }

    }

    public class TestConfig
    {
        public string ConnectionString { get; set; }
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
            string json = Serialize(obj);

            if (parameters != null)
            {
                NamerFactory.AdditionalInformation = string.Join("_", parameters);
            }
            
            Approvals.VerifyWithExtension(json, ".json");
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

            json = Serialize(jsonExceptions);

            if (parameters != null)
            {
                NamerFactory.AdditionalInformation = string.Join("_", parameters);
            }
            
            Approvals.VerifyWithExtension(json, ".json");
        }

        private static string Serialize(object obj)
        {
            
            var settings = new JsonSerializerSettings
            {
                ContractResolver = SortedPropertiesContractResolver.Instance
            };

            string json = JsonConvert.SerializeObject(obj, settings);
            
            string formatJson = JsonPrettyPrint.FormatJson(json);

            return formatJson;
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
    
    public abstract class DatabaseTest : IAsyncLifetime
    {
        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            var builder = new NpgsqlConnectionStringBuilder(TestHelper.TestConfig.ConnectionString)
            {
                Enlist = false,
            };

            this.DbConnection = new NpgsqlConnection(builder.ToString());
            this.Db = new DbService(this.DbConnection);
            this.Tx = await this.Db.BeginTransaction();

            string content = await TestHelper.GetSql("before-tests.sql");

            var parts = content.Split(new[] {"--================================"},
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string sql in parts)
            {
                await this.Db.ExecuteNonQuery(sql);                
            }
        }

        private NpgsqlTransaction Tx { get; set; }
        
        protected NpgsqlConnection DbConnection { get; private set; }
        
        protected DbService Db { get; private set; }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            await this.Tx.RollbackAsync();

            await this.DbConnection.DisposeAsync();
            
            this.Db.Dispose();
        }
    }
    
    public class SortedPropertiesContractResolver : DefaultContractResolver
    {
        // use a static instance for optimal performance

        static SortedPropertiesContractResolver() { Instance = new SortedPropertiesContractResolver(); }

        public static SortedPropertiesContractResolver Instance { get; }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            
            if (properties != null)
            {
                return properties.OrderBy(p => p.UnderlyingName).ToList();
            }

            return properties;
        }
    }
}
