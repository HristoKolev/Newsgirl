namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Testing;
    using Xunit;

    public class ErrorReporterTestError1ReturnsWithoutError : ErrorReporterTest
    {
        [Fact]
        public async Task Error1_returns_without_error()
        {
            var reporter = CreateReporter();

            try
            {
                ThrowException();
            }
            catch (Exception exception)
            {
                string eventID = await reporter.Error(exception);

                await AssertEventSentToServer(eventID);
            }
        }
    }

    public class ErrorReporterTestError2ReturnsWithoutError : ErrorReporterTest
    {
        [Fact]
        public async Task Error2_returns_without_error()
        {
            var reporter = CreateReporter();

            var testException = new DetailedException($"Testing {nameof(ErrorReporterImpl)}.");

            string eventID = await reporter.Error(testException, "TESTING_FINGERPRINT");

            await AssertEventSentToServer(eventID);
        }
    }

    public class ErrorReporterTestError3ReturnsWithoutError : ErrorReporterTest
    {
        [Fact]
        public async Task Error3_returns_without_error()
        {
            var reporter = CreateReporter();

            var testException = new DetailedException($"Testing {nameof(ErrorReporterImpl)}.");

            string eventID = await reporter.Error(testException, new Dictionary<string, object>
            {
                { "testkey", "testvalue" },
            });

            await AssertEventSentToServer(eventID);
        }
    }

    public class ErrorReporterTestError4ReturnsWithoutError : ErrorReporterTest
    {
        [Fact]
        public async Task Error4_returns_without_error()
        {
            var reporter = CreateReporter();

            try
            {
                ThrowException();
            }
            catch (Exception exception)
            {
                string eventID = await reporter.Error(exception, "TESTING_FINGERPRINT", new Dictionary<string, object>
                {
                    { "test_key", "test_value" },
                });

                await AssertEventSentToServer(eventID);
            }
        }
    }

    public class ErrorReporterTestErrorCallHooksIfAvailable : ErrorReporterTest
    {
        [Fact]
        public async Task Error_call_hooks_if_available()
        {
            var reporter = CreateReporter();

            bool called = false;

            reporter.AddDataHook(() =>
            {
                called = true;

                return new Dictionary<string, object>
                {
                    { "test_hook_key", "test_hook_value" },
                };
            });

            try
            {
                ThrowException();
            }
            catch (Exception exception)
            {
                await reporter.Error(exception, "TESTING_FINGERPRINT", new Dictionary<string, object>
                {
                    { "test_key", "test_value" },
                });
            }

            Assert.True(called);
        }
    }

    public class ErrorReporterTestErrorTakesFingerprintFromException : ErrorReporterTest
    {
        [Fact]
        public async Task Error_takes_fingerprint_from_exception()
        {
            const string TESTING_FINGERPRINT = "TESTING_FINGERPRINT";

            var reporter = CreateReporter();

            try
            {
                ThrowException(TESTING_FINGERPRINT);
            }
            catch (Exception exception)
            {
                string eventID = await reporter.Error(exception, new Dictionary<string, object>
                {
                    { "test_key", "test_value" },
                });

                string eventJson = await GetServerEvent(eventID);

                var eventObject = JsonDocument.Parse(eventJson).RootElement;

                // ReSharper disable once HeapView.BoxingAllocation
                string[] fingerprint = eventObject.GetProperty("fingerprints")
                    .EnumerateArray()
                    .Select(x => x.GetString())
                    .ToArray();

                AssertExt.SequentialEqual(new[] { Md5(TESTING_FINGERPRINT) }, fingerprint);
            }
        }

        private static string Md5(string data)
        {
            return BitConverter.ToString(MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(data))).Replace("-", "").ToLower();
        }
    }

    public class ErrorReporterTest
    {
        private const string SENTRY_HOSTNAME = "xdxd-sentry";
        private const string SENTRY_ORG_NAME = "sentry";

        private const string SENTRY_PROJECT_NAME = "error-reporter-test";
        private const string SENTRY_PROJECT_KEY = "8b4b38e072ad44a1be31fa178eab2762";
        private const int SENTRY_PROJECT_ID = 4;

        private const string SENTRY_BEARER_TOKEN = "15b09b26204e4dc780ba94a197f617f9b548126d5e8d40ddbc678fcb4d05baa1";

        protected static ErrorReporterImpl CreateReporter()
        {
            var errorReporter = new ErrorReporterImpl(new ErrorReporterImplConfig
            {
                SentryDsn = $"http://{SENTRY_PROJECT_KEY}@{SENTRY_HOSTNAME}/{SENTRY_PROJECT_ID}",
                Environment = "testing",
                AppVersion = "1.0.0.0",
                InstanceName = "xunit-test",
            });

            return errorReporter;
        }

        protected static async Task AssertEventSentToServer(string eventID)
        {
            string eventJson = await GetServerEvent(eventID);
            var eventObject = JsonDocument.Parse(eventJson).RootElement;
            string serverEventID = eventObject.GetProperty("eventID").GetString();

            Assert.Equal(eventID, serverEventID);
        }

        protected static async Task<string> GetServerEvent(string eventID)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var token = cts.Token;

            string responseString = "Event not found";
            while (responseString.Contains("Event not found"))
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SENTRY_BEARER_TOKEN);
                var httpResponse = await httpClient.GetAsync(
                    $"http://{SENTRY_HOSTNAME}/api/0/projects/{SENTRY_ORG_NAME}/{SENTRY_PROJECT_NAME}/events/{eventID}/",
                    token);
                responseString = await httpResponse.Content.ReadAsStringAsync(token);

                token.ThrowIfCancellationRequested();
                await Task.Delay(200, token);
            }

            return responseString;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected static void ThrowException(string fingerprint = null)
        {
            throw new DetailedException($"Testing {nameof(ErrorReporterImpl)}.") { Fingerprint = fingerprint };
        }
    }
}
