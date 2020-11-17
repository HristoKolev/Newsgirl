namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Xunit;

    public class ErrorReporterTest
    {
        private static ErrorReporterImpl CreateReporter()
        {
            var errorReporterConfig = new ErrorReporterConfig
            {
                SentryDsn = "http://daefbbb645b74db1a5f5060b8d4b1dd3@home-sentry.lan/9",
            };

            var appInfoConfig = new AppInfoConfig
            {
                Environment = "testing",
                AppVersion = "1.0.0.0",
                ServerName = "xunit-test",
            };

            var errorReporter = new ErrorReporterImpl(errorReporterConfig, appInfoConfig);

            return errorReporter;
        }

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
                await reporter.Error(exception);
            }
        }

        [Fact]
        public async Task Error2_returns_without_error()
        {
            var reporter = CreateReporter();

            var testException = new DetailedException($"Testing {nameof(ErrorReporterImpl)}.");

            await reporter.Error(testException, "TESTING_FINGERPRINT");
        }

        [Fact]
        public async Task Error3_returns_without_error()
        {
            var reporter = CreateReporter();

            var testException = new DetailedException($"Testing {nameof(ErrorReporterImpl)}.");

            await reporter.Error(testException, new Dictionary<string, object>
            {
                {"testkey", "testvalue"},
            });
        }

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
                await reporter.Error(exception, "TESTING_FINGERPRINT", new Dictionary<string, object>
                {
                    {"test_key", "test_value"},
                });
            }
        }

        [Fact]
        public async Task Error_call_hooks_if_available()
        {
            var reporter = CreateReporter();

            reporter.AddDataHook(() => new Dictionary<string, object>
            {
                {"test_hook_key", "test_hook_value"},
            });

            try
            {
                ThrowException();
            }
            catch (Exception exception)
            {
                await reporter.Error(exception, "TESTING_FINGERPRINT", new Dictionary<string, object>
                {
                    {"test_key", "test_value"},
                });
            }
        }

        [Fact]
        public async Task Error_takes_fingerprint_from_exception()
        {
            var reporter = CreateReporter();

            try
            {
                ThrowException("TESTING_FINGERPRINT");
            }
            catch (Exception exception)
            {
                await reporter.Error(exception, new Dictionary<string, object>
                {
                    {"test_key", "test_value"},
                });
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowException(string fingerprint = null)
        {
            throw new DetailedException($"Testing {nameof(ErrorReporterImpl)}.")
            {
                Fingerprint = fingerprint,
            };
        }
    }
}
