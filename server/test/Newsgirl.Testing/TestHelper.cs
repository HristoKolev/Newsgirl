using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Newsgirl.Testing;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]

namespace Newsgirl.Testing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Threading.Tasks;
    using ApprovalTests;
    using ApprovalTests.Core;
    using ApprovalUtilities.Utilities;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Npgsql;
    using NSubstitute;
    using Shared;
    using Shared.Logging;
    using Xunit;
    using Xunit.Sdk;

    public static class TestHelper
    {
        // ReSharper disable once InconsistentNaming
        private static TestConfig _testConfig;
        private static readonly object TestConfigSync = new object();

        public static async Task<string> GetResourceText(string name)
        {
            var bytes = await GetResourceBytes(name);
            return EncodingHelper.UTF8.GetString(bytes);
        }

        public static string GetResourceFilePath(string name)
        {
            return $"../../../resources/{name}";
        }

        public static async Task<byte[]> GetResourceBytes(string name)
        {
            var content = await File.ReadAllBytesAsync(GetResourceFilePath(name));
            return content;
        }

        public static async Task<string> GetSql(string name)
        {
            string content = await ResourceHelper.GetString($"sql.{name}");

            return content;
        }

        public static DateTime Date2000 = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateProvider DateProviderStub
        {
            get
            {
                var dateStub = Substitute.For<DateProvider>();
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

        public static ErrorReporter ErrorReporterStub
        {
            get
            {
                var stub = Substitute.For<ErrorReporter>();
                return stub;
            }
        }

        public static DbTransactionService TransactionServiceStub
        {
            get
            {
                var transactionService = Substitute.For<DbTransactionService>();
                transactionService.ExecuteInTransactionAndCommit(Arg.Any<Func<Task>>())
                    .Returns(info => info.Arg<Func<Task>>()());
                transactionService.ExecuteInTransactionAndCommit(Arg.Any<Func<NpgsqlTransaction, Task>>())
                    .Returns(Task.CompletedTask);
                transactionService.ExecuteInTransaction(Arg.Any<Func<NpgsqlTransaction, Task>>())
                    .Returns(Task.CompletedTask);

                return transactionService;
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
                            string json = ResourceHelper.GetString("test-config.json").Result;

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
                message += new string('=', 30) + "\n\n\n";
                message += $"touch '{approved}' && kdiff3 '{received}' '{approved}'\n\n\n";
                message += new string('=', 30) + "\n\n\n";
                message += $"mv '{received}' '{approved}'\n\n\n";
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
        private static readonly string[] IgnoredExceptionFields =
        {
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

        public static void MatchJson(string json, string[] parameters = null)
        {
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

            json = Serialize(jsonExceptions);

            if (parameters != null)
            {
                NamerFactory.AdditionalInformation = string.Join("_", parameters);
            }

            Approvals.VerifyWithExtension(json, ".json");
        }

        public static void MatchError(Action func, string[] parameters = null)
        {
            Exception exception = null;

            try
            {
                func();
            }
            catch (Exception err)
            {
                exception = err;
            }

            MatchError(exception, parameters);
        }

        public static async Task MatchError(Func<Task> func, string[] parameters = null)
        {
            Exception exception = null;

            try
            {
                await func();
            }
            catch (Exception err)
            {
                exception = err;
            }

            MatchError(exception, parameters);
        }

        private static string Serialize(object obj)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = SortedPropertiesContractResolver.Instance,
            };

            string json = JsonConvert.SerializeObject(obj, settings);

            string formatJson = JsonPrettyPrint.FormatJson(json);

            return formatJson;
        }
    }

    public static class AssertExt
    {
        public static void SequentialEqual<T>(IList<T> expected, IList<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }
    }

    public static class JsonPrettyPrint
    {
        private const string IndentString = "  ";

        public static string FormatJson(string str)
        {
            int indent = 0;

            bool quoted = false;

            var sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];

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
                        bool escaped = false;
                        int index = i;
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

        static SortedPropertiesContractResolver()
        {
            Instance = new SortedPropertiesContractResolver();
        }

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

    public static class ResourceHelper
    {
        public static async Task<string> GetString(string resourceName)
        {
            var assembly = typeof(ResourceHelper).Assembly;

            var resourceStream = assembly.GetManifestResourceStream($"{typeof(ResourceHelper).Namespace}.{resourceName}");

            using (var reader = new StreamReader(resourceStream, EncodingHelper.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }

    public class ErrorReporterMock : ErrorReporter, IAsyncDisposable
    {
        private readonly ErrorReporterMockConfig config;
        private const string ZeroGuid = "61289445-04b7-4f59-bbdd-499c36861bc0";

        public List<(Exception, string, Dictionary<string, object>)> Errors { get; } = new List<(Exception, string, Dictionary<string, object>)>();

        public ErrorReporterMock() : this(new ErrorReporterMockConfig()) { }

        public Exception FirstException => this.Errors.First().Item1;

        public Exception SingleException
        {
            get
            {
                if (this.Errors.Count > 1)
                {
                    throw new ApplicationException("SingleException is called with more that 1 error in the list.");
                }

                return this.Errors.Single().Item1;
            }
        }

        public ErrorReporterMock(ErrorReporterMockConfig config)
        {
            this.config = config;
        }

        public Task<string> Error(Exception exception, string fingerprint, Dictionary<string, object> additionalInfo)
        {
            this.Errors.Add((exception, fingerprint, additionalInfo));
            return Task.FromResult(ZeroGuid);
        }

        public Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo)
        {
            return this.Error(exception, null, additionalInfo);
        }

        public Task<string> Error(Exception exception, string fingerprint)
        {
            return this.Error(exception, fingerprint, null);
        }

        public Task<string> Error(Exception exception)
        {
            return this.Error(exception, null, null);
        }

        public ValueTask DisposeAsync()
        {
            if (this.config.ThrowFirstErrorOnDispose && this.Errors.Count > 0)
            {
                var firstException = this.Errors.First().Item1;

                ExceptionDispatchInfo.Capture(firstException).Throw();
            }

            return new ValueTask();
        }
    }

    public class ErrorReporterMockConfig
    {
        public bool ThrowFirstErrorOnDispose { get; set; } = true;
    }

    public class StructuredLogMock : ILog
    {
        public Dictionary<string, List<object>> Logs { get; } = new Dictionary<string, List<object>>();

        public void Log<T>(string eventStreamName, Func<T> func)
        {
            if (!this.Logs.ContainsKey(eventStreamName))
            {
                this.Logs.Add(eventStreamName, new List<object>
                {
                    func(),
                });
            }
            else
            {
                this.Logs[eventStreamName].Add(func());
            }
        }
    }
}
